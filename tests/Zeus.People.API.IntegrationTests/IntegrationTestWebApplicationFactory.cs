using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Interfaces;

namespace Zeus.People.API.IntegrationTests;

/// <summary>
/// Custom WebApplicationFactory used for integration tests. It trims external dependencies
/// (SQL, EventStore, real Cosmos usage) and injects an in-memory read repository so that
/// endpoints can be exercised without live infrastructure.
/// </summary>
public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Force test environment early so Infrastructure layer sees "Testing" and uses its
        // test-oriented branches (e.g. CosmosClient emulator connection creation that does not validate endpoint).
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            config.Sources.Clear();
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Environment
                ["ASPNETCORE_ENVIRONMENT"] = "Testing",
                ["DOTNET_ENVIRONMENT"] = "Testing",

                // Feature flags - disable SQL + EventStore to avoid EF/DB setup
                ["Features:EnableSqlDatabase"] = "false",
                ["Features:EnableEventStore"] = "false",

                // JWT settings used by application auth configuration
                ["JwtSettings:SecretKey"] = "integration-test-secret-key-which-is-long-1234567890",
                ["JwtSettings:Issuer"] = "Zeus.People.API.IntegrationTests",
                ["JwtSettings:Audience"] = "Zeus.People.Client.IntegrationTests",
                ["JwtSettings:ExpirationMinutes"] = "30",

                // Cosmos (values largely irrelevant; client constructed but not used by mock repository)
                ["CosmosDb:DatabaseName"] = "Zeus.People.Testing",
                ["CosmosDb:Endpoint"] = "https://localhost:8081/",
                ["CosmosDb:AuthKey"] = "test-key",
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace JWT bearer configuration so tokens we issue here validate
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var keyBytes = Encoding.ASCII.GetBytes("integration-test-secret-key-which-is-long-1234567890");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                    ValidateIssuer = true,
                    ValidIssuer = "Zeus.People.API.IntegrationTests",
                    ValidateAudience = true,
                    ValidAudience = "Zeus.People.Client.IntegrationTests",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Swap the read repository registrations for our in-memory version.
            // Replace only the academic read repository needed for current test scope.
            ReplaceService<IAcademicReadRepository>(services, _ => new InMemoryAcademicReadRepository());
        });

        return base.CreateHost(builder);
    }

    private static void ReplaceService<T>(IServiceCollection services, Func<IServiceProvider, T> factory) where T : class
    {
        var existing = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var d in existing) services.Remove(d);
        services.AddSingleton(factory);
    }
}

/// <summary>
/// Simple in-memory implementation of the academic read repositories used by the AcademicsController.
/// Only the GetAll* path is required for the current integration test; other members return not-found / empty data.
/// </summary>
internal class InMemoryAcademicReadRepository : IAcademicReadRepository
{
    private readonly List<AcademicSummaryDto> _summaries;
    private readonly List<AcademicDto> _academics;

    public InMemoryAcademicReadRepository()
    {
        _academics = new List<AcademicDto>
        {
            new AcademicDto
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                EmpNr = "EMP100",
                EmpName = "Integration Tester",
                Rank = "Professor",
                IsTenured = true,
                DepartmentId = Guid.NewGuid(),
                DepartmentName = "Test Dept",
                RoomId = Guid.NewGuid(),
                RoomNumber = "T100",
                ExtensionId = Guid.NewGuid(),
                ExtensionNumber = "9001"
            }
        };

        _summaries = _academics.Select(a => new AcademicSummaryDto
        {
            Id = a.Id,
            EmpNr = a.EmpNr,
            EmpName = a.EmpName,
            Rank = a.Rank,
            IsTenured = a.IsTenured,
            DepartmentName = a.DepartmentName,
            RoomNumber = a.RoomNumber
        }).ToList();
    }

    public Task<Result<AcademicDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var academic = _academics.FirstOrDefault(a => a.Id == id);
        return Task.FromResult(academic != null
            ? Result.Success(academic)
            : Result.Failure<AcademicDto>(new Error("Academic.NotFound", $"Academic {id} not found")));
    }

    public Task<Result<AcademicDto>> GetByEmpNrAsync(string empNr, CancellationToken cancellationToken = default)
    {
        var academic = _academics.FirstOrDefault(a => a.EmpNr == empNr);
        return Task.FromResult(academic != null
            ? Result.Success(academic)
            : Result.Failure<AcademicDto>(new Error("Academic.NotFound", $"Academic empNr {empNr} not found")));
    }

    public Task<Result<PagedResult<AcademicSummaryDto>>> GetAllAsync(int pageNumber, int pageSize, string? nameFilter = null, string? rankFilter = null, bool? isTenuredFilter = null, CancellationToken cancellationToken = default)
    {
        IEnumerable<AcademicSummaryDto> query = _summaries;
        if (!string.IsNullOrWhiteSpace(nameFilter))
            query = query.Where(a => a.EmpName.Contains(nameFilter, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(rankFilter))
            query = query.Where(a => a.Rank.Equals(rankFilter, StringComparison.OrdinalIgnoreCase));
        if (isTenuredFilter.HasValue)
            query = query.Where(a => a.IsTenured == isTenuredFilter.Value);

        var items = query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();
        var paged = new PagedResult<AcademicSummaryDto>
        {
            Items = items,
            TotalCount = query.Count(),
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        return Task.FromResult(Result<PagedResult<AcademicSummaryDto>>.Success(paged));
    }

    public Task<Result<PagedResult<AcademicSummaryDto>>> GetByDepartmentAsync(Guid departmentId, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<PagedResult<AcademicSummaryDto>>.Success(new PagedResult<AcademicSummaryDto>{ Items = new List<AcademicSummaryDto>(), TotalCount = 0, PageNumber = pageNumber, PageSize = pageSize }));

    public Task<Result<PagedResult<AcademicSummaryDto>>> GetByRankAsync(string rank, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<PagedResult<AcademicSummaryDto>>.Success(new PagedResult<AcademicSummaryDto>{ Items = new List<AcademicSummaryDto>(), TotalCount = 0, PageNumber = pageNumber, PageSize = pageSize }));

    public Task<Result<PagedResult<AcademicSummaryDto>>> GetTenuredAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<PagedResult<AcademicSummaryDto>>.Success(new PagedResult<AcademicSummaryDto>{ Items = _summaries.Where(s=>s.IsTenured).ToList(), TotalCount = _summaries.Count(s=>s.IsTenured), PageNumber = pageNumber, PageSize = pageSize }));

    public Task<Result<PagedResult<AcademicSummaryDto>>> GetWithExpiringContractsAsync(DateTime? beforeDate, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
        => Task.FromResult(Result<PagedResult<AcademicSummaryDto>>.Success(new PagedResult<AcademicSummaryDto>{ Items = new List<AcademicSummaryDto>(), TotalCount = 0, PageNumber = pageNumber, PageSize = pageSize }));

    public Task<Result<List<AcademicCountByDepartmentDto>>> GetCountByDepartmentAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(Result<List<AcademicCountByDepartmentDto>>.Success(new List<AcademicCountByDepartmentDto>()));
}
