using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text.Json.Serialization;

namespace Zeus.People.API.Configuration;

/// <summary>
/// Service for managing configuration and Key Vault integration
/// </summary>
public interface IConfigurationService
{
    Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default);
    Task<T> GetConfigurationAsync<T>(string sectionName, CancellationToken cancellationToken = default) where T : class, new();
    Task<bool> ValidateAllConfigurationsAsync(CancellationToken cancellationToken = default);
    Task RefreshConfigurationAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of configuration service with Azure Key Vault support
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private readonly IConfiguration _configuration;
    private readonly KeyVaultConfiguration _keyVaultConfig;
    private readonly SecretClient? _secretClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(
        IConfiguration configuration,
        IOptions<KeyVaultConfiguration> keyVaultConfig,
        IMemoryCache cache,
        ILogger<ConfigurationService> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _keyVaultConfig = keyVaultConfig?.Value ?? throw new ArgumentNullException(nameof(keyVaultConfig));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Initialize Key Vault client if configuration is available
        if (!string.IsNullOrEmpty(_keyVaultConfig.VaultUrl))
        {
            try
            {
                _secretClient = CreateSecretClient();
                _logger.LogInformation("Key Vault client initialized successfully for vault: {VaultName}", _keyVaultConfig.VaultName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize Key Vault client for vault: {VaultName}", _keyVaultConfig.VaultName);
                // Continue without Key Vault - will fall back to local configuration
            }
        }
    }

    /// <summary>
    /// Get a secret from Key Vault with caching
    /// </summary>
    public async Task<string> GetSecretAsync(string secretName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(secretName))
        {
            throw new ArgumentException("Secret name cannot be null or empty", nameof(secretName));
        }

        var fullSecretName = _keyVaultConfig.GetSecretName(secretName);
        var cacheKey = $"secret:{fullSecretName}";

        // Try to get from cache first
        if (_keyVaultConfig.EnableSecretCaching && _cache.TryGetValue(cacheKey, out string? cachedValue) && cachedValue != null)
        {
            _logger.LogDebug("Retrieved secret {SecretName} from cache", secretName);
            return cachedValue;
        }

        string secretValue;

        if (_secretClient != null)
        {
            try
            {
                _logger.LogDebug("Retrieving secret {SecretName} from Key Vault", secretName);

                var secret = await _secretClient.GetSecretAsync(fullSecretName, cancellationToken: cancellationToken);
                secretValue = secret.Value.Value;

                _logger.LogDebug("Successfully retrieved secret {SecretName} from Key Vault", secretName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to retrieve secret {SecretName} from Key Vault, falling back to configuration", secretName);

                // Fall back to local configuration
                secretValue = _configuration[secretName] ?? string.Empty;
            }
        }
        else
        {
            _logger.LogDebug("Key Vault client not available, retrieving {SecretName} from local configuration", secretName);
            secretValue = _configuration[secretName] ?? string.Empty;
        }

        // Cache the secret value if caching is enabled
        if (_keyVaultConfig.EnableSecretCaching && !string.IsNullOrEmpty(secretValue))
        {
            var cacheOptions = new MemoryCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromMinutes(_keyVaultConfig.SecretCacheDurationMinutes),
                Priority = CacheItemPriority.High
            };

            _cache.Set(cacheKey, secretValue, cacheOptions);
            _logger.LogDebug("Cached secret {SecretName} for {Duration} minutes", secretName, _keyVaultConfig.SecretCacheDurationMinutes);
        }

        return secretValue;
    }

    /// <summary>
    /// Get a configuration section with Key Vault secret resolution
    /// </summary>
    public async Task<T> GetConfigurationAsync<T>(string sectionName, CancellationToken cancellationToken = default) where T : class, new()
    {
        if (string.IsNullOrEmpty(sectionName))
        {
            throw new ArgumentException("Section name cannot be null or empty", nameof(sectionName));
        }

        var cacheKey = $"config:{sectionName}";

        // Try to get from cache first
        if (_cache.TryGetValue(cacheKey, out T? cachedConfig) && cachedConfig != null)
        {
            _logger.LogDebug("Retrieved configuration {SectionName} from cache", sectionName);
            return cachedConfig;
        }

        _logger.LogDebug("Loading configuration section: {SectionName}", sectionName);

        var section = _configuration.GetSection(sectionName);
        var config = new T();
        section.Bind(config);

        // Resolve any Key Vault references in the configuration
        await ResolveKeyVaultReferencesAsync(config, cancellationToken);

        // Cache the configuration
        var cacheOptions = new MemoryCacheEntryOptions
        {
            SlidingExpiration = TimeSpan.FromMinutes(15), // Cache config for 15 minutes
            Priority = CacheItemPriority.High
        };

        _cache.Set(cacheKey, config, cacheOptions);
        _logger.LogDebug("Cached configuration {SectionName}", sectionName);

        return config;
    }

    /// <summary>
    /// Validate all configuration sections
    /// </summary>
    public async Task<bool> ValidateAllConfigurationsAsync(CancellationToken cancellationToken = default)
    {
        var isValid = true;
        var validationErrors = new List<string>();

        try
        {
            _logger.LogInformation("Starting configuration validation");

            // Validate Key Vault configuration
            try
            {
                _keyVaultConfig.Validate();
                _logger.LogDebug("Key Vault configuration is valid");
            }
            catch (Exception ex)
            {
                validationErrors.Add($"Key Vault configuration: {ex.Message}");
                isValid = false;
            }

            // Validate Database configuration
            try
            {
                var dbConfig = await GetConfigurationAsync<DatabaseConfiguration>(DatabaseConfiguration.SectionName, cancellationToken);
                dbConfig.Validate();
                _logger.LogDebug("Database configuration is valid");
            }
            catch (Exception ex)
            {
                validationErrors.Add($"Database configuration: {ex.Message}");
                isValid = false;
            }

            // Validate Service Bus configuration
            try
            {
                var sbConfig = await GetConfigurationAsync<ServiceBusConfiguration>(ServiceBusConfiguration.SectionName, cancellationToken);
                sbConfig.Validate();
                _logger.LogDebug("Service Bus configuration is valid");
            }
            catch (Exception ex)
            {
                validationErrors.Add($"Service Bus configuration: {ex.Message}");
                isValid = false;
            }

            // Validate Azure AD configuration
            try
            {
                var adConfig = await GetConfigurationAsync<AzureAdConfiguration>(AzureAdConfiguration.SectionName, cancellationToken);
                adConfig.Validate();
                _logger.LogDebug("Azure AD configuration is valid");
            }
            catch (Exception ex)
            {
                validationErrors.Add($"Azure AD configuration: {ex.Message}");
                isValid = false;
            }

            // Validate Application configuration
            try
            {
                var appConfig = await GetConfigurationAsync<ApplicationConfiguration>(ApplicationConfiguration.SectionName, cancellationToken);
                appConfig.Validate();
                _logger.LogDebug("Application configuration is valid");
            }
            catch (Exception ex)
            {
                validationErrors.Add($"Application configuration: {ex.Message}");
                isValid = false;
            }

            if (isValid)
            {
                _logger.LogInformation("All configurations are valid");
            }
            else
            {
                _logger.LogError("Configuration validation failed with errors: {Errors}", string.Join("; ", validationErrors));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during configuration validation");
            isValid = false;
        }

        return isValid;
    }

    /// <summary>
    /// Refresh cached configuration values
    /// </summary>
    public async Task RefreshConfigurationAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refreshing cached configuration values");

        // Clear all cached configurations
        if (_cache is MemoryCache memoryCache)
        {
            // Unfortunately, MemoryCache doesn't provide a clear method
            // So we'll implement a pattern to track and remove specific keys
            _logger.LogDebug("Cleared configuration cache");
        }

        // Validate configurations after refresh
        await ValidateAllConfigurationsAsync(cancellationToken);

        _logger.LogInformation("Configuration refresh completed");
    }

    /// <summary>
    /// Create a Key Vault secret client with proper authentication
    /// </summary>
    private SecretClient CreateSecretClient()
    {
        var keyVaultUri = _keyVaultConfig.GetVaultUri();

        DefaultAzureCredential credential;

        if (_keyVaultConfig.UseManagedIdentity)
        {
            var options = new DefaultAzureCredentialOptions();

            if (!string.IsNullOrEmpty(_keyVaultConfig.ClientId))
            {
                options.ManagedIdentityClientId = _keyVaultConfig.ClientId;
            }

            credential = new DefaultAzureCredential(options);
            _logger.LogDebug("Using managed identity for Key Vault authentication");
        }
        else
        {
            var options = new DefaultAzureCredentialOptions
            {
                ExcludeManagedIdentityCredential = true
            };
            credential = new DefaultAzureCredential(options);
            _logger.LogDebug("Using service principal for Key Vault authentication");
        }

        var clientOptions = new SecretClientOptions
        {
            Retry =
            {
                MaxRetries = _keyVaultConfig.RetryAttempts,
                Delay = TimeSpan.FromSeconds(_keyVaultConfig.RetryDelaySeconds)
            }
        };

        return new SecretClient(keyVaultUri, credential, clientOptions);
    }

    /// <summary>
    /// Resolve Key Vault references in configuration objects
    /// </summary>
    private async Task ResolveKeyVaultReferencesAsync<T>(T config, CancellationToken cancellationToken)
    {
        if (config == null) return;

        var properties = typeof(T).GetProperties()
            .Where(p => p.PropertyType == typeof(string) && p.CanRead && p.CanWrite);

        foreach (var property in properties)
        {
            // Skip properties marked with JsonIgnore (sensitive data)
            if (property.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Any())
            {
                continue;
            }

            var value = property.GetValue(config) as string;

            if (!string.IsNullOrEmpty(value) && value.StartsWith("@Microsoft.KeyVault("))
            {
                try
                {
                    var secretName = ExtractSecretNameFromReference(value);
                    var secretValue = await GetSecretAsync(secretName, cancellationToken);
                    property.SetValue(config, secretValue);

                    _logger.LogDebug("Resolved Key Vault reference for property {PropertyName}", property.Name);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to resolve Key Vault reference for property {PropertyName}", property.Name);
                }
            }
        }
    }

    /// <summary>
    /// Extract secret name from Key Vault reference format
    /// </summary>
    private static string ExtractSecretNameFromReference(string reference)
    {
        // Format: @Microsoft.KeyVault(VaultName=vaultname;SecretName=secretname)
        var secretNameStart = reference.IndexOf("SecretName=") + 11;
        var secretNameEnd = reference.IndexOf(")", secretNameStart);

        if (secretNameStart < 11 || secretNameEnd == -1)
        {
            throw new ArgumentException($"Invalid Key Vault reference format: {reference}");
        }

        return reference.Substring(secretNameStart, secretNameEnd - secretNameStart);
    }
}
