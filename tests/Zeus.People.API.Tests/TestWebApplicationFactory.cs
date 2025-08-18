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
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;

namespace Zeus.People.API.Tests;

/// <summary>
/// Custom WebApplicationFactory for testing with proper JWT configuration
/// </summary>
public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override IHost CreateHost(IHostBuilder builder)
    {
        // Set explicit test environment
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Testing");
        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Testing");

        // Configure the application configuration before the host is built
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Clear existing configuration sources
            config.Sources.Clear();

            // Add comprehensive test configuration
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Environment
                ["ASPNETCORE_ENVIRONMENT"] = "Testing",
                ["DOTNET_ENVIRONMENT"] = "Testing",

                // JWT Settings
                ["JwtSettings:SecretKey"] = "test-secret-key-that-is-at-least-32-characters-long",
                ["JwtSettings:Issuer"] = "Zeus.People.API.Tests",
                ["JwtSettings:Audience"] = "Zeus.People.Client.Tests",
                ["JwtSettings:ExpirationMinutes"] = "60",

                // Azure AD Settings (disabled for testing)
                ["AzureAd:Instance"] = "https://login.microsoftonline.com/",
                ["AzureAd:TenantId"] = "test-tenant-id",
                ["AzureAd:ClientId"] = "test-client-id",
                ["AzureAd:Audience"] = "test-audience",

                // Database connections (using in-memory or local test databases)
                ["ConnectionStrings:AcademicDatabase"] = "Data Source=:memory:",
                ["ConnectionStrings:EventStoreDatabase"] = "Data Source=:memory:",
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:",

                // External service connections (mocked)
                ["ConnectionStrings:ServiceBus"] = "Endpoint=sb://test-host/;SharedAccessKeyName=test;SharedAccessKey=test=",
                ["ConnectionStrings:CosmosDb"] = "AccountEndpoint=https://test-cosmos/;AccountKey=test-key",

                // Key Vault (disabled for testing)
                ["KeyVault:VaultUrl"] = "",
                ["KeyVault:UseManagedIdentity"] = "false",
                ["KeyVault:ClientId"] = "",
                ["KeyVault:ClientSecret"] = "",

                // Application Insights (disabled for testing)
                ["ApplicationInsights:ConnectionString"] = "",
                ["ApplicationInsights:InstrumentationKey"] = "",

                // Logging
                ["Logging:LogLevel:Default"] = "Warning",
                ["Logging:LogLevel:Microsoft"] = "Warning",
                ["Logging:LogLevel:Microsoft.Hosting.Lifetime"] = "Information"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Set test environment explicitly
            services.Configure<IWebHostEnvironment>(env =>
            {
                // This is handled by the environment variables above
            });

            // Remove and replace authentication services
            RemoveService<IConfigureOptions<JwtBearerOptions>>(services);

            // Add test JWT configuration
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

            // Replace all read repositories with mock implementations
            ReplaceService<IAcademicReadRepository>(services, provider => new MockReadModelRepository());
            ReplaceService<IDepartmentReadRepository>(services, provider => new MockReadModelRepository());
            ReplaceService<IRoomReadRepository>(services, provider => new MockReadModelRepository());
            ReplaceService<IExtensionReadRepository>(services, provider => new MockReadModelRepository());

            // Replace health checks with test-friendly versions
            RemoveAllHealthChecks(services);
            services.AddHealthChecks()
                .AddCheck("test-health", () => HealthCheckResult.Healthy("Test environment health check"));

            // Configure host options for faster test teardown
            services.Configure<HostOptions>(opts =>
            {
                opts.ShutdownTimeout = TimeSpan.FromSeconds(5);
                opts.ServicesStartConcurrently = true;
                opts.ServicesStopConcurrently = true;
            });
        });

        return base.CreateHost(builder);
    }

    /// <summary>
    /// Helper method to remove a service from the service collection
    /// </summary>
    private static void RemoveService<T>(IServiceCollection services)
    {
        var descriptor = services.FirstOrDefault(d => d.ServiceType == typeof(T));
        if (descriptor != null)
        {
            services.Remove(descriptor);
        }
    }

    /// <summary>
    /// Helper method to replace a service with a new implementation
    /// </summary>
    private static void ReplaceService<T>(IServiceCollection services, Func<IServiceProvider, T> factory) where T : class
    {
        // Remove existing registrations
        var descriptors = services.Where(d => d.ServiceType == typeof(T)).ToList();
        foreach (var descriptor in descriptors)
        {
            services.Remove(descriptor);
        }

        // Add new registration
        services.AddSingleton<T>(factory);
    }

    /// <summary>
    /// Helper method to remove all health check registrations
    /// </summary>
    private static void RemoveAllHealthChecks(IServiceCollection services)
    {
        // Remove health check service registrations
        var healthCheckDescriptors = services.Where(d =>
            d.ServiceType == typeof(HealthCheckService) ||
            d.ServiceType.FullName?.Contains("HealthCheck") == true)
            .ToList();

        foreach (var descriptor in healthCheckDescriptors)
        {
            services.Remove(descriptor);
        }
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
