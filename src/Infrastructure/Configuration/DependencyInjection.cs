using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Zeus.People.Application.Interfaces;
using Zeus.People.Infrastructure.Configuration;
using Zeus.People.Infrastructure.EventStore;
using Zeus.People.Infrastructure.HealthChecks;
using Zeus.People.Infrastructure.Messaging;
using Zeus.People.Infrastructure.Persistence;
using Zeus.People.Infrastructure.Persistence.Repositories;

namespace Zeus.People.Infrastructure.Configuration;

/// <summary>
/// Dependency injection configuration for the Infrastructure layer
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Adds infrastructure services to the service collection
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Check if SQL Database features are enabled (explicit string checking for debugging)
        var sqlDatabaseConfigValue = configuration["Features:EnableSqlDatabase"] ?? configuration["Features__EnableSqlDatabase"] ?? "true";
        var eventStoreConfigValue = configuration["Features:EnableEventStore"] ?? configuration["Features__EnableEventStore"] ?? "true";

        var enableSqlDatabase = !string.Equals(sqlDatabaseConfigValue, "false", StringComparison.OrdinalIgnoreCase);
        var enableEventStore = !string.Equals(eventStoreConfigValue, "false", StringComparison.OrdinalIgnoreCase);

        // Debug logging for feature flag evaluation (will appear in app logs)
        Console.WriteLine($"DEBUG: Features:EnableSqlDatabase config value: '{sqlDatabaseConfigValue}' -> parsed as: {enableSqlDatabase}");
        Console.WriteLine($"DEBUG: Features:EnableEventStore config value: '{eventStoreConfigValue}' -> parsed as: {enableEventStore}");

        // Only add Entity Framework if SQL Database is enabled (feature flag takes priority)
        if (enableSqlDatabase)
        {
            services.AddDbContext<AcademicContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("AcademicDatabase");
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });

                // Enable sensitive data logging in development
                if (bool.TryParse(configuration["Logging:EnableSensitiveDataLogging"], out var enableSensitiveDataLogging) && enableSensitiveDataLogging)
                {
                    options.EnableSensitiveDataLogging();
                }
            });
        }

        // Only add Event Store if enabled (feature flag takes priority)
        if (enableEventStore)
        {
            services.AddDbContext<EventStoreContext>(options =>
            {
                var connectionString = configuration.GetConnectionString("EventStoreDatabase");
                options.UseSqlServer(connectionString, sqlOptions =>
                {
                    sqlOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorNumbersToAdd: null);
                });
            });
        }

        // Add Azure Service Bus (only if connection string is available)
        var serviceBusConnectionString = configuration.GetConnectionString("ServiceBus");
        if (!string.IsNullOrEmpty(serviceBusConnectionString))
        {
            var serviceBusSection = configuration.GetSection(ServiceBusConfiguration.SectionName);
            services.Configure<ServiceBusConfiguration>(options =>
            {
                options.Namespace = serviceBusSection[nameof(ServiceBusConfiguration.Namespace)] ?? string.Empty;
                options.TopicName = serviceBusSection[nameof(ServiceBusConfiguration.TopicName)] ?? "domain-events";
                options.SubscriptionName = serviceBusSection[nameof(ServiceBusConfiguration.SubscriptionName)] ?? "zeus-people-subscription";
                if (bool.TryParse(serviceBusSection[nameof(ServiceBusConfiguration.UseManagedIdentity)], out var useManagedIdentity))
                    options.UseManagedIdentity = useManagedIdentity;
                if (int.TryParse(serviceBusSection[nameof(ServiceBusConfiguration.MaxRetryAttempts)], out var maxRetryAttempts))
                    options.MaxRetryAttempts = maxRetryAttempts;
                if (int.TryParse(serviceBusSection[nameof(ServiceBusConfiguration.DelayBetweenRetriesInSeconds)], out var delayBetweenRetries))
                    options.DelayBetweenRetriesInSeconds = delayBetweenRetries;
                if (int.TryParse(serviceBusSection[nameof(ServiceBusConfiguration.MaxDelayInSeconds)], out var maxDelay))
                    options.MaxDelayInSeconds = maxDelay;
            });

            services.AddSingleton(serviceProvider =>
            {
                var connectionString = configuration.GetConnectionString("ServiceBus");
                var clientOptions = new ServiceBusClientOptions
                {
                    RetryOptions = new ServiceBusRetryOptions
                    {
                        Mode = ServiceBusRetryMode.Exponential,
                        MaxRetries = 3,
                        Delay = TimeSpan.FromSeconds(2),
                        MaxDelay = TimeSpan.FromSeconds(30)
                    }
                };

                // Use Managed Identity in production, connection string in development
                if (string.IsNullOrEmpty(connectionString))
                {
                    var serviceBusNamespace = configuration["ServiceBus:Namespace"];
                    return new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential(), clientOptions);
                }
                else
                {
                    return new ServiceBusClient(connectionString, clientOptions);
                }
            });
        }

        // Add Cosmos DB for read operations
        var cosmosDbSection = configuration.GetSection(CosmosDbConfiguration.SectionName);
        services.Configure<CosmosDbConfiguration>(options =>
        {
            options.DatabaseName = cosmosDbSection[nameof(CosmosDbConfiguration.DatabaseName)] ?? "Zeus.People";
            options.Endpoint = cosmosDbSection[nameof(CosmosDbConfiguration.Endpoint)] ?? string.Empty;
            options.AuthKey = cosmosDbSection[nameof(CosmosDbConfiguration.AuthKey)] ?? string.Empty;
            if (bool.TryParse(cosmosDbSection[nameof(CosmosDbConfiguration.UseManagedIdentity)], out var useManagedIdentity))
                options.UseManagedIdentity = useManagedIdentity;
            if (int.TryParse(cosmosDbSection[nameof(CosmosDbConfiguration.RequestTimeoutInSeconds)], out var requestTimeout))
                options.RequestTimeoutInSeconds = requestTimeout;
            if (int.TryParse(cosmosDbSection[nameof(CosmosDbConfiguration.MaxRetryAttemptsOnRateLimitedRequests)], out var maxRetryAttempts))
                options.MaxRetryAttemptsOnRateLimitedRequests = maxRetryAttempts;
            if (int.TryParse(cosmosDbSection[nameof(CosmosDbConfiguration.MaxRetryWaitTimeInSeconds)], out var maxRetryWait))
                options.MaxRetryWaitTimeInSeconds = maxRetryWait;
        });

        services.AddSingleton<CosmosClient>(serviceProvider =>
        {
            var cosmosDbConfig = serviceProvider.GetRequiredService<IOptions<CosmosDbConfiguration>>().Value;
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var environment = serviceProvider.GetService<IHostEnvironment>();

            // Check if we're in a test environment
            var isTestEnvironment = environment?.EnvironmentName.Equals("Testing", StringComparison.OrdinalIgnoreCase) == true ||
                                  environment?.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase) == true;

            var cosmosClientOptions = new CosmosClientOptions
            {
                MaxRetryAttemptsOnRateLimitedRequests = cosmosDbConfig.MaxRetryAttemptsOnRateLimitedRequests,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(cosmosDbConfig.MaxRetryWaitTimeInSeconds),
                ConnectionMode = ConnectionMode.Direct,
                RequestTimeout = TimeSpan.FromSeconds(cosmosDbConfig.RequestTimeoutInSeconds)
            };

            // In test environments, use a mock/fake connection or emulator
            if (isTestEnvironment)
            {
                // Use Cosmos DB Emulator connection or return a test client
                var testConnectionString = "AccountEndpoint=https://localhost:8081/;AccountKey=C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==";
                try
                {
                    return new CosmosClient(testConnectionString, cosmosClientOptions);
                }
                catch
                {
                    // If emulator is not available, create a minimal client for testing
                    return new CosmosClient("https://test.documents.azure.com:443/", "test-key", cosmosClientOptions);
                }
            }

            // For non-test environments, use the original logic
            // Prefer managed identity when configured, fall back to connection string
            if (cosmosDbConfig.UseManagedIdentity && !string.IsNullOrEmpty(cosmosDbConfig.Endpoint))
            {
                return new CosmosClient(cosmosDbConfig.Endpoint, new DefaultAzureCredential(), cosmosClientOptions);
            }
            else if (!string.IsNullOrEmpty(cosmosDbConfig.AuthKey) && !string.IsNullOrEmpty(cosmosDbConfig.Endpoint))
            {
                return new CosmosClient(cosmosDbConfig.Endpoint, cosmosDbConfig.AuthKey, cosmosClientOptions);
            }
            else
            {
                // Fall back to connection string if available
                var connectionString = configuration.GetConnectionString("CosmosDbConnection");
                if (!string.IsNullOrEmpty(connectionString))
                {
                    return new CosmosClient(connectionString, cosmosClientOptions);
                }

                throw new InvalidOperationException("Cosmos DB configuration is invalid. Either provide endpoint with managed identity/auth key, or a valid connection string.");
            }
        });

        // Register repositories (conditionally based on SQL availability)
        if (enableSqlDatabase && !string.IsNullOrEmpty(configuration.GetConnectionString("AcademicDatabase")))
        {
            services.AddScoped<IAcademicRepository, AcademicRepository>();
            services.AddScoped<IDepartmentRepository, DepartmentRepository>();
            services.AddScoped<IRoomRepository, RoomRepository>();
            services.AddScoped<IExtensionRepository, ExtensionRepository>();
            services.AddScoped<IUnitOfWork, UnitOfWork>();
        }
        // Note: In Cosmos-only mode, write operations are handled via read repositories and direct Cosmos operations

        // Register read repositories (always available via Cosmos DB)
        services.AddScoped<IAcademicReadRepository, CosmosDbReadModelRepository>();
        services.AddScoped<IDepartmentReadRepository, CosmosDbReadModelRepository>();
        services.AddScoped<IRoomReadRepository, CosmosDbReadModelRepository>();
        services.AddScoped<IExtensionReadRepository, CosmosDbReadModelRepository>();

        // Register event store and publisher (conditionally)
        if (enableEventStore && !string.IsNullOrEmpty(configuration.GetConnectionString("EventStoreDatabase")))
        {
            services.AddScoped<IEventStore, SqlEventStore>();
        }
        // Note: In Cosmos-only mode, event sourcing is disabled

        if (!string.IsNullOrEmpty(serviceBusConnectionString))
        {
            services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();
        }
        // Note: In Service Bus unavailable mode, events are not published

        // Add health checks (conditionally based on available services)
        var healthChecksBuilder = services.AddHealthChecks();

        // Only add SQL-based health checks if SQL services are enabled (feature flag takes priority)
        if (enableSqlDatabase)
        {
            Console.WriteLine("DEBUG: Adding DatabaseHealthCheck because enableSqlDatabase = true");
            healthChecksBuilder.AddCheck<DatabaseHealthCheck>("database", tags: new[] { "database", "sql" });
        }
        else
        {
            Console.WriteLine("DEBUG: Skipping DatabaseHealthCheck because enableSqlDatabase = false");
        }

        if (enableEventStore)
        {
            Console.WriteLine("DEBUG: Adding EventStoreHealthCheck because enableEventStore = true");
            healthChecksBuilder.AddCheck<EventStoreHealthCheck>("eventstore", tags: new[] { "eventstore", "sql" });
        }
        else
        {
            Console.WriteLine("DEBUG: Skipping EventStoreHealthCheck because enableEventStore = false");
        }

        // Only add Service Bus health check if Service Bus is available
        if (!string.IsNullOrEmpty(serviceBusConnectionString))
        {
            healthChecksBuilder.AddCheck<ServiceBusHealthCheck>("servicebus", tags: new[] { "servicebus", "messaging" });
        }

        // Always add Cosmos DB health check (primary database)
        healthChecksBuilder.AddCheck<CosmosDbHealthCheck>("cosmosdb", tags: new[] { "cosmosdb", "nosql" });

        return services;
    }

    /// <summary>
    /// Ensures databases are created and migrated (optional - call manually)
    /// </summary>
    public static async Task<IServiceProvider> EnsureDatabasesCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

        try
        {
            // Only migrate Academic database if it's configured
            var enableSqlDatabase = bool.TryParse(configuration["Features:EnableSqlDatabase"], out var sqlEnabled) ? sqlEnabled : true;
            if (enableSqlDatabase && !string.IsNullOrEmpty(configuration.GetConnectionString("AcademicDatabase")))
            {
                var academicContext = scope.ServiceProvider.GetService<AcademicContext>();
                if (academicContext != null && await academicContext.Database.CanConnectAsync())
                {
                    await academicContext.Database.MigrateAsync();
                }
            }

            // Only migrate Event Store database if it's configured
            var enableEventStore = bool.TryParse(configuration["Features:EnableEventStore"], out var eventStoreEnabled) ? eventStoreEnabled : true;
            if (enableEventStore && !string.IsNullOrEmpty(configuration.GetConnectionString("EventStoreDatabase")))
            {
                var eventStoreContext = scope.ServiceProvider.GetService<EventStoreContext>();
                if (eventStoreContext != null && await eventStoreContext.Database.CanConnectAsync())
                {
                    await eventStoreContext.Database.MigrateAsync();
                }
            }

            // Always ensure Cosmos DB containers are created (primary database)
            await EnsureCosmosDbContainersAsync(scope.ServiceProvider);
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail startup
            var loggerFactory = scope.ServiceProvider.GetService<Microsoft.Extensions.Logging.ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("DatabaseMigration");
            logger?.LogWarning(ex, "Failed to ensure databases are created during startup");
        }

        return serviceProvider;
    }

    private static async Task EnsureCosmosDbContainersAsync(IServiceProvider serviceProvider)
    {
        try
        {
            var cosmosClient = serviceProvider.GetRequiredService<CosmosClient>();
            var configuration = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<CosmosDbConfiguration>>();

            var databaseName = configuration.Value.DatabaseName;

            // Create database if it doesn't exist
            var databaseResponse = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseName);
            var database = databaseResponse.Database;

            // Create containers for read models
            var containers = new[]
            {
                new { Name = "academics", PartitionKey = "/id" },
                new { Name = "departments", PartitionKey = "/id" },
                new { Name = "rooms", PartitionKey = "/id" },
                new { Name = "extensions", PartitionKey = "/id" }
            };

            foreach (var container in containers)
            {
                await database.CreateContainerIfNotExistsAsync(
                    container.Name,
                    container.PartitionKey,
                    400); // Request Units per second
            }
        }
        catch (Exception ex)
        {
            // Log the exception but don't fail startup
            var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
            var logger = loggerFactory?.CreateLogger("CosmosDbSetup");
            logger?.LogWarning(ex, "Failed to ensure Cosmos DB containers are created during startup");
        }
    }
}
