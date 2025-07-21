using Microsoft.Extensions.Diagnostics.HealthChecks;
using Zeus.People.API.Configuration;

namespace Zeus.People.API.Configuration.HealthChecks;

/// <summary>
/// Health check for Azure AD configuration and connectivity
/// </summary>
public class AzureAdHealthCheck : IHealthCheck
{
    private readonly IConfigurationService _configurationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AzureAdHealthCheck> _logger;

    public AzureAdHealthCheck(
        IConfigurationService configurationService,
        IHttpClientFactory httpClientFactory,
        ILogger<AzureAdHealthCheck> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Azure AD configuration health");

            var azureAdConfig = await _configurationService.GetConfigurationAsync<AzureAdConfiguration>(
                AzureAdConfiguration.SectionName, cancellationToken);

            var healthData = new Dictionary<string, object>
            {
                ["Instance"] = azureAdConfig.Instance ?? "Not configured",
                ["Domain"] = azureAdConfig.Domain ?? "Not configured",
                ["ClientId"] = !string.IsNullOrEmpty(azureAdConfig.ClientId) ? "Configured" : "Not configured",
                ["ClientSecret"] = !string.IsNullOrEmpty(azureAdConfig.ClientSecret) ? "Configured" : "Not configured",
                ["SignUpSignInPolicyId"] = azureAdConfig.SignUpSignInPolicyId ?? "Not configured"
            };

            // Validate configuration first
            try
            {
                azureAdConfig.Validate();
                healthData["ConfigurationValid"] = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Azure AD configuration validation failed");
                healthData["ConfigurationValid"] = false;
                healthData["ValidationError"] = ex.Message;
                return HealthCheckResult.Unhealthy("Azure AD configuration is invalid", ex);
            }

            // Test Azure AD endpoint accessibility
            if (!string.IsNullOrEmpty(azureAdConfig.Instance))
            {
                var endpointTest = await TestAzureAdEndpointAsync(azureAdConfig, cancellationToken);
                healthData["EndpointAccessible"] = endpointTest.IsSuccess;

                if (endpointTest.IsSuccess)
                {
                    _logger.LogDebug("Azure AD configuration health check passed");
                    return HealthCheckResult.Healthy("Azure AD configuration is valid and endpoint is accessible");
                }
                else
                {
                    _logger.LogWarning("Azure AD endpoint is not accessible: {Error}", endpointTest.Error);
                    return HealthCheckResult.Degraded("Azure AD configuration is valid but endpoint is not accessible", new Exception(endpointTest.Error ?? "Unknown error"));
                }
            }
            else
            {
                _logger.LogWarning("Azure AD instance URL not configured");
                return HealthCheckResult.Degraded("Azure AD instance URL not configured");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AD health check failed with unexpected error");
            return HealthCheckResult.Unhealthy("Azure AD health check failed", ex);
        }
    }

    private async Task<(bool IsSuccess, string? Error)> TestAzureAdEndpointAsync(
        AzureAdConfiguration config, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(10);

            // Test well-known endpoint for B2C
            var wellKnownUrl = $"{config.Instance?.TrimEnd('/')}/{config.Domain}/v2.0/.well-known/openid-configuration?p={config.SignUpSignInPolicyId}";

            _logger.LogDebug("Testing Azure AD endpoint: {Url}", wellKnownUrl);

            using var response = await httpClient.GetAsync(wellKnownUrl, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Azure AD endpoint test successful");
                return (true, null);
            }
            else
            {
                var error = $"HTTP {response.StatusCode}: {response.ReasonPhrase}";
                _logger.LogWarning("Azure AD endpoint test failed: {Error}", error);
                return (false, error);
            }
        }
        catch (TaskCanceledException)
        {
            const string error = "Request timeout";
            _logger.LogWarning("Azure AD endpoint test timed out");
            return (false, error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure AD endpoint test failed");
            return (false, ex.Message);
        }
    }
}
