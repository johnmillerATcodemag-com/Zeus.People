using Microsoft.Extensions.Configuration;
using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Zeus.People.API.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

namespace Zeus.People.API.Configuration;

public static class ConfigurationExtensions
{
    /// <summary>
    /// Configures comprehensive application configuration with Azure Key Vault integration
    /// </summary>
    public static void ConfigureAppConfiguration(this WebApplicationBuilder builder)
    {
        // Add configuration sources in order of precedence (highest last)
        builder.Configuration.Sources.Clear();

        // 1. Default configuration sources
        builder.Configuration
            .SetBasePath(builder.Environment.ContentRootPath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

        // 2. Azure Key Vault (if configured)
        if (!builder.Environment.IsDevelopment() || !string.IsNullOrEmpty(builder.Configuration["KeyVault:VaultUrl"]))
        {
            AddAzureKeyVault(builder);
        }

        // 3. Environment variables (highest precedence)
        builder.Configuration.AddEnvironmentVariables();

        // Register configuration validation services
        builder.Services.Configure<DatabaseConfiguration>(builder.Configuration.GetSection(DatabaseConfiguration.SectionName));
        builder.Services.Configure<ServiceBusConfiguration>(builder.Configuration.GetSection(ServiceBusConfiguration.SectionName));
        builder.Services.Configure<AzureAdConfiguration>(builder.Configuration.GetSection(AzureAdConfiguration.SectionName));
        builder.Services.Configure<ApplicationConfiguration>(builder.Configuration.GetSection(ApplicationConfiguration.SectionName));
        builder.Services.Configure<KeyVaultConfiguration>(builder.Configuration.GetSection(KeyVaultConfiguration.SectionName));

        // Add configuration services
        builder.Services.AddSingleton<IConfigurationService, ConfigurationService>();

        // Add health checks for configuration dependencies
        builder.Services.AddHealthChecks()
            .AddCheck<Zeus.People.API.HealthChecks.ConfigurationStatusHealthCheck>("configuration", tags: new[] { "configuration", "validation", "ready" });
        // TODO: Add specific health checks when they are implemented and compilation errors are fixed
        // .AddCheck<DatabaseHealthCheck>("database", HealthStatus.Unhealthy, new[] { "database", "sql", "ready" })
        // .AddCheck<ServiceBusHealthCheck>("servicebus", HealthStatus.Unhealthy, new[] { "servicebus", "messaging", "ready" })
        // .AddCheck<KeyVaultHealthCheck>("keyvault", HealthStatus.Unhealthy, new[] { "keyvault", "secrets", "ready" })
        // .AddCheck<AzureAdHealthCheck>("azuread", HealthStatus.Unhealthy, new[] { "authentication", "identity", "ready" });

        // Add memory cache for configuration caching
        builder.Services.AddMemoryCache();

        // Add HTTP client for external dependencies
        builder.Services.AddHttpClient();

        // Configure application insights
        if (!string.IsNullOrEmpty(builder.Configuration["ApplicationInsights:ConnectionString"]))
        {
            builder.Services.AddApplicationInsightsTelemetry();
        }
    }

    /// <summary>
    /// Adds Azure Key Vault as a configuration source
    /// </summary>
    private static void AddAzureKeyVault(WebApplicationBuilder builder)
    {
        try
        {
            var keyVaultUrl = builder.Configuration["KeyVault:VaultUrl"];
            if (string.IsNullOrEmpty(keyVaultUrl))
            {
                Console.WriteLine("Warning: KeyVault:VaultUrl not configured. Skipping Key Vault configuration.");
                return;
            }

            var keyVaultUri = new Uri(keyVaultUrl);

            // Use managed identity in production, development credentials locally
            var credential = builder.Environment.IsDevelopment()
                ? new DefaultAzureCredential(new DefaultAzureCredentialOptions
                {
                    ExcludeEnvironmentCredential = false,
                    ExcludeAzureCliCredential = false,
                    ExcludeAzurePowerShellCredential = false,
                    ExcludeVisualStudioCredential = true,
                    ExcludeVisualStudioCodeCredential = true,
                    ExcludeInteractiveBrowserCredential = true,
                    ExcludeManagedIdentityCredential = true
                })
                : new DefaultAzureCredential();

            // Create Key Vault client
            var secretClient = new SecretClient(keyVaultUri, credential);

            // Add Key Vault configuration provider
            builder.Configuration.AddAzureKeyVault(secretClient, new AzureKeyVaultConfigurationOptions
            {
                Manager = new DefaultKeyVaultSecretManager(),
                ReloadInterval = TimeSpan.FromMinutes(5) // Reload secrets every 5 minutes
            });

            Console.WriteLine($"Successfully configured Azure Key Vault: {keyVaultUrl}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Warning: Failed to configure Azure Key Vault: {ex.Message}");
            // Don't fail startup if Key Vault is unavailable in development
            if (!builder.Environment.IsDevelopment())
            {
                throw;
            }
        }
    }

    /// <summary>
    /// Validates all configuration sections and their dependencies
    /// </summary>
    public static async Task ValidateConfigurationAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

        try
        {
            logger.LogInformation("Starting configuration validation...");

            // Skip strict validation in test environments
            var environment = app.Environment;
            var isTestEnvironment = environment.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase) ||
                                  environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase);

