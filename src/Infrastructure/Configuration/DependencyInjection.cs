using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
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
        // Add Entity Framework for write operations
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

        // Add Event Store
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

        // Add Azure Service Bus
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

        services.AddSingleton(serviceProvider =>
        {
            var connectionString = configuration.GetConnectionString("CosmosDb");
            var cosmosClientOptions = new CosmosClientOptions
            {
                MaxRetryAttemptsOnRateLimitedRequests = 3,
                MaxRetryWaitTimeOnRateLimitedRequests = TimeSpan.FromSeconds(30),
                ConnectionMode = ConnectionMode.Direct
            };

            // Use Managed Identity in production, connection string in development
            if (string.IsNullOrEmpty(connectionString))
            {
                var cosmosDbEndpoint = configuration["CosmosDb:Endpoint"];
                return new CosmosClient(cosmosDbEndpoint, new DefaultAzureCredential(), cosmosClientOptions);
            }
            else
            {
                return new CosmosClient(connectionString, cosmosClientOptions);
            }
        });

        // Register repositories
        services.AddScoped<IAcademicRepository, AcademicRepository>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<IExtensionRepository, ExtensionRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register read repositories
        services.AddScoped<IAcademicReadRepository, CosmosDbReadModelRepository>();
        services.AddScoped<IDepartmentReadRepository, CosmosDbReadModelRepository>();
        services.AddScoped<IRoomReadRepository, CosmosDbReadModelRepository>();
        services.AddScoped<IExtensionReadRepository, CosmosDbReadModelRepository>();

        // Register event store and publisher
        services.AddScoped<IEventStore, SqlEventStore>();
        services.AddScoped<IEventPublisher, ServiceBusEventPublisher>();

        // Add health checks
        services.AddHealthChecks()
            .AddCheck<DatabaseHealthCheck>("database", tags: new[] { "database", "sql" })
            .AddCheck<EventStoreHealthCheck>("eventstore", tags: new[] { "eventstore", "sql" })
            .AddCheck<ServiceBusHealthCheck>("servicebus", tags: new[] { "servicebus", "messaging" })
            .AddCheck<CosmosDbHealthCheck>("cosmosdb", tags: new[] { "cosmosdb", "nosql" });

        return services;
    }

    /// <summary>
    /// Ensures databases are created and migrated
    /// </summary>
    public static async Task<IServiceProvider> EnsureDatabasesCreatedAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();

        // Ensure Academic database is created and migrated
        var academicContext = scope.ServiceProvider.GetRequiredService<AcademicContext>();
        await academicContext.Database.MigrateAsync();

        // Ensure Event Store database is created and migrated
        var eventStoreContext = scope.ServiceProvider.GetRequiredService<EventStoreContext>();
        await eventStoreContext.Database.MigrateAsync();

        // Ensure Cosmos DB containers are created
        await EnsureCosmosDbContainersAsync(scope.ServiceProvider);

        return serviceProvider;
    }

    private static async Task EnsureCosmosDbContainersAsync(IServiceProvider serviceProvider)
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
}
