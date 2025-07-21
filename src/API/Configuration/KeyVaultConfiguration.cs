using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Zeus.People.API.Configuration;

/// <summary>
/// Azure Key Vault configuration settings
/// </summary>
public class KeyVaultConfiguration
{
    public const string SectionName = "KeyVaultSettings";

    /// <summary>
    /// Key Vault URL
    /// </summary>
    [Required(ErrorMessage = "Key Vault URL is required")]
    [Url(ErrorMessage = "Key Vault URL must be a valid URL")]
    public string VaultUrl { get; set; } = string.Empty;

    /// <summary>
    /// Key Vault name
    /// </summary>
    [Required(ErrorMessage = "Key Vault name is required")]
    public string VaultName { get; set; } = string.Empty;

    /// <summary>
    /// Client ID for managed identity authentication
    /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Client secret for service principal authentication (development only)
    /// </summary>
    [JsonIgnore] // Sensitive data should not be serialized
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Tenant ID for Azure AD authentication
    /// </summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Use managed identity for authentication
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Secret cache duration in minutes
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Secret cache duration must be between 1 minute and 24 hours")]
    public int SecretCacheDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Enable secret value caching
    /// </summary>
    public bool EnableSecretCaching { get; set; } = true;

    /// <summary>
    /// Retry attempts for Key Vault operations
    /// </summary>
    [Range(0, 10, ErrorMessage = "Retry attempts must be between 0 and 10")]
    public int RetryAttempts { get; set; } = 3;

    /// <summary>
    /// Retry delay in seconds
    /// </summary>
    [Range(1, 60, ErrorMessage = "Retry delay must be between 1 and 60 seconds")]
    public int RetryDelaySeconds { get; set; } = 2;

    /// <summary>
    /// Key Vault API timeout in seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Environment-specific secret prefix
    /// </summary>
    public string SecretPrefix { get; set; } = string.Empty;

    /// <summary>
    /// Validate the Key Vault configuration
    /// </summary>
    public void Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Key Vault configuration validation failed: {errors}");
        }

        // Additional business logic validation
        if (!VaultUrl.Contains("vault.azure.net"))
        {
            throw new InvalidOperationException("Key Vault URL must be a valid Azure Key Vault URL");
        }

        if (!UseManagedIdentity && string.IsNullOrEmpty(ClientSecret))
        {
            throw new InvalidOperationException("Client secret is required when not using managed identity");
        }

        if (!UseManagedIdentity && string.IsNullOrEmpty(TenantId))
        {
            throw new InvalidOperationException("Tenant ID is required when not using managed identity");
        }

        // Validate vault name format
        if (VaultName.Length < 3 || VaultName.Length > 24)
        {
            throw new InvalidOperationException("Vault name must be between 3 and 24 characters");
        }

        if (!System.Text.RegularExpressions.Regex.IsMatch(VaultName, "^[a-zA-Z0-9-]+$"))
        {
            throw new InvalidOperationException("Vault name can only contain alphanumeric characters and hyphens");
        }
    }

    /// <summary>
    /// Get the full secret name with prefix
    /// </summary>
    /// <param name="secretName">Base secret name</param>
    /// <returns>Full secret name with prefix</returns>
    public string GetSecretName(string secretName)
    {
        if (string.IsNullOrEmpty(SecretPrefix))
        {
            return secretName;
        }

        return $"{SecretPrefix}-{secretName}";
    }

    /// <summary>
    /// Get the Key Vault URI
    /// </summary>
    /// <returns>Key Vault URI</returns>
    public Uri GetVaultUri()
    {
        return new Uri(VaultUrl);
    }
}
