# Configuration & Secrets Management Instructions

## Overview

Implement secure configuration and secrets management for the Academic Management System using Azure Key Vault and .NET configuration providers.

## Configuration Strategy

### Configuration Sources (in order of precedence)

1. Command line arguments
2. Environment variables
3. Azure Key Vault (for secrets)
4. appsettings.{Environment}.json
5. appsettings.json
6. User secrets (development only)

### Configuration Structure

#### appsettings.json (Base Configuration)

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFramework": "Information"
    }
  },
  "AllowedHosts": "*",
  "ApplicationSettings": {
    "ApplicationName": "Academic Management System",
    "Version": "1.0.0",
    "Environment": "Development"
  },
  "CorsSettings": {
    "AllowedOrigins": ["https://localhost:3000"],
    "AllowedMethods": ["GET", "POST", "PUT", "DELETE"],
    "AllowedHeaders": ["*"]
  },
  "CacheSettings": {
    "DefaultCacheDurationMinutes": 15,
    "ReadModelCacheDurationMinutes": 30
  }
}
```

#### appsettings.Development.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Information"
    }
  },
  "DatabaseSettings": {
    "CommandTimeoutSeconds": 30,
    "EnableSensitiveDataLogging": true
  },
  "ServiceBusSettings": {
    "MessageRetryCount": 3,
    "MessageTimeoutMinutes": 5
  }
}
```

#### appsettings.Production.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "AcademicManagement": "Information"
    }
  },
  "DatabaseSettings": {
    "CommandTimeoutSeconds": 60,
    "EnableSensitiveDataLogging": false
  },
  "ServiceBusSettings": {
    "MessageRetryCount": 5,
    "MessageTimeoutMinutes": 10
  }
}
```

## Secrets Management with Azure Key Vault

### Key Vault Secrets Structure

```
Secrets Naming Convention: {environment}-{service}-{secret-type}

Examples:
- prod-academic-sql-connection
- prod-academic-cosmosdb-connection
- prod-academic-servicebus-connection
- prod-academic-jwt-secret
- prod-academic-appinsights-key
```

### Configuration Classes

#### Database Configuration

```csharp
public class DatabaseConfiguration
{
    public const string SectionName = "DatabaseSettings";

    public string WriteConnectionString { get; set; } = string.Empty;
    public string ReadConnectionString { get; set; } = string.Empty;
    public int CommandTimeoutSeconds { get; set; } = 30;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public int MaxRetryCount { get; set; } = 3;
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);
}
```

#### Service Bus Configuration

```csharp
public class ServiceBusConfiguration
{
    public const string SectionName = "ServiceBusSettings";

    public string ConnectionString { get; set; } = string.Empty;
    public string TopicName { get; set; } = "domain-events";
    public string SubscriptionName { get; set; } = "academic-management";
    public int MessageRetryCount { get; set; } = 3;
    public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxConcurrentCalls { get; set; } = 10;
}
```

#### Azure AD Configuration

```csharp
public class AzureAdConfiguration
{
    public const string SectionName = "AzureAd";

