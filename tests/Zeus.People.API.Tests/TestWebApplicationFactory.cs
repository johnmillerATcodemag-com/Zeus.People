using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Zeus.People.Application.Interfaces;

namespace Zeus.People.API.Tests;

/// <summary>
/// Custom WebApplicationFactory for testing with proper JWT configuration
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Configure the application configuration before the host is built
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration sources
            config.Sources.Clear();

            // Add our test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:SecretKey"] = "test-secret-key-that-is-at-least-32-characters-long",
                ["JwtSettings:Issuer"] = "Zeus.People.API.Tests",
                ["JwtSettings:Audience"] = "Zeus.People.Client.Tests",
                ["JwtSettings:ExpirationMinutes"] = "60",
                ["ConnectionStrings:AcademicDatabase"] = "Server=(localdb)\\MSSQLLocalDB;Database=Zeus.People.Academic.Test;Trusted_Connection=True;MultipleActiveResultSets=true",
                ["ConnectionStrings:EventStoreDatabase"] = "Server=(localdb)\\MSSQLLocalDB;Database=Zeus.People.EventStore.Test;Trusted_Connection=True;MultipleActiveResultSets=true",
                ["ConnectionStrings:ServiceBus"] = "Endpoint=sb://localhost.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=test-service-bus-key",
                ["ConnectionStrings:CosmosDb"] = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==",
                ["ASPNETCORE_ENVIRONMENT"] = "Testing"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the existing JWT Bearer authentication configuration
            var jwtDescriptor = services.FirstOrDefault(d => d.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>));
            if (jwtDescriptor != null)
            {
                services.Remove(jwtDescriptor);
            }

            // Add our test JWT configuration
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                var key = Encoding.ASCII.GetBytes("test-secret-key-that-is-at-least-32-characters-long");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = "Zeus.People.API.Tests",
                    ValidateAudience = true,
                    ValidAudience = "Zeus.People.Client.Tests",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

            // Replace database repositories with mock implementations
            var repositoryDescriptors = services.Where(d =>
                d.ServiceType == typeof(IAcademicReadRepository) ||
                d.ServiceType == typeof(IDepartmentReadRepository) ||
                d.ServiceType == typeof(IRoomReadRepository) ||
                d.ServiceType == typeof(IExtensionReadRepository))
                .ToList(); foreach (var descriptor in repositoryDescriptors)
            {
                services.Remove(descriptor);
            }

            // Add mock repository
            var mockRepository = new MockReadModelRepository();
            services.AddSingleton<IAcademicReadRepository>(mockRepository);
            services.AddSingleton<IDepartmentReadRepository>(mockRepository);
            services.AddSingleton<IRoomReadRepository>(mockRepository);
            services.AddSingleton<IExtensionReadRepository>(mockRepository);

            // Remove problematic health checks in test environment
            services.PostConfigure<Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckServiceOptions>(options =>
            {
                var checksToRemove = new[] { "servicebus", "cosmosdb", "database", "eventstore" };
                foreach (var checkName in checksToRemove)
                {
                    var check = options.Registrations.FirstOrDefault(r => r.Name == checkName);
                    if (check != null)
                    {
                        options.Registrations.Remove(check);
                    }
                }
            });
        });

        return base.CreateHost(builder);
    }
}

/// <summary>
/// JWT token generator for testing
/// </summary>
public static class TestJwtTokenGenerator
{
    public static string GenerateToken(string role = "User")
    {
        var key = Encoding.ASCII.GetBytes("test-secret-key-that-is-at-least-32-characters-long");
        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "testuser"),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "Zeus.People.API.Tests",
            Audience = "Zeus.People.Client.Tests",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
