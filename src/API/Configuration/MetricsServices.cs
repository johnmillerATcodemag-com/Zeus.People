using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace Zeus.People.API.Configuration;

/// <summary>
/// Service for tracking custom business metrics
/// </summary>
public interface ICustomMetricsService
{
    void TrackBusinessEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null);
    void TrackBusinessRule(string ruleName, bool passed, string details = "");
    void TrackPerformance(string operationName, TimeSpan duration, bool success = true, string? details = null);
    void TrackDependencyCall(string dependencyName, string commandName, DateTimeOffset startTime, TimeSpan duration, bool success);
    void IncrementCounter(string counterName, IDictionary<string, string>? properties = null);
    void SetGauge(string gaugeName, double value, IDictionary<string, string>? properties = null);
    IDisposable StartTimer(string operationName);
}

/// <summary>
/// Implementation of custom metrics service using Application Insights
/// </summary>
public class CustomMetricsService : ICustomMetricsService
{
    private readonly TelemetryClient? _telemetryClient;
    private readonly ILogger<CustomMetricsService> _logger;
    private readonly ConcurrentDictionary<string, double> _counters = new();

    public CustomMetricsService(ILogger<CustomMetricsService> logger, TelemetryClient? telemetryClient = null)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public void TrackBusinessEvent(string eventName, IDictionary<string, string>? properties = null, IDictionary<string, double>? metrics = null)
    {
        try
        {
            _telemetryClient?.TrackEvent(eventName, properties, metrics);
            _logger.LogDebug("Tracked business event: {EventName}", eventName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to track business event: {EventName}", eventName);
        }
    }

    public void TrackBusinessRule(string ruleName, bool passed, string details = "")
    {
        var properties = new Dictionary<string, string>
        {
            ["RuleName"] = ruleName,
            ["Passed"] = passed.ToString(),
            ["Details"] = details
        };

        var metrics = new Dictionary<string, double>
        {
            ["BusinessRuleViolation"] = passed ? 0 : 1
        };

        TrackBusinessEvent("BusinessRuleEvaluation", properties, metrics);
    }

    public void TrackPerformance(string operationName, TimeSpan duration, bool success = true, string? details = null)
    {
        var properties = new Dictionary<string, string>
        {
            ["OperationName"] = operationName,
            ["Success"] = success.ToString()
        };

        if (!string.IsNullOrEmpty(details))
        {
            properties["Details"] = details;
        }

        var metrics = new Dictionary<string, double>
        {
            ["Duration"] = duration.TotalMilliseconds,
            ["Success"] = success ? 1 : 0
        };

        TrackBusinessEvent("PerformanceMetric", properties, metrics);

        // Also track as custom metric
        _telemetryClient?.TrackMetric($"Performance.{operationName}.Duration", duration.TotalMilliseconds, properties);
    }

    public void TrackDependencyCall(string dependencyName, string commandName, DateTimeOffset startTime, TimeSpan duration, bool success)
    {
        _telemetryClient?.TrackDependency(dependencyName, commandName, null, startTime, duration, success);
        _logger.LogDebug("Tracked dependency call: {DependencyName}.{CommandName}, Duration: {Duration}ms, Success: {Success}",
            dependencyName, commandName, duration.TotalMilliseconds, success);
    }

    public void IncrementCounter(string counterName, IDictionary<string, string>? properties = null)
    {
        _counters.AddOrUpdate(counterName, 1, (key, value) => value + 1);
        _telemetryClient?.TrackMetric(counterName, _counters[counterName], properties);
    }

    public void SetGauge(string gaugeName, double value, IDictionary<string, string>? properties = null)
    {
        _telemetryClient?.TrackMetric(gaugeName, value, properties);
        _logger.LogDebug("Set gauge {GaugeName} to {Value}", gaugeName, value);
    }

    public IDisposable StartTimer(string operationName)
    {
        return new PerformanceTimer(operationName, this);
    }

    private class PerformanceTimer : IDisposable
    {
        private readonly string _operationName;
        private readonly CustomMetricsService _metricsService;
        private readonly Stopwatch _stopwatch;
        private bool _disposed = false;

        public PerformanceTimer(string operationName, CustomMetricsService metricsService)
        {
            _operationName = operationName;
            _metricsService = metricsService;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _stopwatch.Stop();
                _metricsService.TrackPerformance(_operationName, _stopwatch.Elapsed);
                _disposed = true;
            }
        }
    }
}

