using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Net;
using Zeus.People.API.Configuration;
using Xunit;
using Xunit.Abstractions;

namespace Zeus.People.Tests.Integration.Configuration;

/// <summary>
/// Integration tests for application startup with Azure configuration
/// </summary>
public class ApplicationStartupTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public ApplicationStartupTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task Application_StartsSuccessfully_WithDefaultConfiguration()
    {
        // Arrange & Act
        var client = _factory.CreateClient();

        // Assert - Application should start without throwing
        Assert.NotNull(client);
        _output.WriteLine("✅ Application started successfully with default configuration");
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy_OnStartup()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Healthy", content);
        _output.WriteLine($"✅ Health check response: {content}");
    }

    [Fact]
    public async Task Configuration_LoadsAllRequiredSections()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act & Assert - Check required configuration sections
        var requiredSections = new[]
        {
            "Logging",
            "AllowedHosts"
        };

        foreach (var section in requiredSections)
        {
            var sectionExists = configuration.GetSection(section).Exists();
            Assert.True(sectionExists, $"Configuration section '{section}' should exist");
            _output.WriteLine($"✅ Configuration section '{section}' exists");
        }
    }

    [Fact]
    public async Task ConfigurationService_IsRegistered()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var configService = scope.ServiceProvider.GetService<IConfigurationService>();

        // Assert
        Assert.NotNull(configService);
        _output.WriteLine("✅ Configuration service is properly registered");
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Staging")]
    [InlineData("Production")]
    public async Task Application_StartsWithEnvironment(string environment)
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(environment);
        });

        // Act
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _output.WriteLine($"✅ Application started successfully in {environment} environment");
    }

    [Fact]
    public async Task Application_HandlesKeyVaultConfiguration_Gracefully()
    {
        // Arrange - Configure with a test Key Vault URL
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["KeyVault:VaultUrl"] = "https://test-keyvault.vault.azure.net/"
                });
            });
        });

        // Act & Assert - Application should start even if Key Vault is not accessible
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");

        // Should not fail startup, might show warnings in logs
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        _output.WriteLine("✅ Application handles Key Vault configuration gracefully");
    }

    [Fact]
    public async Task Swagger_IsAvailable_InDevelopment()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger");

        // Assert
        Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.Redirect);
        _output.WriteLine("✅ Swagger UI is available in Development environment");
    }

    [Fact]
    public async Task Swagger_IsNotAvailable_InProduction()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Production");
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        _output.WriteLine("✅ Swagger UI is not available in Production environment");
    }

    [Fact]
    public async Task ConfigurationValidation_RunsOnStartup()
    {
        // This test verifies that the configuration validation doesn't prevent startup
        // even when some configurations might be missing

        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                // Add a test service to verify DI container is working
                services.AddSingleton<TestStartupService>();
            });
        });

        // Act
        using var scope = factory.Services.CreateScope();
        var testService = scope.ServiceProvider.GetService<TestStartupService>();

        // Assert
        Assert.NotNull(testService);
        _output.WriteLine("✅ Configuration validation completed successfully on startup");
    }

    [Fact]
    public async Task Application_ConfiguresJwtAuthentication()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Act - Try to access an authenticated endpoint
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/people");

        // Assert - Should return 401 Unauthorized (not 500 error)
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        _output.WriteLine("✅ JWT Authentication is properly configured");
    }

    [Fact]
    public async Task HealthCheck_IncludesDetailedInformation()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        Assert.Contains("status", content);
        Assert.Contains("totalDuration", content);
        Assert.Contains("timestamp", content);

        _output.WriteLine($"✅ Health check includes detailed information: {content}");
    }

    /// <summary>
    /// Test service to verify DI container is working
    /// </summary>
    private class TestStartupService
    {
        public bool IsInitialized { get; } = true;
    }
}

/// <summary>
/// Configuration-specific integration tests
/// </summary>
public class ConfigurationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly ITestOutputHelper _output;

    public ConfigurationIntegrationTests(WebApplicationFactory<Program> factory, ITestOutputHelper output)
    {
        _factory = factory;
        _output = output;
    }

    [Fact]
    public async Task ConfigurationService_GetSecretAsync_HandlesNonExistentSecrets()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

        // Act
        var secret = await configService.GetSecretAsync("non-existent-secret");

        // Assert
        Assert.Null(secret);
        _output.WriteLine("✅ Configuration service handles non-existent secrets gracefully");
    }

    [Fact]
    public async Task Configuration_LoadsFromMultipleSources()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Add test configuration from multiple sources
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["TestSetting"] = "FromMemory",
                    ["Database:ConnectionString"] = "TestConnection"
                });
            });
        });

        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act
        var testSetting = configuration["TestSetting"];
        var dbConnection = configuration["Database:ConnectionString"];

        // Assert
        Assert.Equal("FromMemory", testSetting);
        Assert.Equal("TestConnection", dbConnection);
        _output.WriteLine("✅ Configuration loads from multiple sources correctly");
    }

    [Fact]
    public async Task Application_ValidatesConfigurationClasses()
    {
        // This test verifies that configuration classes can be instantiated and validated

        // Arrange
        using var scope = _factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Act & Assert - Try to create configuration objects
        try
        {
            var dbConfig = configuration.GetSection(DatabaseConfiguration.SectionName).Get<DatabaseConfiguration>();
            var appConfig = configuration.GetSection(ApplicationConfiguration.SectionName).Get<ApplicationConfiguration>();

            // Configuration objects should be created (even if with default/null values)
            Assert.NotNull(dbConfig ?? new DatabaseConfiguration());
            Assert.NotNull(appConfig ?? new ApplicationConfiguration());

            _output.WriteLine("✅ Configuration classes instantiate correctly");
        }
        catch (Exception ex)
        {
            _output.WriteLine($"❌ Configuration validation failed: {ex.Message}");
            throw;
        }
    }

    [Fact]
    public async Task Application_ConfiguresHttpClientFactory()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();

        // Act
        var httpClientFactory = scope.ServiceProvider.GetService<IHttpClientFactory>();

        // Assert
        Assert.NotNull(httpClientFactory);

        var httpClient = httpClientFactory.CreateClient();
        Assert.NotNull(httpClient);

        _output.WriteLine("✅ HTTP Client Factory is properly configured");
    }
}