    public string Instance { get; set; } = string.Empty;
    public string TenantId { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public List<string> ValidIssuers { get; set; } = new();
}
```

### Configuration Registration

#### Program.cs Configuration Setup

```csharp
public static void ConfigureAppConfiguration(WebApplicationBuilder builder)
{
    var keyVaultUrl = builder.Configuration["KeyVaultSettings:VaultUrl"];

    if (!string.IsNullOrEmpty(keyVaultUrl))
    {
        var credential = new DefaultAzureCredential();
        builder.Configuration.AddAzureKeyVault(
            new Uri(keyVaultUrl),
            credential);
    }

    // Register configuration classes
    builder.Services.Configure<DatabaseConfiguration>(
        builder.Configuration.GetSection(DatabaseConfiguration.SectionName));

    builder.Services.Configure<ServiceBusConfiguration>(
        builder.Configuration.GetSection(ServiceBusConfiguration.SectionName));

    builder.Services.Configure<AzureAdConfiguration>(
        builder.Configuration.GetSection(AzureAdConfiguration.SectionName));
}
```

## Environment-Specific Configuration

### Development Environment

- Use User Secrets for local development
- Local SQL Server or SQL Server LocalDB
- Azure Storage Emulator for Service Bus testing
- Application Insights in development mode

### Staging Environment

- Azure Key Vault for secrets
- Azure SQL Database (Basic tier)
- Azure Service Bus (Standard tier)
- Separate Azure AD B2C tenant for testing

### Production Environment

- Azure Key Vault with access policies
- Azure SQL Database (Premium tier)
- Azure Service Bus (Premium tier)
- Production Azure AD B2C tenant
- Application Insights with sampling

## Security Best Practices

### Key Vault Access

- Use Managed Identity for Azure resources
- Implement least privilege access
- Regular access review and rotation
- Monitor Key Vault access logs

### Connection Strings

- Never store connection strings in code
- Use Key Vault references in App Service
- Implement connection string validation
- Use encrypted connections only

### Sensitive Data Handling

- Mark sensitive properties with [JsonIgnore]
- Implement custom configuration providers for encryption
- Use data protection APIs for temporary encryption
- Audit configuration access

## Configuration Validation

### Startup Validation

```csharp
public static void ValidateConfiguration(IServiceProvider services)
{
    var databaseConfig = services.GetRequiredService<IOptions<DatabaseConfiguration>>();
    var serviceBusConfig = services.GetRequiredService<IOptions<ServiceBusConfiguration>>();
    var azureAdConfig = services.GetRequiredService<IOptions<AzureAdConfiguration>>();

    ValidateDatabaseConfiguration(databaseConfig.Value);
    ValidateServiceBusConfiguration(serviceBusConfig.Value);
    ValidateAzureAdConfiguration(azureAdConfig.Value);
}

private static void ValidateDatabaseConfiguration(DatabaseConfiguration config)
{
    if (string.IsNullOrEmpty(config.WriteConnectionString))
        throw new InvalidOperationException("Write database connection string is required");

    if (string.IsNullOrEmpty(config.ReadConnectionString))
        throw new InvalidOperationException("Read database connection string is required");
}
```

### Health Checks

```csharp
public static void AddConfigurationHealthChecks(IServiceCollection services)
{
    services.AddHealthChecks()
        .AddCheck<DatabaseConnectionHealthCheck>("database")
        .AddCheck<ServiceBusConnectionHealthCheck>("servicebus")
        .AddCheck<KeyVaultHealthCheck>("keyvault")
        .AddCheck<AzureAdHealthCheck>("azuread");
}
```

## Configuration Documentation

### Environment Variables Documentation

| Variable                                  | Description             | Required   | Default     |
| ----------------------------------------- | ----------------------- | ---------- | ----------- |
| ASPNETCORE_ENVIRONMENT                    | Application environment | Yes        | Development |
| KeyVaultSettings\_\_VaultUrl              | Azure Key Vault URL     | Yes (Prod) | -           |
| ApplicationInsights\_\_InstrumentationKey | App Insights key        | Yes (Prod) | -           |

### Configuration Dependencies

- Database configurations depend on Key Vault secrets
- Service Bus requires valid connection string
- Azure AD requires tenant and client configuration
- Application Insights requires instrumentation key

## Troubleshooting Configuration Issues

### Common Issues

1. **Key Vault Access Denied**: Check Managed Identity permissions
2. **Missing Configuration**: Verify environment-specific files
3. **Invalid Connection Strings**: Test connections during startup
4. **Azure AD Authentication**: Validate tenant and client IDs

### Debugging Tools

- Configuration endpoint for non-sensitive values
- Health check endpoints
- Application Insights configuration tracking
- Azure Key Vault access logging

## Configuration Change Management

### Change Process

1. Update configuration in appropriate environment file
2. Update Key Vault secrets if required
3. Test configuration changes in staging
4. Deploy to production with validation
5. Monitor for configuration-related issues

### Rollback Procedures

- Keep previous Key Vault secret versions
- Maintain configuration backups
- Document configuration dependencies
- Test rollback procedures regularly
