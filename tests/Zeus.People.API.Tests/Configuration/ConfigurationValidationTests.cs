using System.ComponentModel.DataAnnotations;
using Xunit;
using Zeus.People.API.Configuration;

namespace Zeus.People.API.Tests.Configuration;

/// <summary>
/// Tests to verify that configuration validation catches invalid values
/// </summary>
public class ConfigurationValidationTests
{
    #region Database Configuration Tests

    [Fact]
    public void DatabaseConfiguration_WithEmptyConnectionStrings_ShouldFailValidation()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            WriteConnectionString = "",
            ReadConnectionString = "",
            EventStoreConnectionString = "",
            CommandTimeoutSeconds = 30,
            MaxRetryCount = 3,
            ConnectionPoolMinSize = 5,
            ConnectionPoolMaxSize = 100,
            ConnectionLifetimeMinutes = 15
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Write database connection string is required", exception.Message);
        Assert.Contains("Read database connection string is required", exception.Message);
        Assert.Contains("Event store database connection string is required", exception.Message);
    }

    [Fact]
    public void DatabaseConfiguration_WithInvalidTimeoutRange_ShouldFailValidation()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            WriteConnectionString = "Server=localhost;Database=Test;",
            ReadConnectionString = "Server=localhost;Database=Test;",
            EventStoreConnectionString = "Server=localhost;Database=Test;",
            CommandTimeoutSeconds = 500, // Exceeds max of 300
            MaxRetryCount = 3,
            ConnectionPoolMinSize = 5,
            ConnectionPoolMaxSize = 100,
            ConnectionLifetimeMinutes = 15
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Command timeout must be between 1 and 300 seconds", exception.Message);
    }

    [Fact]
    public void DatabaseConfiguration_WithInvalidRetryCount_ShouldFailValidation()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            WriteConnectionString = "Server=localhost;Database=Test;",
            ReadConnectionString = "Server=localhost;Database=Test;",
            EventStoreConnectionString = "Server=localhost;Database=Test;",
            CommandTimeoutSeconds = 30,
            MaxRetryCount = 15, // Exceeds max of 10
            ConnectionPoolMinSize = 5,
            ConnectionPoolMaxSize = 100,
            ConnectionLifetimeMinutes = 15
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Max retry count must be between 0 and 10", exception.Message);
    }

    [Fact]
    public void DatabaseConfiguration_WithInvalidPoolSizeRelationship_ShouldFailValidation()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            WriteConnectionString = "Server=localhost;Database=Test;",
            ReadConnectionString = "Server=localhost;Database=Test;",
            EventStoreConnectionString = "Server=localhost;Database=Test;",
            CommandTimeoutSeconds = 30,
            MaxRetryCount = 3,
            ConnectionPoolMinSize = 50, // Greater than max
            ConnectionPoolMaxSize = 25,
            ConnectionLifetimeMinutes = 15
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Connection pool maximum size must be greater than or equal to minimum size", exception.Message);
    }

    [Fact]
    public void DatabaseConfiguration_WithTooShortTimeoutForProduction_ShouldFailValidation()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            WriteConnectionString = "Server=localhost;Database=Test;",
            ReadConnectionString = "Server=localhost;Database=Test;",
            EventStoreConnectionString = "Server=localhost;Database=Test;",
            CommandTimeoutSeconds = 2, // Less than 5 seconds
            MaxRetryCount = 3,
            ConnectionPoolMinSize = 5,
            ConnectionPoolMaxSize = 100,
            ConnectionLifetimeMinutes = 15
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Command timeout should be at least 5 seconds for production use", exception.Message);
    }

    [Fact]
    public void DatabaseConfiguration_WithValidValues_ShouldPassValidation()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            WriteConnectionString = "Server=localhost;Database=Test;",
            ReadConnectionString = "Server=localhost;Database=Test;",
            EventStoreConnectionString = "Server=localhost;Database=Test;",
            CommandTimeoutSeconds = 30,
            MaxRetryCount = 3,
            ConnectionPoolMinSize = 5,
            ConnectionPoolMaxSize = 100,
            ConnectionLifetimeMinutes = 15
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    #endregion

    #region Service Bus Configuration Tests

    [Fact]
    public void ServiceBusConfiguration_WithMissingConnectionStringWhenNotUsingManagedIdentity_ShouldFailValidation()
    {
        // Arrange
        var config = new ServiceBusConfiguration
        {
            ConnectionString = "",
            Namespace = "",
            TopicName = "domain-events",
            SubscriptionName = "academic-management",
            MessageRetryCount = 3,
            MaxConcurrentCalls = 10,
            UseManagedIdentity = false, // Requires connection string
            PrefetchCount = 10,
            MaxDeliveryCount = 5
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        // The validation will fail on DataAnnotations first with the required attribute message
        Assert.Contains("Service Bus connection string is required", exception.Message);
    }

    [Fact]
    public void ServiceBusConfiguration_WithMissingNamespaceWhenUsingManagedIdentity_ShouldFailValidation()
    {
        // Arrange
        var config = new ServiceBusConfiguration
        {
            ConnectionString = "test",
            Namespace = "", // Required when UseManagedIdentity = true
            TopicName = "domain-events",
            SubscriptionName = "academic-management",
            MessageRetryCount = 3,
            MaxConcurrentCalls = 10,
            UseManagedIdentity = true,
            PrefetchCount = 10,
            MaxDeliveryCount = 5
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Namespace is required when using managed identity", exception.Message);
    }

    [Fact]
    public void ServiceBusConfiguration_WithEmptyRequiredFields_ShouldFailValidation()
    {
        // Arrange
        var config = new ServiceBusConfiguration
        {
            ConnectionString = "test",
            Namespace = "test",
            TopicName = "", // Required
            SubscriptionName = "", // Required
            MessageRetryCount = 3,
            MaxConcurrentCalls = 10,
            UseManagedIdentity = true,
            PrefetchCount = 10,
            MaxDeliveryCount = 5
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Topic name is required", exception.Message);
        Assert.Contains("Subscription name is required", exception.Message);
    }

    [Fact]
    public void ServiceBusConfiguration_WithInvalidRangeValues_ShouldFailValidation()
    {
        // Arrange
        var config = new ServiceBusConfiguration
        {
            ConnectionString = "test",
            Namespace = "test",
            TopicName = "domain-events",
            SubscriptionName = "academic-management",
            MessageRetryCount = 15, // Exceeds max of 10
            MaxConcurrentCalls = 150, // Exceeds max of 100
            UseManagedIdentity = true,
            PrefetchCount = 1500, // Exceeds max of 1000
            MaxDeliveryCount = 150 // Exceeds max of 100
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Message retry count must be between 0 and 10", exception.Message);
        Assert.Contains("Max concurrent calls must be between 1 and 100", exception.Message);
        Assert.Contains("Prefetch count must be between 0 and 1000", exception.Message);
        Assert.Contains("Max delivery count must be between 1 and 100", exception.Message);
    }

    [Fact]
    public void ServiceBusConfiguration_WithValidValues_ShouldPassValidation()
    {
        // Arrange
        var config = new ServiceBusConfiguration
        {
            ConnectionString = "Endpoint=sb://test.servicebus.windows.net/;SharedAccessKeyName=test;SharedAccessKey=test=",
            Namespace = "sb-academic-dev-local",
            TopicName = "domain-events",
            SubscriptionName = "academic-management",
            MessageRetryCount = 3,
            MaxConcurrentCalls = 10,
            UseManagedIdentity = true,
            AutoCompleteMessages = true,
            PrefetchCount = 10,
            RequiresSession = false,
            EnableDeadLetterQueue = true,
            MaxDeliveryCount = 5
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    #endregion

    #region Azure AD Configuration Tests

    [Fact]
    public void AzureAdConfiguration_WithMissingRequiredFields_ShouldFailValidation()
    {
        // Arrange
        var config = new AzureAdConfiguration
        {
            Instance = "", // Required
            TenantId = "", // Required
            ClientId = "", // Required
            Audience = "", // Required
            TokenCacheDurationMinutes = 60
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Azure AD instance is required", exception.Message);
        Assert.Contains("Azure AD tenant ID is required", exception.Message);
        Assert.Contains("Client ID is required", exception.Message);
        Assert.Contains("Audience is required", exception.Message);
    }

    [Fact]
    public void AzureAdConfiguration_WithInvalidUrlFormat_ShouldFailValidation()
    {
        // Arrange
        var config = new AzureAdConfiguration
        {
            Instance = "not-a-valid-url", // Invalid URL
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            Audience = "api://academic-management",
            TokenCacheDurationMinutes = 60
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Azure AD instance must be a valid URL", exception.Message);
    }

    [Fact]
    public void AzureAdConfiguration_WithInvalidTokenCacheDuration_ShouldFailValidation()
    {
        // Arrange
        var config = new AzureAdConfiguration
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            Audience = "api://academic-management",
            TokenCacheDurationMinutes = 2000 // Exceeds max of 1440
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Token cache duration must be between 1 minute and 24 hours", exception.Message);
    }

    [Fact]
    public void AzureAdConfiguration_WithInvalidDomainFormat_ShouldFailValidation()
    {
        // Arrange
        var config = new AzureAdConfiguration
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            Audience = "api://academic-management",
            Domain = "invalid-domain-format", // Should contain .onmicrosoft.com or .b2clogin.com
            TokenCacheDurationMinutes = 60
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Domain should be a valid Azure AD B2C domain", exception.Message);
    }

    [Fact]
    public void AzureAdConfiguration_WithValidValues_ShouldPassValidation()
    {
        // Arrange
        var config = new AzureAdConfiguration
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            ClientSecret = "test-secret-key",
            Audience = "api://academic-management",
            ValidIssuers = new List<string> { "https://login.microsoftonline.com/12345678-1234-1234-1234-123456789012/v2.0" },
            Domain = "",
            EnableTokenCaching = true,
            TokenCacheDurationMinutes = 60
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    #endregion

    #region Application Configuration Tests

    [Fact]
    public void ApplicationConfiguration_WithMissingRequiredFields_ShouldFailValidation()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            ApplicationName = "", // Required
            Version = "", // Required
            Environment = "", // Required
            SupportEmail = "invalid-email" // Invalid email format
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Application name is required", exception.Message);
        Assert.Contains("Application version is required", exception.Message);
        Assert.Contains("Environment is required", exception.Message);
        Assert.Contains("Support email must be a valid email address", exception.Message);
    }

    [Fact]
    public void ApplicationConfiguration_WithInvalidEnvironment_ShouldFailValidation()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            ApplicationName = "Test App",
            Version = "1.0.0",
            Environment = "InvalidEnvironment", // Must be Development, Staging, or Production
            SupportEmail = "test@example.com"
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Environment must be one of: Development, Staging, Production", exception.Message);
    }

    [Fact]
    public void ApplicationConfiguration_WithInvalidEmailAddress_ShouldFailValidation()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            ApplicationName = "Test App",
            Version = "1.0.0",
            Environment = "Development",
            SupportEmail = "not-an-email-address" // Invalid email format
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Support email must be a valid email address", exception.Message);
    }

    [Fact]
    public void ApplicationConfiguration_WithValidValues_ShouldPassValidation()
    {
        // Arrange
        var config = new ApplicationConfiguration
        {
            ApplicationName = "Academic Management System",
            Version = "1.0.0",
            Environment = "Development",
            Description = "Zeus.People Academic Management System API",
            SupportEmail = "support@example.com"
        };

        // Act & Assert
        config.Validate(); // Should not throw
    }

    #endregion

    #region Edge Case Tests

    [Fact]
    public void ConfigurationValidation_WithMultipleErrors_ShouldCombineAllErrorMessages()
    {
        // Arrange
        var config = new DatabaseConfiguration
        {
            WriteConnectionString = "", // Required field missing
            ReadConnectionString = "", // Required field missing
            EventStoreConnectionString = "", // Required field missing
            CommandTimeoutSeconds = 500, // Out of range
            MaxRetryCount = 15, // Out of range
            ConnectionPoolMinSize = 50, // Invalid relationship
            ConnectionPoolMaxSize = 25, // Invalid relationship
            ConnectionLifetimeMinutes = 0 // Out of range
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());

        // Should contain multiple error messages
        Assert.Contains("Write database connection string is required", exception.Message);
        Assert.Contains("Read database connection string is required", exception.Message);
        Assert.Contains("Event store database connection string is required", exception.Message);
        Assert.Contains("Command timeout must be between 1 and 300 seconds", exception.Message);
        Assert.Contains("Max retry count must be between 0 and 10", exception.Message);
    }

    [Fact]
    public void ServiceBusConfiguration_WithShortMessageTimeout_ShouldFailValidation()
    {
        // Arrange
        var config = new ServiceBusConfiguration
        {
            ConnectionString = "test",
            Namespace = "test-namespace",
            TopicName = "domain-events",
            SubscriptionName = "academic-management",
            MessageRetryCount = 3,
            MaxConcurrentCalls = 10,
            UseManagedIdentity = true,
            PrefetchCount = 10,
            MaxDeliveryCount = 5,
            MessageTimeout = TimeSpan.FromSeconds(10) // Less than 30 seconds
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Message timeout should be at least 30 seconds", exception.Message);
    }

    [Fact]
    public void ServiceBusConfiguration_WithExcessiveMaxWaitTime_ShouldFailValidation()
    {
        // Arrange
        var config = new ServiceBusConfiguration
        {
            ConnectionString = "test",
            Namespace = "test-namespace",
            TopicName = "domain-events",
            SubscriptionName = "academic-management",
            MessageRetryCount = 3,
            MaxConcurrentCalls = 10,
            UseManagedIdentity = true,
            PrefetchCount = 10,
            MaxDeliveryCount = 5,
            MaxWaitTime = TimeSpan.FromMinutes(5) // More than 1 minute
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Max wait time should not exceed 1 minute for optimal performance", exception.Message);
    }

    [Fact]
    public void AzureAdConfiguration_WithExcessiveClockSkew_ShouldFailValidation()
    {
        // Arrange
        var config = new AzureAdConfiguration
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            Audience = "api://academic-management",
            ClockSkew = TimeSpan.FromHours(1) // More than 30 minutes
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("Clock skew should not exceed 30 minutes for security reasons", exception.Message);
    }

    [Fact]
    public void AzureAdConfiguration_WithInvalidValidIssuers_ShouldFailValidation()
    {
        // Arrange
        var config = new AzureAdConfiguration
        {
            Instance = "https://login.microsoftonline.com/",
            TenantId = "12345678-1234-1234-1234-123456789012",
            ClientId = "87654321-4321-4321-4321-210987654321",
            Audience = "api://academic-management",
            ValidIssuers = new List<string> { "not-a-valid-uri", "another-invalid-uri" }
        };

        // Act & Assert
        var exception = Assert.Throws<InvalidOperationException>(() => config.Validate());
        Assert.Contains("All valid issuers must be well-formed absolute URIs", exception.Message);
    }

    #endregion
}
