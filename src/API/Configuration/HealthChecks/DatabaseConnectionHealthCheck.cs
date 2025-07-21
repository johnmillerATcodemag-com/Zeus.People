using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using System.Data.SqlClient;

namespace Zeus.People.API.Configuration.HealthChecks;

/// <summary>
/// Health check for database connections
/// </summary>
public class DatabaseConnectionHealthCheck : IHealthCheck
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<DatabaseConnectionHealthCheck> _logger;

    public DatabaseConnectionHealthCheck(
        IConfigurationService configurationService,
        ILogger<DatabaseConnectionHealthCheck> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking database connection health");

            var dbConfig = await _configurationService.GetConfigurationAsync<DatabaseConfiguration>(
                DatabaseConfiguration.SectionName, cancellationToken);

            var healthData = new Dictionary<string, object>();
            var isHealthy = true;
            var errors = new List<string>();

            // Check write connection
            try
            {
                await CheckConnectionAsync(dbConfig.WriteConnectionString, "Write Database", cancellationToken);
                healthData["WriteDatabase"] = "Connected";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Write database connection failed");
                healthData["WriteDatabase"] = "Failed";
                errors.Add($"Write database: {ex.Message}");
                isHealthy = false;
            }

            // Check read connection
            try
            {
                await CheckConnectionAsync(dbConfig.ReadConnectionString, "Read Database", cancellationToken);
                healthData["ReadDatabase"] = "Connected";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Read database connection failed");
                healthData["ReadDatabase"] = "Failed";
                errors.Add($"Read database: {ex.Message}");
                isHealthy = false;
            }

            // Check event store connection
            try
            {
                await CheckConnectionAsync(dbConfig.EventStoreConnectionString, "Event Store Database", cancellationToken);
                healthData["EventStoreDatabase"] = "Connected";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Event store database connection failed");
                healthData["EventStoreDatabase"] = "Failed";
                errors.Add($"Event store database: {ex.Message}");
                isHealthy = false;
            }

            healthData["ConfigurationValid"] = true;

            if (isHealthy)
            {
                _logger.LogDebug("Database connection health check passed");
                return HealthCheckResult.Healthy("All database connections are working", healthData);
            }
            else
            {
                var errorMessage = string.Join("; ", errors);
                _logger.LogWarning("Database connection health check failed: {Errors}", errorMessage);
                return HealthCheckResult.Unhealthy(errorMessage, data: healthData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed with unexpected error");
            return HealthCheckResult.Unhealthy("Database health check failed", ex, new Dictionary<string, object>
            {
                ["ConfigurationValid"] = false,
                ["Error"] = ex.Message
            });
        }
    }

    private static async Task CheckConnectionAsync(string connectionString, string connectionName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            throw new InvalidOperationException($"{connectionName} connection string is not configured");
        }

        using var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT 1";
        command.CommandTimeout = 5; // 5 second timeout for health checks

        await command.ExecuteScalarAsync(cancellationToken);
    }
}
