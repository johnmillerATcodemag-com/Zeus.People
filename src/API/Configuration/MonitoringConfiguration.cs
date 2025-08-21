using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using Microsoft.ApplicationInsights;

namespace Zeus.People.API.Configuration;

/// <summary>
/// Configuration model for monitoring settings
/// </summary>
public class MonitoringConfiguration
{
    public const string SectionName = "Monitoring";

    public string ApplicationInsightsConnectionString { get; set; } = string.Empty;
    public string ApplicationInsightsInstrumentationKey { get; set; } = string.Empty;
    public bool EnableApplicationInsights { get; set; } = true;
    public bool EnablePerformanceCounters { get; set; } = true;
    public bool EnableDependencyTracking { get; set; } = true;
    public bool EnableRequestTracking { get; set; } = true;
    public bool EnableExceptionTracking { get; set; } = true;
    public bool EnableEventTracking { get; set; } = true;
    public bool EnableCustomMetrics { get; set; } = true;
    public bool EnableDistributedTracing { get; set; } = true;
    public double SamplingPercentage { get; set; } = 100.0;
    public int MaxTelemetryItemsPerSecond { get; set; } = 200;
    public bool EnableAdaptiveSampling { get; set; } = true;
    public bool EnableHeartbeat { get; set; } = true;
    public int TelemetryFlushIntervalSeconds { get; set; } = 30;
    public bool EnableSqlCommandTextInstrumentation { get; set; } = false;
    public bool EnableDeveloperMode { get; set; } = false;
}

/// <summary>
/// Extensions for configuring comprehensive Application Insights monitoring
/// </summary>
public static class MonitoringExtensions
{
    /// <summary>
    /// Adds comprehensive Application Insights telemetry and monitoring
    /// </summary>
    public static IServiceCollection AddComprehensiveMonitoring(
        this IServiceCollection services, 
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var monitoringConfig = new MonitoringConfiguration();
        configuration.GetSection(MonitoringConfiguration.SectionName).Bind(monitoringConfig);

        // Always add custom metrics and performance monitoring services first
        // This ensures they're available even if Application Insights isn't configured
        services.AddSingleton<ICustomMetricsService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<CustomMetricsService>>();
            var telemetryClient = serviceProvider.GetService<TelemetryClient>(); // May be null
            return new CustomMetricsService(logger, telemetryClient);
        });
        services.AddSingleton<IPerformanceMonitoringService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<PerformanceMonitoringService>>();
            var telemetryClient = serviceProvider.GetService<TelemetryClient>(); // May be null
            return new PerformanceMonitoringService(logger, telemetryClient);
        });

        // Configure Application Insights connection string from multiple sources
        var connectionString = GetApplicationInsightsConnectionString(configuration);
        
        if (string.IsNullOrEmpty(connectionString))
        {
            Console.WriteLine("Warning: Application Insights connection string not found. Telemetry will not be collected.");
            return services;
        }

        // Configure Application Insights with comprehensive settings
        services.AddApplicationInsightsTelemetry(options =>
        {
            options.ConnectionString = connectionString;
            options.EnableAdaptiveSampling = monitoringConfig.EnableAdaptiveSampling;
            options.EnablePerformanceCounterCollectionModule = monitoringConfig.EnablePerformanceCounters;
            options.EnableDependencyTrackingTelemetryModule = monitoringConfig.EnableDependencyTracking;
            options.EnableRequestTrackingTelemetryModule = monitoringConfig.EnableRequestTracking;
            options.EnableEventCounterCollectionModule = true;
            options.DeveloperMode = environment.IsDevelopment() || monitoringConfig.EnableDeveloperMode;
            options.EnableHeartbeat = monitoringConfig.EnableHeartbeat;
        });

        // Configure telemetry processors and initializers
        services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
        services.AddSingleton<ITelemetryProcessor, CustomTelemetryProcessor>();

        // Configure dependency collection
        services.ConfigureTelemetryModule<DependencyTrackingTelemetryModule>((module, o) =>
        {
            module.EnableSqlCommandTextInstrumentation = monitoringConfig.EnableSqlCommandTextInstrumentation;
            module.EnableLegacyCorrelationHeadersInjection = true;
        });

        return services;
    }

    /// <summary>
    /// Gets Application Insights connection string from various configuration sources
    /// </summary>
    private static string GetApplicationInsightsConnectionString(IConfiguration configuration)
    {
        // Priority order: explicit connection string, then Key Vault, then instrumentation key
        return configuration["ApplicationInsights:ConnectionString"] 
            ?? configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
            ?? configuration["ApplicationInsights:InstrumentationKey"]
            ?? configuration["APPINSIGHTS_INSTRUMENTATIONKEY"]
            ?? string.Empty;
    }
}

/// <summary>
/// Custom telemetry initializer to enrich telemetry with additional context
/// </summary>
public class CustomTelemetryInitializer : ITelemetryInitializer
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public CustomTelemetryInitializer(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public void Initialize(ITelemetry telemetry)
    {
        // Add environment information
        telemetry.Context.GlobalProperties["Environment"] = _environment.EnvironmentName;
        telemetry.Context.GlobalProperties["Application"] = "Zeus.People.API";
        telemetry.Context.GlobalProperties["Version"] = _configuration["ApplicationSettings:Version"] ?? "1.0.0";

        // Add deployment information
        telemetry.Context.GlobalProperties["DeploymentId"] = Environment.GetEnvironmentVariable("WEBSITE_DEPLOYMENT_ID") ?? "local";
        telemetry.Context.GlobalProperties["InstanceId"] = Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? Environment.MachineName;

        // Add custom properties
        telemetry.Context.Component.Version = _configuration["ApplicationSettings:Version"] ?? "1.0.0";
        telemetry.Context.Cloud.RoleName = "Zeus.People.API";
        telemetry.Context.Cloud.RoleInstance = Environment.MachineName;
    }
}

/// <summary>
/// Custom telemetry processor for filtering and enriching telemetry
/// </summary>
public class CustomTelemetryProcessor : ITelemetryProcessor
{
    private readonly ITelemetryProcessor _next;
    private readonly ILogger<CustomTelemetryProcessor> _logger;

    public CustomTelemetryProcessor(ITelemetryProcessor next, ILogger<CustomTelemetryProcessor> logger)
    {
        _next = next;
        _logger = logger;
    }

    public void Process(ITelemetry item)
    {
        // Filter out noise from telemetry
        if (ShouldFilter(item))
        {
            return;
        }

        // Enrich telemetry with additional information
        EnrichTelemetry(item);

        _next.Process(item);
    }

    private bool ShouldFilter(ITelemetry item)
    {
        // Filter out health check requests from request telemetry
        if (item is Microsoft.ApplicationInsights.DataContracts.RequestTelemetry request)
        {
            if (request.Url?.AbsolutePath?.EndsWith("/health") == true)
            {
                return true;
            }
        }

        return false;
    }

    private void EnrichTelemetry(ITelemetry item)
    {
        // Add correlation ID if available
        if (item is Microsoft.ApplicationInsights.DataContracts.RequestTelemetry request)
        {
            if (!request.Properties.ContainsKey("CorrelationId"))
            {
                request.Properties["CorrelationId"] = request.Context.Operation.Id ?? Guid.NewGuid().ToString();
            }
        }
    }
}
