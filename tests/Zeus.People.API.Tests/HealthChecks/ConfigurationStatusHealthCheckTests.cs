using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using Zeus.People.API.HealthChecks;
using Zeus.People.API.Configuration;
using Microsoft.Extensions.Options;

namespace Zeus.People.API.Tests.HealthChecks;

/// <summary>
/// Tests to verify that health checks report configuration status correctly
/// </summary>
public class ConfigurationStatusHealthCheckTests : IDisposable
{
    private readonly Mock<ILogger<ConfigurationStatusHealthCheck>> _mockLogger;

    public ConfigurationStatusHealthCheckTests()
    {
        _mockLogger = new Mock<ILogger<ConfigurationStatusHealthCheck>>();
    }

    [Fact]
    public async Task ConfigurationStatusHealthCheck_WhenAllConfigurationsValid_ShouldReturnHealthy()
    {
        // Arrange
        var validDatabaseConfig = new DatabaseConfiguration
        {
            WriteConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;",
            ReadConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;",
            EventStoreConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=TestEventStore;Trusted_Connection=true;",
            CommandTimeoutSeconds = 30,
            MaxRetryCount = 3
        };

        var validServiceBusConfig = new ServiceBusConfiguration
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test",
            UseManagedIdentity = false, // Using connection string, not managed identity
            TopicName = "domain-events",
            SubscriptionName = "main-subscription",
            MaxConcurrentCalls = 1,
            PrefetchCount = 0,
            MessageRetryCount = 3
        };