            if (isTestEnvironment)
            {
                logger.LogInformation("Running in test environment - skipping strict configuration validation");
                return;
            }

            // Get configuration service
            var configService = scope.ServiceProvider.GetRequiredService<IConfigurationService>();

            // Test Key Vault connectivity if configured
            var keyVaultConfig = app.Configuration.GetSection(KeyVaultConfiguration.SectionName).Get<KeyVaultConfiguration>();
            if (keyVaultConfig != null && !string.IsNullOrEmpty(keyVaultConfig.VaultUrl))
            {
                logger.LogInformation("Testing Key Vault connectivity...");
                try
                {
                    // Try to get a test secret or just verify connectivity
                    await configService.GetSecretAsync("test-connectivity");
                    logger.LogInformation("Key Vault connectivity verified");
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Key Vault connectivity test failed, but continuing startup");
                }
            }

            // Validate configuration sections
            var databaseConfig = app.Configuration.GetSection(DatabaseConfiguration.SectionName).Get<DatabaseConfiguration>();
            if (databaseConfig != null)
            {
                try
                {
                    databaseConfig.Validate();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Database configuration is invalid: {ex.Message}");
                }
            }

            var serviceBusConfig = app.Configuration.GetSection(ServiceBusConfiguration.SectionName).Get<ServiceBusConfiguration>();
            if (serviceBusConfig != null)
            {
                try
                {
                    serviceBusConfig.Validate();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Service Bus configuration is invalid: {ex.Message}");
                }
            }

            var azureAdConfig = app.Configuration.GetSection(AzureAdConfiguration.SectionName).Get<AzureAdConfiguration>();
            if (azureAdConfig != null)
            {
                try
                {
                    azureAdConfig.Validate();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Azure AD configuration is invalid: {ex.Message}");
                }
            }

            var appConfig = app.Configuration.GetSection(ApplicationConfiguration.SectionName).Get<ApplicationConfiguration>();
            if (appConfig != null)
            {
                try
                {
                    appConfig.Validate();
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Application configuration is invalid: {ex.Message}");
                }
            }