/// <summary>
/// Service for monitoring application performance and resource utilization
/// </summary>
public interface IPerformanceMonitoringService
{
    void TrackMemoryUsage();
    void TrackThreadPoolMetrics();
    void TrackGCMetrics();
    void TrackHttpRequestMetrics(HttpContext context, TimeSpan duration, int statusCode);
    void StartPerformanceCounterCollection();
}

/// <summary>
/// Implementation of performance monitoring service
/// </summary>
public class PerformanceMonitoringService : IPerformanceMonitoringService
{
    private readonly TelemetryClient? _telemetryClient;
    private readonly ILogger<PerformanceMonitoringService> _logger;
    private readonly Timer _performanceTimer;

    public PerformanceMonitoringService(ILogger<PerformanceMonitoringService> logger, TelemetryClient? telemetryClient = null)
    {
        _telemetryClient = telemetryClient;
        _logger = logger;

        // Start periodic performance collection
        _performanceTimer = new Timer(CollectPerformanceMetrics, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
    }

    public void TrackMemoryUsage()
    {
        var process = Process.GetCurrentProcess();
        var workingSet = process.WorkingSet64;
        var privateMemory = process.PrivateMemorySize64;
        var gcTotalMemory = GC.GetTotalMemory(false);

        _telemetryClient?.TrackMetric("Performance.Memory.WorkingSet", workingSet);
        _telemetryClient?.TrackMetric("Performance.Memory.PrivateBytes", privateMemory);
        _telemetryClient?.TrackMetric("Performance.Memory.GCTotalMemory", gcTotalMemory);

        _logger.LogDebug("Memory metrics - Working Set: {WorkingSet} bytes, Private: {PrivateMemory} bytes, GC: {GCMemory} bytes",
            workingSet, privateMemory, gcTotalMemory);
    }

    public void TrackThreadPoolMetrics()
    {
        ThreadPool.GetAvailableThreads(out int availableWorkerThreads, out int availableCompletionPortThreads);
        ThreadPool.GetMaxThreads(out int maxWorkerThreads, out int maxCompletionPortThreads);
        
        var usedWorkerThreads = maxWorkerThreads - availableWorkerThreads;
        var usedCompletionPortThreads = maxCompletionPortThreads - availableCompletionPortThreads;

        _telemetryClient?.TrackMetric("Performance.ThreadPool.WorkerThreads.Available", availableWorkerThreads);
        _telemetryClient?.TrackMetric("Performance.ThreadPool.WorkerThreads.Used", usedWorkerThreads);
        _telemetryClient?.TrackMetric("Performance.ThreadPool.CompletionPortThreads.Available", availableCompletionPortThreads);
        _telemetryClient?.TrackMetric("Performance.ThreadPool.CompletionPortThreads.Used", usedCompletionPortThreads);
    }

    public void TrackGCMetrics()
    {
        var gen0Collections = GC.CollectionCount(0);
        var gen1Collections = GC.CollectionCount(1);
        var gen2Collections = GC.CollectionCount(2);

        _telemetryClient?.TrackMetric("Performance.GC.Gen0Collections", gen0Collections);
        _telemetryClient?.TrackMetric("Performance.GC.Gen1Collections", gen1Collections);
        _telemetryClient?.TrackMetric("Performance.GC.Gen2Collections", gen2Collections);
    }

    public void TrackHttpRequestMetrics(HttpContext context, TimeSpan duration, int statusCode)
    {
        var properties = new Dictionary<string, string>
        {
            ["Method"] = context.Request.Method,
            ["Path"] = context.Request.Path,
            ["StatusCode"] = statusCode.ToString(),
            ["ContentType"] = context.Response.ContentType ?? "unknown"
        };

        var metrics = new Dictionary<string, double>
        {
            ["Duration"] = duration.TotalMilliseconds,
            ["ResponseSize"] = context.Response.ContentLength ?? 0
        };

        _telemetryClient?.TrackEvent("HttpRequest", properties, metrics);
    }

    public void StartPerformanceCounterCollection()
    {
        _logger.LogInformation("Performance counter collection started");
    }

    private void CollectPerformanceMetrics(object? state)
    {
        try
        {
            TrackMemoryUsage();
            TrackThreadPoolMetrics();
            TrackGCMetrics();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to collect performance metrics");
        }
    }
}