        var validAzureAdConfig = new AzureAdConfiguration
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            Audience = "api://test-api",
            EnableTokenCaching = true,
            TokenCacheDurationMinutes = 60
        };

        var validApplicationConfig = new ApplicationConfiguration
        {
            ApplicationName = "Zeus.People",
            Version = "1.0.0",
            Environment = "Development",
            SupportEmail = "support@test.com"
        };

        var healthCheck = new ConfigurationStatusHealthCheck(
            Options.Create(validDatabaseConfig),
            Options.Create(validServiceBusConfig),
            Options.Create(validAzureAdConfig),
            Options.Create(validApplicationConfig),
            _mockLogger.Object
        );

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Description.Should().Contain("All configurations are valid");
        result.Data.Should().ContainKey("database_config");
        result.Data.Should().ContainKey("servicebus_config");
        result.Data.Should().ContainKey("azuread_config");
        result.Data.Should().ContainKey("application_config");
        result.Data["database_config"].Should().Be("Valid");
        result.Data["servicebus_config"].Should().Be("Valid");
        result.Data["azuread_config"].Should().Be("Valid");
        result.Data["application_config"].Should().Be("Valid");
    }

    [Fact]
    public async Task ConfigurationStatusHealthCheck_WhenDatabaseConfigInvalid_ShouldReturnUnhealthy()
    {
        // Arrange
        var invalidDatabaseConfig = new DatabaseConfiguration
        {
            WriteConnectionString = "", // Invalid - empty connection string
            ReadConnectionString = "",
            EventStoreConnectionString = "",
            CommandTimeoutSeconds = 500, // Invalid - exceeds maximum
            MaxRetryCount = 15 // Invalid - exceeds maximum
        };

        var validServiceBusConfig = new ServiceBusConfiguration
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test",
            UseManagedIdentity = false,
            TopicName = "domain-events",
            SubscriptionName = "main-subscription"
        };

        var validAzureAdConfig = new AzureAdConfiguration
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            Audience = "api://test-api"
        };

        var validApplicationConfig = new ApplicationConfiguration
        {
            ApplicationName = "Zeus.People",
            Version = "1.0.0",
            Environment = "Development",
            SupportEmail = "admin@test.com"
        };

        var healthCheck = new ConfigurationStatusHealthCheck(
            Options.Create(invalidDatabaseConfig),
            Options.Create(validServiceBusConfig),
            Options.Create(validAzureAdConfig),
            Options.Create(validApplicationConfig),
            _mockLogger.Object
        );

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Configuration validation failed");
        result.Data.Should().ContainKey("database_config");
        result.Data["database_config"].Should().NotBe("Valid");
        result.Data["database_config"].ToString().Should().Contain("Invalid");
    }

    [Fact]
    public async Task ConfigurationStatusHealthCheck_WhenServiceBusConfigInvalid_ShouldReturnUnhealthy()
    {
        // Arrange
        var validDatabaseConfig = new DatabaseConfiguration
        {
            WriteConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;",
            ReadConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;",
            EventStoreConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=TestEventStore;Trusted_Connection=true;"
        };

        var invalidServiceBusConfig = new ServiceBusConfiguration
        {
            ConnectionString = "", // Invalid - empty when not using managed identity
            TopicName = "", // Invalid - empty topic name
            SubscriptionName = "",
            MaxConcurrentCalls = 200, // Invalid - exceeds maximum
            MessageRetryCount = 20 // Invalid - exceeds maximum
        };

        var validAzureAdConfig = new AzureAdConfiguration
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            Audience = "api://test-api"
        };

        var validApplicationConfig = new ApplicationConfiguration
        {
            ApplicationName = "Zeus.People",
            Version = "1.0.0",
            Environment = "Development",
            SupportEmail = "admin@test.com"
        };

        var healthCheck = new ConfigurationStatusHealthCheck(
            Options.Create(validDatabaseConfig),
            Options.Create(invalidServiceBusConfig),
            Options.Create(validAzureAdConfig),
            Options.Create(validApplicationConfig),
            _mockLogger.Object
        );

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Configuration validation failed");
        result.Data.Should().ContainKey("servicebus_config");
        result.Data["servicebus_config"].Should().NotBe("Valid");
    }

    [Fact]
    public async Task ConfigurationStatusHealthCheck_WhenAzureAdConfigInvalid_ShouldReturnUnhealthy()
    {
        // Arrange
        var validDatabaseConfig = new DatabaseConfiguration
        {
            WriteConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;",
            ReadConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;",
            EventStoreConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=TestEventStore;Trusted_Connection=true;"
        };

        var validServiceBusConfig = new ServiceBusConfiguration
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test",
            UseManagedIdentity = false,
            TopicName = "domain-events",
            SubscriptionName = "main-subscription"
        };

        var invalidAzureAdConfig = new AzureAdConfiguration
        {
            Instance = "invalid-url", // Invalid URL format
            TenantId = "", // Invalid - empty tenant
            ClientId = "",
            Audience = "",
            ClockSkew = TimeSpan.FromMinutes(50), // Invalid - exceeds maximum
            TokenCacheDurationMinutes = 2000 // Invalid - exceeds maximum
        };

        var validApplicationConfig = new ApplicationConfiguration
        {
            ApplicationName = "Zeus.People",
            Version = "1.0.0",
            Environment = "Development",
            SupportEmail = "admin@test.com"
        };

        var healthCheck = new ConfigurationStatusHealthCheck(
            Options.Create(validDatabaseConfig),
            Options.Create(validServiceBusConfig),
            Options.Create(invalidAzureAdConfig),
            Options.Create(validApplicationConfig),
            _mockLogger.Object
        );

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Configuration validation failed");
        result.Data.Should().ContainKey("azuread_config");
        result.Data["azuread_config"].Should().NotBe("Valid");
    }

    [Fact]
    public async Task ConfigurationStatusHealthCheck_WhenApplicationConfigInvalid_ShouldReturnUnhealthy()
    {
        // Arrange
        var validDatabaseConfig = new DatabaseConfiguration
        {
            WriteConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;",
            ReadConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;"
        };

        var validServiceBusConfig = new ServiceBusConfiguration
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test",
            UseManagedIdentity = false,
            TopicName = "domain-events",
            SubscriptionName = "main-subscription"
        };

        var validAzureAdConfig = new AzureAdConfiguration
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            Audience = "api://test-api"
        };

        var invalidApplicationConfig = new ApplicationConfiguration
        {
            ApplicationName = "", // Invalid - empty name
            Version = "",
            Environment = "InvalidEnvironment", // Invalid - not Development/Staging/Production
            SupportEmail = "invalid-email" // Invalid email format
        };

        var healthCheck = new ConfigurationStatusHealthCheck(
            Options.Create(validDatabaseConfig),
            Options.Create(validServiceBusConfig),
            Options.Create(validAzureAdConfig),
            Options.Create(invalidApplicationConfig),
            _mockLogger.Object
        );

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Unhealthy);
        result.Description.Should().Contain("Configuration validation failed");
        result.Data.Should().ContainKey("application_config");
        result.Data["application_config"].Should().NotBe("Valid");
    }

    [Fact]
    public async Task ConfigurationStatusHealthCheck_ShouldIncludeDetailedConfigurationInfo()
    {
        // Arrange
        var databaseConfig = new DatabaseConfiguration
        {
            WriteConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;",
            ReadConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=Test;Trusted_Connection=true;",
            EventStoreConnectionString = "Server=(localdb)\\MSSQLLocalDB;Database=EventStore;Trusted_Connection=true;",
            CommandTimeoutSeconds = 30,
            MaxRetryCount = 3
        };

        var serviceBusConfig = new ServiceBusConfiguration
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test",
            UseManagedIdentity = false,
            TopicName = "domain-events",
            SubscriptionName = "main-subscription"
        };

        var azureAdConfig = new AzureAdConfiguration
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "test-tenant-id",
            ClientId = "test-client-id",
            Audience = "api://test-api",
            EnableTokenCaching = false
        };

        var applicationConfig = new ApplicationConfiguration
        {
            ApplicationName = "Zeus.People",
            Version = "1.0.0",
            Environment = "Development",
            SupportEmail = "admin@test.com"
        };

        var healthCheck = new ConfigurationStatusHealthCheck(
            Options.Create(databaseConfig),
            Options.Create(serviceBusConfig),
            Options.Create(azureAdConfig),
            Options.Create(applicationConfig),
            _mockLogger.Object
        );

        var context = new HealthCheckContext();

        // Act
        var result = await healthCheck.CheckHealthAsync(context);

        // Assert
        result.Status.Should().Be(HealthStatus.Healthy);
        result.Data.Should().ContainKey("configuration_summary");
        result.Data.Should().ContainKey("validation_timestamp");

        // Should include configuration details
        var summary = result.Data["configuration_summary"];
        summary.Should().NotBeNull();
        summary.ToString().Should().Contain("Database:");
        summary.ToString().Should().Contain("ServiceBus:");
        summary.ToString().Should().Contain("Azure AD:");
        summary.ToString().Should().Contain("Application:");
    }

    public void Dispose()
    {
        // Cleanup if needed
    }
}
