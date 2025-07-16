using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Zeus.People.Infrastructure.Configuration;

namespace Zeus.People.Infrastructure.HealthChecks;

/// <summary>
/// Health check for Cosmos DB connectivity
/// </summary>
public class CosmosDbHealthCheck : IHealthCheck
{
    private readonly CosmosClient _cosmosClient;
    private readonly CosmosDbConfiguration _configuration;

    public CosmosDbHealthCheck(
        CosmosClient cosmosClient,
        IOptions<CosmosDbConfiguration> configuration)
    {
        _cosmosClient = cosmosClient ?? throw new ArgumentNullException(nameof(cosmosClient));
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to read database properties to check connectivity
            var database = _cosmosClient.GetDatabase(_configuration.DatabaseName);
            await database.ReadAsync(cancellationToken: cancellationToken);

            return HealthCheckResult.Healthy("Cosmos DB connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Cosmos DB connection failed",
                ex);
        }
    }
}
