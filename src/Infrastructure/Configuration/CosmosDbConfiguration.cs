namespace Zeus.People.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Cosmos DB
/// </summary>
public class CosmosDbConfiguration
{
    public const string SectionName = "CosmosDb";

    public string DatabaseName { get; set; } = "Zeus.People";
    public string Endpoint { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
    public bool UseManagedIdentity { get; set; } = true;
    public int RequestTimeoutInSeconds { get; set; } = 30;
    public int MaxRetryAttemptsOnRateLimitedRequests { get; set; } = 3;
    public int MaxRetryWaitTimeInSeconds { get; set; } = 30;
}
