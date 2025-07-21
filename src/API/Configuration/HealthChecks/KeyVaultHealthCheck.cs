using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Zeus.People.API.Configuration.HealthChecks;

/// <summary>
/// Health check for Key Vault access
/// </summary>
public class KeyVaultHealthCheck : IHealthCheck
{
    private readonly IConfigurationService _configurationService;
    private readonly ILogger<KeyVaultHealthCheck> _logger;

    public KeyVaultHealthCheck(
        IConfigurationService configurationService,
        ILogger<KeyVaultHealthCheck> logger)
    {
        _configurationService = configurationService ?? throw new ArgumentNullException(nameof(configurationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogDebug("Checking Key Vault access health");

            var keyVaultConfig = await _configurationService.GetConfigurationAsync<KeyVaultConfiguration>(
                KeyVaultConfiguration.SectionName, cancellationToken);

            var healthData = new Dictionary<string, object>
            {
                ["VaultName"] = keyVaultConfig.VaultName,
                ["VaultUrl"] = keyVaultConfig.VaultUrl,
                ["UseManagedIdentity"] = keyVaultConfig.UseManagedIdentity,
                ["SecretCachingEnabled"] = keyVaultConfig.EnableSecretCaching
            };

            // Validate configuration first
            try
            {
                keyVaultConfig.Validate();
                healthData["ConfigurationValid"] = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Key Vault configuration validation failed");
                healthData["ConfigurationValid"] = false;
                return HealthCheckResult.Unhealthy("Key Vault configuration is invalid", ex, healthData);
            }

            // If Key Vault URL is not configured, return degraded status
            if (string.IsNullOrEmpty(keyVaultConfig.VaultUrl))
            {
                healthData["AccessStatus"] = "Not Configured";
                _logger.LogWarning("Key Vault is not configured, using local configuration only");
                return HealthCheckResult.Degraded("Key Vault not configured, using local configuration");
            }

            // Test Key Vault access by trying to list secrets (metadata only)
            try
            {
                // Try to get a test secret to verify access
                // We'll use a known secret that should exist or handle the case where it doesn't
                var testResult = await TestKeyVaultAccessAsync(keyVaultConfig, cancellationToken);

                healthData["AccessStatus"] = testResult.IsAccessible ? "Accessible" : "Access Denied";
                healthData["AuthenticationMethod"] = keyVaultConfig.UseManagedIdentity ? "Managed Identity" : "Service Principal";
                healthData["SecretsListable"] = testResult.CanListSecrets;

                if (testResult.IsAccessible)
                {
                    _logger.LogDebug("Key Vault access health check passed");
                    return HealthCheckResult.Healthy("Key Vault is accessible", healthData);
                }
                else
                {
                    _logger.LogError("Key Vault access denied: {Error}", testResult.Error);
                    healthData["Error"] = testResult.Error;
                    return HealthCheckResult.Unhealthy("Key Vault access denied", new Exception(testResult.Error), healthData);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Key Vault access test failed");
                healthData["AccessStatus"] = "Failed";
                healthData["Error"] = ex.Message;

                return HealthCheckResult.Unhealthy("Key Vault access failed", ex, healthData);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Key Vault health check failed with unexpected error");
            return HealthCheckResult.Unhealthy("Key Vault health check failed", ex, new Dictionary<string, object>
            {
                ["ConfigurationValid"] = false,
                ["Error"] = ex.Message
            });
        }
    }

    private async Task<KeyVaultTestResult> TestKeyVaultAccessAsync(KeyVaultConfiguration config, CancellationToken cancellationToken)
    {
        try
        {
            // Create a temporary SecretClient to test access
            var credential = new Azure.Identity.DefaultAzureCredential(new Azure.Identity.DefaultAzureCredentialOptions
            {
                ManagedIdentityClientId = config.UseManagedIdentity ? config.ClientId : null,
                ExcludeManagedIdentityCredential = !config.UseManagedIdentity
            });

            var secretClient = new SecretClient(new Uri(config.VaultUrl), credential);

            // Test if we can list secrets (this requires List permission)
            try
            {
                var secretsPage = secretClient.GetPropertiesOfSecretsAsync(cancellationToken);
                await using var enumerator = secretsPage.GetAsyncEnumerator(cancellationToken);

                // Just try to get the first page to test access
                var hasSecrets = await enumerator.MoveNextAsync();

                return new KeyVaultTestResult
                {
                    IsAccessible = true,
                    CanListSecrets = true,
                    Error = null
                };
            }
            catch (Azure.RequestFailedException ex) when (ex.Status == 403)
            {
                // Access denied for listing, but vault might still be accessible for specific secret retrieval
                _logger.LogWarning("Cannot list Key Vault secrets (permission denied), but vault may still be accessible for specific secrets");

                return new KeyVaultTestResult
                {
                    IsAccessible = true,
                    CanListSecrets = false,
                    Error = "List permission denied, but vault is accessible"
                };
            }
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 401)
        {
            return new KeyVaultTestResult
            {
                IsAccessible = false,
                CanListSecrets = false,
                Error = "Authentication failed"
            };
        }
        catch (Azure.RequestFailedException ex) when (ex.Status == 403)
        {
            return new KeyVaultTestResult
            {
                IsAccessible = false,
                CanListSecrets = false,
                Error = "Access denied"
            };
        }
        catch (Exception ex)
        {
            return new KeyVaultTestResult
            {
                IsAccessible = false,
                CanListSecrets = false,
                Error = ex.Message
            };
        }
    }

    private class KeyVaultTestResult
    {
        public bool IsAccessible { get; set; }
        public bool CanListSecrets { get; set; }
        public string? Error { get; set; }
    }
}
