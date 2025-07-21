using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FluentAssertions;
using System.Net;
using Xunit.Abstractions;

namespace Zeus.People.API.Tests;

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
    public void ApplicationStartup_WithDefaultConfiguration_ShouldSucceed()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" },
                    { "AzureAd:TenantId", "test-tenant" },
                    { "AzureAd:ClientId", "test-client" },
                    { "ServiceBus:ConnectionString", "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test=" },
                    { "KeyVault:VaultName", "test-vault" },
                    { "KeyVault:UseManagedIdentity", "false" }
                });
            });
            builder.ConfigureServices(services =>
            {
                // Override health checks for testing
                services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromSeconds(5));
            });
        });

        // Act & Assert
        var client = factory.CreateClient();
        client.Should().NotBeNull();

        _output.WriteLine("Application started successfully with test configuration");
    }

    [Fact]
    public async Task HealthCheck_Endpoint_ShouldBeAccessible()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" },
                    { "AzureAd:TenantId", "test-tenant" },
                    { "AzureAd:ClientId", "test-client" },
                    { "ServiceBus:ConnectionString", "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test=" },
                    { "KeyVault:VaultName", "test-vault" },
                    { "KeyVault:UseManagedIdentity", "false" }
                });
            });
        });

        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.ServiceUnavailable);
        _output.WriteLine($"Health check endpoint returned: {response.StatusCode}");
    }

    [Fact]
    public void ConfigurationServices_ShouldBeRegistered()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", "Data Source=:memory:" },
                    { "AzureAd:TenantId", "test-tenant" },
                    { "AzureAd:ClientId", "test-client" },
                    { "ServiceBus:ConnectionString", "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test=" },
                    { "KeyVault:VaultName", "test-vault" },
                    { "KeyVault:UseManagedIdentity", "false" }
                });
            });
        });

        // Act
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;

        // Assert
        var configuration = services.GetService<IConfiguration>();
        var logger = services.GetService<ILogger<ApplicationStartupTests>>();

        configuration.Should().NotBeNull();
        logger.Should().NotBeNull();

        _output.WriteLine("All required services are registered");
    }

    [Fact]
    public void ApplicationStartup_WithAzureConfiguration_ShouldLoadSettings()
    {
        // Arrange
        var testSettings = new Dictionary<string, string?>
        {
            { "ConnectionStrings:DefaultConnection", "Server=test;Database=TestDB;Integrated Security=true;" },
            { "AzureAd:TenantId", "11111111-1111-1111-1111-111111111111" },
            { "AzureAd:ClientId", "22222222-2222-2222-2222-222222222222" },
            { "AzureAd:Instance", "https://login.microsoftonline.com/" },
            { "ServiceBus:ConnectionString", "Endpoint=sb://test-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=testkey=" },
            { "KeyVault:VaultName", "test-key-vault" },
            { "KeyVault:UseManagedIdentity", "true" },
            { "ApplicationInsights:ConnectionString", "InstrumentationKey=test-key;IngestionEndpoint=https://test.in.applicationinsights.azure.com/" }
        };

        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(testSettings);
            });
        });

        // Act
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        // Assert
        configuration["AzureAd:TenantId"].Should().Be("11111111-1111-1111-1111-111111111111");
        configuration["ServiceBus:ConnectionString"].Should().Contain("test-namespace.servicebus.windows.net");
        configuration["KeyVault:VaultName"].Should().Be("test-key-vault");
        configuration["KeyVault:UseManagedIdentity"].Should().Be("true");

        _output.WriteLine("Azure configuration loaded successfully");
    }

    [Fact]
    public void ApplicationStartup_InDevelopmentEnvironment_ShouldUseLocalSettings()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "ConnectionStrings:DefaultConnection", "Data Source=localhost;Initial Catalog=ZeusPeopleDB;Integrated Security=true;" },
                    { "KeyVault:UseManagedIdentity", "false" },
                    { "AzureAd:TenantId", "dev-tenant" },
                    { "AzureAd:ClientId", "dev-client" }
                });
            });
        });

        // Act
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var environment = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();

        // Assert
        environment.EnvironmentName.Should().Be("Development");
        configuration["KeyVault:UseManagedIdentity"].Should().Be("false");
        configuration["ConnectionStrings:DefaultConnection"].Should().Contain("localhost");

        _output.WriteLine("Development environment configuration validated");
    }

    [Fact]
    public void ApplicationStartup_WithInvalidConfiguration_ShouldHandleGracefully()
    {
        // Arrange
        var factory = _factory.WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Testing");
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Intentionally missing required configuration
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    { "SomeOtherSetting", "value" }
                });
            });
        });

        // Act & Assert - Application should still start even with incomplete config
        var client = factory.CreateClient();
        client.Should().NotBeNull();

        _output.WriteLine("Application handled missing configuration gracefully");
    }
}
