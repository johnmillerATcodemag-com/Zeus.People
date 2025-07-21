using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Zeus.People.API.Configuration;

/// <summary>
/// Azure AD B2C configuration settings with authentication support
/// </summary>
public class AzureAdConfiguration
{
    public const string SectionName = "AzureAd";

    /// <summary>
    /// Azure AD instance URL
    /// </summary>
    [Required(ErrorMessage = "Azure AD instance is required")]
    [Url(ErrorMessage = "Azure AD instance must be a valid URL")]
    public string Instance { get; set; } = "https://login.microsoftonline.com/";

    /// <summary>
    /// Azure AD tenant ID
    /// </summary>
    [Required(ErrorMessage = "Azure AD tenant ID is required")]
    public string TenantId { get; set; } = string.Empty;

    /// <summary>
    /// Application client ID
    /// </summary>
    [Required(ErrorMessage = "Client ID is required")]
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Application client secret
    /// </summary>
    [JsonIgnore] // Sensitive data should not be serialized
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>
    /// Token audience
    /// </summary>
    [Required(ErrorMessage = "Audience is required")]
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// Valid token issuers
    /// </summary>
    public List<string> ValidIssuers { get; set; } = new();

    /// <summary>
    /// Domain name for Azure AD B2C
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Sign-up or sign-in policy name
    /// </summary>
    public string SignUpSignInPolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Reset password policy name
    /// </summary>
    public string ResetPasswordPolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Edit profile policy name
    /// </summary>
    public string EditProfilePolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Enable token caching
    /// </summary>
    public bool EnableTokenCaching { get; set; } = true;

    /// <summary>
    /// Token cache duration in minutes
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Token cache duration must be between 1 minute and 24 hours")]
    public int TokenCacheDurationMinutes { get; set; } = 60;

    /// <summary>
    /// Clock skew tolerance for token validation
    /// </summary>
    public TimeSpan ClockSkew { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Validate the Azure AD configuration
    /// </summary>
    public void Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Azure AD configuration validation failed: {errors}");
        }

        // Additional business logic validation
        if (!string.IsNullOrEmpty(Domain) && !Domain.Contains(".onmicrosoft.com") && !Domain.Contains(".b2clogin.com"))
        {
            throw new InvalidOperationException("Domain should be a valid Azure AD B2C domain");
        }

        if (ValidIssuers.Any() && ValidIssuers.Any(issuer => !Uri.IsWellFormedUriString(issuer, UriKind.Absolute)))
        {
            throw new InvalidOperationException("All valid issuers must be well-formed absolute URIs");
        }

        if (ClockSkew > TimeSpan.FromMinutes(30))
        {
            throw new InvalidOperationException("Clock skew should not exceed 30 minutes for security reasons");
        }
    }

    /// <summary>
    /// Get the authority URL for the tenant
    /// </summary>
    public string GetAuthority()
    {
        if (string.IsNullOrEmpty(Domain))
        {
            return $"{Instance.TrimEnd('/')}/{TenantId}/";
        }

        return $"https://{Domain}.b2clogin.com/{Domain}.onmicrosoft.com/{SignUpSignInPolicyId}/";
    }

    /// <summary>
    /// Check if this is an Azure AD B2C configuration
    /// </summary>
    public bool IsB2C => !string.IsNullOrEmpty(Domain);
}