            logger.LogInformation("Configuration validation completed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Configuration validation failed");
            throw;
        }
    }

    /// <summary>
    /// Configures JWT authentication with Azure Key Vault support
    /// </summary>
    public static async Task AddJwtAuthenticationAsync(this IServiceCollection services, IServiceProvider serviceProvider)
    {
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        var logger = serviceProvider.GetRequiredService<ILogger<WebApplication>>();

        try
        {
            // Check if running in test environment
            var environment = serviceProvider.GetService<IHostEnvironment>();
            var isTestEnvironment = environment?.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase) == true ||
                                  environment?.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase) == true;

            if (isTestEnvironment)
            {
                logger.LogInformation("Running in test environment - using basic JWT authentication configuration");

                // Simple JWT configuration for testing
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = false,
                        ValidateIssuer = false,
                        ValidateAudience = false,
                        ValidateLifetime = false,
                        RequireExpirationTime = false,
                        RequireSignedTokens = false
                    };
                });

                logger.LogInformation("Test JWT Authentication configured successfully");
                return;
            }

            var configService = serviceProvider.GetRequiredService<IConfigurationService>();

            // Get JWT settings from configuration or Key Vault
            var jwtKey = await configService.GetSecretAsync("JwtSettings--SecretKey");
            if (string.IsNullOrEmpty(jwtKey))
            {
                jwtKey = configuration["JwtSettings:SecretKey"] ?? string.Empty;
            }

            var jwtIssuer = await configService.GetSecretAsync("JwtSettings--Issuer");
            if (string.IsNullOrEmpty(jwtIssuer))
            {
                jwtIssuer = configuration["JwtSettings:Issuer"] ?? "https://localhost:7001";
            }

            var jwtAudience = await configService.GetSecretAsync("JwtSettings--Audience");
            if (string.IsNullOrEmpty(jwtAudience))
            {
                jwtAudience = configuration["JwtSettings:Audience"] ?? "https://localhost:7001";
            }

            if (string.IsNullOrEmpty(jwtKey))
            {
                logger.LogWarning("JWT Secret Key not found in configuration or Key Vault");
                return;
            }

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = jwtAudience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(5)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        logger.LogWarning("JWT Authentication failed: {Error}", context.Exception.Message);
                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        logger.LogWarning("JWT Challenge triggered: {Error}", context.Error);
                        return Task.CompletedTask;
                    }
                };
            });

            logger.LogInformation("JWT Authentication configured successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to configure JWT authentication");
            throw;
        }
    }

    /// <summary>
    /// Adds Swagger configuration with JWT authentication support
    /// </summary>
    public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "Zeus.People API",
                Version = "v1",
                Description = "Academic Management System API with comprehensive configuration management"
            });

            // Add JWT authentication to Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\""
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }

    /// <summary>
    /// Adds a debug endpoint to view configuration in development environment
    /// </summary>
    public static IApplicationBuilder AddConfigurationEndpoint(this WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            return app;
        }

        app.MapGet("/debug/config", (IConfiguration configuration) =>
        {
            var configDict = new Dictionary<string, object?>();

            // Get safe configuration values (exclude secrets)
            var safeKeys = new[]
            {
                "Database:Server",
                "Database:DatabaseName",
                "Database:CommandTimeout",
                "ServiceBus:Namespace",
                "ServiceBus:DefaultMessageTimeToLive",
                "AzureAd:Instance",
                "AzureAd:Domain",
                "Application:Name",
                "Application:Version",
                "KeyVault:VaultUrl"
            };

            foreach (var key in safeKeys)
            {
                configDict[key] = configuration[key];
            }

            return Results.Ok(new
            {
                Environment = app.Environment.EnvironmentName,
                Configuration = configDict,
                Timestamp = DateTime.UtcNow
            });
        })
        .WithName("GetConfiguration")
        .WithTags("Debug")
        .AllowAnonymous();

        return app;
    }
}

/// <summary>
/// Custom Key Vault secret manager for handling secret naming conventions
/// </summary>
public class DefaultKeyVaultSecretManager : KeyVaultSecretManager
{
    /// <summary>
    /// Transforms Key Vault secret names to configuration keys
    /// </summary>
    /// <param name="secret">The Key Vault secret</param>
    /// <returns>The configuration key</returns>
    public override string GetKey(KeyVaultSecret secret)
    {
        // Convert double dashes to configuration section separators
        // Example: "DatabaseSettings--ConnectionString" becomes "DatabaseSettings:ConnectionString"
        return secret.Name.Replace("--", ":");
    }

    /// <summary>
    /// Determines whether a secret should be loaded
    /// </summary>
    /// <param name="secret">The secret properties</param>
    /// <returns>True if the secret should be loaded</returns>
    public override bool Load(SecretProperties secret)
    {
        // Only load enabled secrets
        return secret.Enabled ?? false;
    }
}
