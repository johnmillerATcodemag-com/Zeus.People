using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Zeus.People.API.Configuration;

namespace Zeus.People.API.Middleware;

/// <summary>
/// Middleware for tracking application performance metrics and distributed tracing
/// </summary>
public class PerformanceMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICustomMetricsService _metricsService;
    private readonly IPerformanceMonitoringService _performanceService;
    private readonly TelemetryClient? _telemetryClient;
    private readonly ILogger<PerformanceMonitoringMiddleware> _logger;

    public PerformanceMonitoringMiddleware(
        RequestDelegate next,
        ICustomMetricsService metricsService,
        IPerformanceMonitoringService performanceService,
        ILogger<PerformanceMonitoringMiddleware> logger,
        TelemetryClient? telemetryClient = null)
    {
        _next = next;
        _metricsService = metricsService;
        _performanceService = performanceService;
        _telemetryClient = telemetryClient;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var startTime = DateTimeOffset.UtcNow;
        var requestTelemetry = _telemetryClient != null ? new Microsoft.ApplicationInsights.DataContracts.RequestTelemetry
        {
            Name = $"{context.Request.Method} {context.Request.Path}",
            Timestamp = startTime,
            Url = new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}")
        } : null;

        // Set correlation context
        context.Items["RequestStartTime"] = startTime;
        context.Items["RequestStopwatch"] = stopwatch;

        // Add correlation ID to response headers
        var correlationId = context.TraceIdentifier;
        context.Response.Headers.Append("X-Correlation-ID", correlationId);

        Exception? exception = null;

        try
        {
            // Track request start
            _metricsService.IncrementCounter("HttpRequests.Started", new Dictionary<string, string>
            {
                ["Method"] = context.Request.Method,
                ["Path"] = context.Request.Path.ToString(),
                ["UserAgent"] = context.Request.Headers.UserAgent.ToString()
            });

            await _next(context);

            // Track successful completion
            if (requestTelemetry != null)
            {
                requestTelemetry.Success = true;
                requestTelemetry.ResponseCode = context.Response.StatusCode.ToString();
            }
        }
        catch (Exception ex)
        {
            exception = ex;
            if (requestTelemetry != null)
            {
                requestTelemetry.Success = false;
                requestTelemetry.ResponseCode = "500";
            }
            
            // Track exception metrics
            _metricsService.TrackBusinessEvent("HttpRequest.Exception", new Dictionary<string, string>
            {
                ["Method"] = context.Request.Method,
                ["Path"] = context.Request.Path.ToString(),
                ["ExceptionType"] = ex.GetType().Name,
                ["ExceptionMessage"] = ex.Message
            });

            throw;
        }
        finally
        {
            stopwatch.Stop();
            if (requestTelemetry != null)
            {
                requestTelemetry.Duration = stopwatch.Elapsed;
            }

            // Track performance metrics
            _performanceService.TrackHttpRequestMetrics(context, stopwatch.Elapsed, context.Response.StatusCode);
            
            // Track response time metrics
            var responseTimeCategory = GetResponseTimeCategory(stopwatch.Elapsed);
            _metricsService.IncrementCounter($"HttpRequests.ResponseTime.{responseTimeCategory}");

            // Track status code metrics
            var statusCodeCategory = GetStatusCodeCategory(context.Response.StatusCode);
            _metricsService.IncrementCounter($"HttpRequests.StatusCode.{statusCodeCategory}");

            // Track detailed performance metrics
            _metricsService.TrackPerformance("HttpRequest", stopwatch.Elapsed, exception == null, 
                $"{context.Request.Method} {context.Request.Path}");

            // Send request telemetry
            _telemetryClient?.TrackRequest(requestTelemetry);

            // Log performance information
            _logger.LogInformation(
                "HTTP {Method} {Path} responded {StatusCode} in {Duration}ms - Correlation ID: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.Response.StatusCode,
                stopwatch.ElapsedMilliseconds,
                correlationId);
        }
    }

    private string GetResponseTimeCategory(TimeSpan duration)
    {
        return duration.TotalMilliseconds switch
        {
            < 100 => "Fast",
            < 500 => "Normal",
            < 1000 => "Slow",
            < 2000 => "VerySlow",
            _ => "Critical"
        };
    }

    private string GetStatusCodeCategory(int statusCode)
    {
        return statusCode switch
        {
            >= 200 and < 300 => "Success",
            >= 300 and < 400 => "Redirect",
            >= 400 and < 500 => "ClientError",
            >= 500 => "ServerError",
            _ => "Unknown"
        };
    }
}

/// <summary>
/// Middleware for tracking business metrics and domain events
/// </summary>
public class BusinessMetricsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ICustomMetricsService _metricsService;
    private readonly ILogger<BusinessMetricsMiddleware> _logger;

    public BusinessMetricsMiddleware(
        RequestDelegate next,
        ICustomMetricsService metricsService,
        ILogger<BusinessMetricsMiddleware> logger)
    {
        _next = next;
        _metricsService = metricsService;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Track business-specific metrics based on endpoints
        TrackBusinessMetrics(context);

        await _next(context);

        // Track post-request business metrics
        TrackPostRequestBusinessMetrics(context);
    }

    private void TrackBusinessMetrics(HttpContext context)
    {
        var path = context.Request.Path.ToString().ToLowerInvariant();
        var method = context.Request.Method.ToUpperInvariant();

        // Track business entity access patterns
        if (path.Contains("/academics"))
        {
            _metricsService.IncrementCounter("Business.Academic.Access", new Dictionary<string, string>
            {
                ["Method"] = method,
                ["Endpoint"] = path
            });
        }
        else if (path.Contains("/departments"))
        {
            _metricsService.IncrementCounter("Business.Department.Access", new Dictionary<string, string>
            {
                ["Method"] = method,
                ["Endpoint"] = path
            });
        }
        else if (path.Contains("/rooms"))
        {
            _metricsService.IncrementCounter("Business.Room.Access", new Dictionary<string, string>
            {
                ["Method"] = method,
                ["Endpoint"] = path
            });
        }

        // Track authentication patterns
        if (context.Request.Headers.ContainsKey("Authorization"))
        {
            _metricsService.IncrementCounter("Business.AuthenticatedRequest");
        }
        else
        {
            _metricsService.IncrementCounter("Business.AnonymousRequest");
        }
    }

    private void TrackPostRequestBusinessMetrics(HttpContext context)
    {
        var path = context.Request.Path.ToString().ToLowerInvariant();
        var method = context.Request.Method.ToUpperInvariant();
        var statusCode = context.Response.StatusCode;

        // Track business operation success/failure rates
        if (method == "POST" || method == "PUT" || method == "DELETE")
        {
            var operationType = path switch
            {
                var p when p.Contains("/academics") => "Academic",
                var p when p.Contains("/departments") => "Department", 
                var p when p.Contains("/rooms") => "Room",
                _ => "Unknown"
            };

            var success = statusCode >= 200 && statusCode < 300;
            _metricsService.TrackBusinessEvent($"Business.{operationType}.Operation", 
                new Dictionary<string, string>
                {
                    ["Method"] = method,
                    ["Success"] = success.ToString(),
                    ["StatusCode"] = statusCode.ToString()
                },
                new Dictionary<string, double>
                {
                    ["Success"] = success ? 1 : 0
                });
        }
    }
}
