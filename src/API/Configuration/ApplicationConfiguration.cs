using System.ComponentModel.DataAnnotations;

namespace Zeus.People.API.Configuration;

/// <summary>
/// Application configuration settings with feature flags
/// </summary>
public class ApplicationConfiguration
{
    public const string SectionName = "ApplicationSettings";

    /// <summary>
    /// Application name
    /// </summary>
    [Required(ErrorMessage = "Application name is required")]
    public string ApplicationName { get; set; } = "Academic Management System";

    /// <summary>
    /// Application version
    /// </summary>
    [Required(ErrorMessage = "Application version is required")]
    public string Version { get; set; } = "1.0.0";

    /// <summary>
    /// Environment name (Development, Staging, Production)
    /// </summary>
    [Required(ErrorMessage = "Environment is required")]
    public string Environment { get; set; } = "Development";

    /// <summary>
    /// Application description
    /// </summary>
    public string Description { get; set; } = "Zeus.People Academic Management System API";

    /// <summary>
    /// Contact email for support
    /// </summary>
    [EmailAddress(ErrorMessage = "Support email must be a valid email address")]
    public string SupportEmail { get; set; } = string.Empty;

    /// <summary>
    /// Feature flags for enabling/disabling functionality
    /// </summary>
    public FeatureFlags Features { get; set; } = new();

    /// <summary>
    /// Performance settings
    /// </summary>
    public PerformanceSettings Performance { get; set; } = new();

    /// <summary>
    /// Security settings
    /// </summary>
    public SecuritySettings Security { get; set; } = new();

    /// <summary>
    /// Validate the application configuration
    /// </summary>
    public void Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Application configuration validation failed: {errors}");
        }

        // Validate nested objects
        Features.Validate();
        Performance.Validate();
        Security.Validate();

        // Additional business logic validation
        var validEnvironments = new[] { "Development", "Staging", "Production" };
        if (!validEnvironments.Contains(Environment))
        {
            throw new InvalidOperationException($"Environment must be one of: {string.Join(", ", validEnvironments)}");
        }

        if (!Version.Contains('.'))
        {
            throw new InvalidOperationException("Version should follow semantic versioning (e.g., 1.0.0)");
        }
    }
}

/// <summary>
/// Feature flags for controlling application functionality
/// </summary>
public class FeatureFlags
{
    /// <summary>
    /// Enable detailed API logging
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Enable Swagger UI in production
    /// </summary>
    public bool EnableSwaggerInProduction { get; set; } = false;

    /// <summary>
    /// Enable health checks endpoint
    /// </summary>
    public bool EnableHealthChecks { get; set; } = true;

    /// <summary>
    /// Enable metrics collection
    /// </summary>
    public bool EnableMetrics { get; set; } = true;

    /// <summary>
    /// Enable request/response caching
    /// </summary>
    public bool EnableCaching { get; set; } = true;

    /// <summary>
    /// Enable rate limiting
    /// </summary>
    public bool EnableRateLimiting { get; set; } = true;

    /// <summary>
    /// Enable CORS
    /// </summary>
    public bool EnableCors { get; set; } = true;

    /// <summary>
    /// Enable API versioning
    /// </summary>
    public bool EnableApiVersioning { get; set; } = true;

    /// <summary>
    /// Enable background services
    /// </summary>
    public bool EnableBackgroundServices { get; set; } = true;

    /// <summary>
    /// Enable audit logging
    /// </summary>
    public bool EnableAuditLogging { get; set; } = true;

    /// <summary>
    /// Enable experimental features
    /// </summary>
    public bool EnableExperimentalFeatures { get; set; } = false;

    /// <summary>
    /// Validate feature flags
    /// </summary>
    public void Validate()
    {
        // Feature flags are typically boolean and don't require complex validation
        // But we can add business rules here if needed
    }
}

/// <summary>
/// Performance-related settings
/// </summary>
public class PerformanceSettings
{
    /// <summary>
    /// Default cache duration in minutes
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Default cache duration must be between 1 minute and 24 hours")]
    public int DefaultCacheDurationMinutes { get; set; } = 15;

    /// <summary>
    /// Read model cache duration in minutes
    /// </summary>
    [Range(1, 1440, ErrorMessage = "Read model cache duration must be between 1 minute and 24 hours")]
    public int ReadModelCacheDurationMinutes { get; set; } = 30;

    /// <summary>
    /// Maximum request size in MB
    /// </summary>
    [Range(1, 100, ErrorMessage = "Maximum request size must be between 1 and 100 MB")]
    public int MaxRequestSizeMB { get; set; } = 10;

    /// <summary>
    /// Request timeout in seconds
    /// </summary>
    [Range(1, 300, ErrorMessage = "Request timeout must be between 1 and 300 seconds")]
    public int RequestTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Maximum concurrent connections
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Maximum concurrent connections must be between 1 and 1000")]
    public int MaxConcurrentConnections { get; set; } = 100;

    /// <summary>
    /// Validate performance settings
    /// </summary>
    public void Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Performance settings validation failed: {errors}");
        }
    }
}

/// <summary>
/// Security-related settings
/// </summary>
public class SecuritySettings
{
    /// <summary>
    /// Require HTTPS for all requests
    /// </summary>
    public bool RequireHttps { get; set; } = true;

    /// <summary>
    /// Enable HSTS (HTTP Strict Transport Security)
    /// </summary>
    public bool EnableHsts { get; set; } = true;

    /// <summary>
    /// HSTS max age in seconds
    /// </summary>
    [Range(1, 31536000, ErrorMessage = "HSTS max age must be between 1 second and 1 year")]
    public int HstsMaxAgeSeconds { get; set; } = 31536000; // 1 year

    /// <summary>
    /// Enable X-Frame-Options header
    /// </summary>
    public bool EnableFrameOptions { get; set; } = true;

    /// <summary>
    /// Enable X-Content-Type-Options header
    /// </summary>
    public bool EnableContentTypeOptions { get; set; } = true;

    /// <summary>
    /// Enable Referrer Policy header
    /// </summary>
    public bool EnableReferrerPolicy { get; set; } = true;

    /// <summary>
    /// Content Security Policy header value
    /// </summary>
    public string ContentSecurityPolicy { get; set; } = "default-src 'self'";

    /// <summary>
    /// API key header name for API key authentication
    /// </summary>
    public string ApiKeyHeaderName { get; set; } = "X-API-Key";

    /// <summary>
    /// Enable request body size limits
    /// </summary>
    public bool EnableRequestSizeLimits { get; set; } = true;

    /// <summary>
    /// Validate security settings
    /// </summary>
    public void Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Security settings validation failed: {errors}");
        }

        // Additional security validation
        if (EnableHsts && HstsMaxAgeSeconds < 300)
        {
            throw new InvalidOperationException("HSTS max age should be at least 5 minutes for security effectiveness");
        }

        if (string.IsNullOrWhiteSpace(ContentSecurityPolicy))
        {
            throw new InvalidOperationException("Content Security Policy cannot be empty when security features are enabled");
        }
    }
}
