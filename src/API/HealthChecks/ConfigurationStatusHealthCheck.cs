using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Zeus.People.API.Configuration;
using System.Text;

namespace Zeus.People.API.HealthChecks;

/// <summary>
/// Health check that reports configuration status correctly by validating all configuration classes
/// </summary>
public class ConfigurationStatusHealthCheck : IHealthCheck
{
    private readonly IOptions<DatabaseConfiguration> _databaseConfig;
    private readonly IOptions<ServiceBusConfiguration> _serviceBusConfig;
    private readonly IOptions<AzureAdConfiguration> _azureAdConfig;
    private readonly IOptions<ApplicationConfiguration> _applicationConfig;
    private readonly ILogger<ConfigurationStatusHealthCheck> _logger;

    public ConfigurationStatusHealthCheck(
        IOptions<DatabaseConfiguration> databaseConfig,
        IOptions<ServiceBusConfiguration> serviceBusConfig,
        IOptions<AzureAdConfiguration> azureAdConfig,
        IOptions<ApplicationConfiguration> applicationConfig,
        ILogger<ConfigurationStatusHealthCheck> logger)
    {
        _databaseConfig = databaseConfig ?? throw new ArgumentNullException(nameof(databaseConfig));
        _serviceBusConfig = serviceBusConfig ?? throw new ArgumentNullException(nameof(serviceBusConfig));
        _azureAdConfig = azureAdConfig ?? throw new ArgumentNullException(nameof(azureAdConfig));
        _applicationConfig = applicationConfig ?? throw new ArgumentNullException(nameof(applicationConfig));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Starting configuration status health check");

            var data = new Dictionary<string, object>();
            var errors = new List<string>();
            var configSummary = new StringBuilder();

            // Validate Database Configuration
            var (dbValid, dbErrors, dbSummary) = await ValidateDatabaseConfigurationAsync(cancellationToken);
            data["database_config"] = dbValid ? "Valid" : $"Invalid: {string.Join(", ", dbErrors)}";
            if (!dbValid) errors.AddRange(dbErrors);
            configSummary.AppendLine($"Database: {dbSummary}");

            // Validate Service Bus Configuration
            var (sbValid, sbErrors, sbSummary) = await ValidateServiceBusConfigurationAsync(cancellationToken);
            data["servicebus_config"] = sbValid ? "Valid" : $"Invalid: {string.Join(", ", sbErrors)}";
            if (!sbValid) errors.AddRange(sbErrors);
            configSummary.AppendLine($"ServiceBus: {sbSummary}");

            // Validate Azure AD Configuration
            var (adValid, adErrors, adSummary) = await ValidateAzureAdConfigurationAsync(cancellationToken);
            data["azuread_config"] = adValid ? "Valid" : $"Invalid: {string.Join(", ", adErrors)}";
            if (!adValid) errors.AddRange(adErrors);
            configSummary.AppendLine($"Azure AD: {adSummary}");

            // Validate Application Configuration
            var (appValid, appErrors, appSummary) = await ValidateApplicationConfigurationAsync(cancellationToken);
            data["application_config"] = appValid ? "Valid" : $"Invalid: {string.Join(", ", appErrors)}";
            if (!appValid) errors.AddRange(appErrors);
            configSummary.AppendLine($"Application: {appSummary}");

            // Add summary information
            data["configuration_summary"] = configSummary.ToString().Trim();
            data["validation_timestamp"] = DateTime.UtcNow;

            // Determine overall health status
            if (errors.Count == 0)
            {
                _logger.LogInformation("All configurations are valid");
                return HealthCheckResult.Healthy(
                    "All configurations are valid and properly loaded",
                    data);
            }
            else
            {
                var errorMessage = $"Configuration validation failed: {string.Join("; ", errors)}";
                _logger.LogWarning("Configuration validation failed with {ErrorCount} errors: {Errors}",
                    errors.Count, string.Join("; ", errors));

                return HealthCheckResult.Unhealthy(
                    errorMessage,
                    data: data);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Configuration status health check was cancelled");
            return HealthCheckResult.Unhealthy("Health check was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during configuration status health check");
            return HealthCheckResult.Unhealthy(
                "Unexpected error during configuration validation",
                ex);
        }
    }

    private async Task<(bool IsValid, List<string> Errors, string Summary)> ValidateDatabaseConfigurationAsync(CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make async for consistency

        try
        {
            var config = _databaseConfig.Value;
            config.Validate(); // This will throw if invalid

            var summary = $"Command timeout {config.CommandTimeoutSeconds}s, Max retries {config.MaxRetryCount}";
            return (true, new List<string>(), summary);
        }
        catch (Exception ex)
        {
            return (false, new List<string> { ex.Message }, "Configuration error");
        }
    }

    private async Task<(bool IsValid, List<string> Errors, string Summary)> ValidateServiceBusConfigurationAsync(CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make async for consistency

        try
        {
            var config = _serviceBusConfig.Value;
            config.Validate(); // This will throw if invalid

            var authType = config.UseManagedIdentity ? "Managed Identity" : "Connection String";
            var summary = $"Topic '{config.TopicName}', Subscription '{config.SubscriptionName}', Auth: {authType}";
            return (true, new List<string>(), summary);
        }
        catch (Exception ex)
        {
            return (false, new List<string> { ex.Message }, "Configuration error");
        }
    }

    private async Task<(bool IsValid, List<string> Errors, string Summary)> ValidateAzureAdConfigurationAsync(CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make async for consistency

        try
        {
            var config = _azureAdConfig.Value;
            config.Validate(); // This will throw if invalid

            var summary = $"Tenant '{config.TenantId}', Client '{config.ClientId}', Token caching: {config.EnableTokenCaching}";
            return (true, new List<string>(), summary);
        }
        catch (Exception ex)
        {
            return (false, new List<string> { ex.Message }, "Configuration error");
        }
    }

    private async Task<(bool IsValid, List<string> Errors, string Summary)> ValidateApplicationConfigurationAsync(CancellationToken cancellationToken)
    {
        await Task.Yield(); // Make async for consistency

        try
        {
            var config = _applicationConfig.Value;
            config.Validate(); // This will throw if invalid

            var summary = $"Environment '{config.Environment}', Version '{config.Version}', Health checks: {config.Features.EnableHealthChecks}";
            return (true, new List<string>(), summary);
        }
        catch (Exception ex)
        {
            return (false, new List<string> { ex.Message }, "Configuration error");
        }
    }
}
