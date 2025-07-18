using System.Net;
using System.Text.Json;
using Zeus.People.Domain.Exceptions;

namespace Zeus.People.API.Middleware;

/// <summary>
/// Global exception handling middleware
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var result = exception switch
        {
            EntityNotFoundException notFoundEx => new
            {
                success = false,
                error = notFoundEx.Message
            },
            DomainException domainEx => new
            {
                success = false,
                error = domainEx.Message
            },
            UnauthorizedAccessException _ => new
            {
                success = false,
                error = "Unauthorized access"
            },
            _ => new
            {
                success = false,
                error = "An internal server error occurred"
            }
        };

        context.Response.StatusCode = exception switch
        {
            EntityNotFoundException => (int)HttpStatusCode.NotFound,
            DomainException => (int)HttpStatusCode.BadRequest,
            UnauthorizedAccessException => (int)HttpStatusCode.Unauthorized,
            _ => (int)HttpStatusCode.InternalServerError
        };

        var jsonResponse = JsonSerializer.Serialize(result, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Extension methods for exception handling middleware
/// </summary>
public static class ExceptionHandlingMiddlewareExtensions
{
    /// <summary>
    /// Adds global exception handling middleware
    /// </summary>
    public static IApplicationBuilder UseExceptionHandling(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ExceptionHandlingMiddleware>();
    }
}
