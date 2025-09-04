using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Serilog;

namespace Zeus.People.API.Configuration
{
    public static class SecretsConfigurationExtensions
    {
        /// <summary>
        /// Configure secrets management supporting environment variables and Azure Key Vault
        /// </summary>
        public static void ConfigureSecretsManagement(this WebApplicationBuilder builder)
        {
            var configuration = builder.Configuration;
            var environment = builder.Environment;

            // 1. First priority: Environment variables (for development)
            ConfigureEnvironmentVariables(builder);

            // 2. Second priority: Azure Key Vault (for production/staging)
            if (!environment.IsDevelopment())
            {
                ConfigureAzureKeyVault(builder);
            }

            // 3. Third priority: Development overrides
            if (environment.IsDevelopment())
            {
                ConfigureDevelopmentOverrides(builder);
            }
        }

        private static void ConfigureEnvironmentVariables(WebApplicationBuilder builder)
        {
            Log.Information("Configuring environment variable-based secrets");

            // Map environment variables to configuration paths
            var environmentMappings = new Dictionary<string, string>
            {
                // JWT Settings
                { "JWT_SECRET_KEY", "JwtSettings:SecretKey" },

                // Azure AD
                { "AZURE_AD_TENANT_ID", "AzureAd:TenantId" },
                { "AZURE_AD_CLIENT_ID", "AzureAd:ClientId" },
                { "AZURE_AD_CLIENT_SECRET", "AzureAd:ClientSecret" },

                // Database Connections
                { "DATABASE_CONNECTION_STRING", "ConnectionStrings:AcademicDatabase" },
                { "EVENT_STORE_CONNECTION_STRING", "ConnectionStrings:EventStoreDatabase" },

                // Service Bus
                { "SERVICE_BUS_CONNECTION_STRING", "ConnectionStrings:ServiceBus" },

                // Application Insights
                { "APPLICATION_INSIGHTS_CONNECTION_STRING", "ApplicationInsights:ConnectionString" },
                { "APPLICATION_INSIGHTS_INSTRUMENTATION_KEY", "ApplicationInsights:InstrumentationKey" }
            };

            // Create in-memory configuration dictionary
            var configValues = new Dictionary<string, string?>();

            foreach (var mapping in environmentMappings)
            {
                var envValue = Environment.GetEnvironmentVariable(mapping.Key);
                if (!string.IsNullOrEmpty(envValue))
                {
                    configValues[mapping.Value] = envValue;
                    Log.Information("Configured {ConfigPath} from environment variable {EnvVar}",
                        mapping.Value, mapping.Key);
                }
            }

            // Add the configuration values
            if (configValues.Any())
            {
                builder.Configuration.AddInMemoryCollection(configValues);
            }
        }

        private static void ConfigureAzureKeyVault(WebApplicationBuilder builder)
        {
            try
            {
                var keyVaultUrl = builder.Configuration["KeyVaultSettings:VaultUrl"];
                var clientId = builder.Configuration["KeyVaultSettings:ClientId"];
                var useManagedIdentity = builder.Configuration.GetValue<bool>("KeyVaultSettings:UseManagedIdentity", true);

                if (string.IsNullOrEmpty(keyVaultUrl))
                {
                    Log.Warning("Key Vault URL not configured, skipping Key Vault integration");
                    return;
                }

                Log.Information("Configuring Azure Key Vault integration with URL: {KeyVaultUrl}", keyVaultUrl);

                // Configure credential based on environment
                DefaultAzureCredential credential;
                if (useManagedIdentity)
                {
                    credential = new DefaultAzureCredential();
                    Log.Information("Using Managed Identity for Key Vault authentication");
                }
                else if (!string.IsNullOrEmpty(clientId))
                {
                    var options = new DefaultAzureCredentialOptions
                    {
                        ManagedIdentityClientId = clientId
                    };
                    credential = new DefaultAzureCredential(options);
                    Log.Information("Using User-Assigned Managed Identity for Key Vault authentication");
                }
                else
                {
                    credential = new DefaultAzureCredential();
                    Log.Information("Using Default Azure Credential for Key Vault authentication");
                }

                // Create Key Vault client
                var secretClient = new SecretClient(new Uri(keyVaultUrl), credential);

                // Add Azure Key Vault configuration
                builder.Configuration.AddAzureKeyVault(secretClient, new KeyVaultSecretManager());

                Log.Information("Successfully configured Azure Key Vault integration");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to configure Azure Key Vault integration");
                // Don't throw - allow application to continue with other configuration sources
            }
        }

        private static void ConfigureDevelopmentOverrides(WebApplicationBuilder builder)
        {
            Log.Information("Applying development configuration overrides");

            var devOverrides = new Dictionary<string, string?>
            {
                // Override any missing required values with development defaults
            };

            // Check for missing critical configuration and provide development defaults
            var jwtSecret = builder.Configuration["JwtSettings:SecretKey"];
            if (string.IsNullOrEmpty(jwtSecret) || jwtSecret.Contains("REPLACE_WITH"))
            {
                Log.Warning("JWT Secret not configured, generating development secret");
                devOverrides["JwtSettings:SecretKey"] = GenerateSecureKey(64);
            }

            var dbConnection = builder.Configuration["ConnectionStrings:AcademicDatabase"];
            if (string.IsNullOrEmpty(dbConnection) || dbConnection.Contains("REPLACE_WITH"))
            {
                Log.Information("Using default LocalDB connection for development");
                devOverrides["ConnectionStrings:AcademicDatabase"] = "Server=(localdb)\\MSSQLLocalDB;Database=Zeus.People.Academic.Dev;Trusted_Connection=True;MultipleActiveResultSets=true";
            }

            var eventStoreConnection = builder.Configuration["ConnectionStrings:EventStoreDatabase"];
            if (string.IsNullOrEmpty(eventStoreConnection) || eventStoreConnection.Contains("REPLACE_WITH"))
            {
                Log.Information("Using default LocalDB connection for Event Store development");
                devOverrides["ConnectionStrings:EventStoreDatabase"] = "Server=(localdb)\\MSSQLLocalDB;Database=Zeus.People.EventStore.Dev;Trusted_Connection=True;MultipleActiveResultSets=true";
            }

            var serviceBusConnection = builder.Configuration["ConnectionStrings:ServiceBus"];
            if (string.IsNullOrEmpty(serviceBusConnection) || serviceBusConnection.Contains("REPLACE_WITH"))
            {
                Log.Information("Using in-memory Service Bus for development");
                devOverrides["ConnectionStrings:ServiceBus"] = "UseDevelopmentInMemory";
            }

            if (devOverrides.Any())
            {
                builder.Configuration.AddInMemoryCollection(devOverrides);
            }
        }

        private static string GenerateSecureKey(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }

    /// <summary>
    /// Custom Key Vault secret manager to handle secret name mapping
    /// </summary>
    public class KeyVaultSecretManager : Azure.Extensions.AspNetCore.Configuration.Secrets.KeyVaultSecretManager
    {
        public override bool Load(SecretProperties secret)
        {
            // Load all secrets by default
            return true;
        }

        public override string GetKey(KeyVaultSecret secret)
        {
            // Map Key Vault secret names to configuration keys
            var secretName = secret.Name;

            // Convert kebab-case or underscore to configuration hierarchy
            return secretName
                .Replace("--", ConfigurationPath.KeyDelimiter)
                .Replace("-", ConfigurationPath.KeyDelimiter)
                .Replace("_", ConfigurationPath.KeyDelimiter);
        }
    }
}
