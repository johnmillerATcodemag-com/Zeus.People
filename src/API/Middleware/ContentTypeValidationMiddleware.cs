using System.Net;
using System.Text.Json;

namespace Zeus.People.API.Middleware;

/// <summary>
/// Middleware to validate content-type for API requests
/// </summary>
public class ContentTypeValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ContentTypeValidationMiddleware> _logger;
    private readonly HashSet<string> _supportedContentTypes;

    public ContentTypeValidationMiddleware(RequestDelegate next, ILogger<ContentTypeValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
        _supportedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "application/json",
            "application/json; charset=utf-8",
            "text/json"
        };
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Only validate content-type for requests with body content (POST, PUT, PATCH)
        if (ShouldValidateContentType(context))
        {
            var contentType = context.Request.ContentType;

            if (!string.IsNullOrEmpty(contentType))
            {
                // Extract the media type (remove charset and other parameters)
                var mediaType = contentType.Split(';')[0].Trim();

                if (!_supportedContentTypes.Contains(mediaType))
                {
                    _logger.LogWarning("Unsupported media type: {ContentType}", contentType);
                    await WriteUnsupportedMediaTypeResponse(context, contentType);
                    return;
                }
            }
        }

        await _next(context);
    }

    private static bool ShouldValidateContentType(HttpContext context)
    {
        var method = context.Request.Method;
        var hasContentLength = context.Request.ContentLength > 0;
        var hasTransferEncoding = !string.IsNullOrEmpty(context.Request.Headers["Transfer-Encoding"]);

        return (method == "POST" || method == "PUT" || method == "PATCH") &&
               (hasContentLength || hasTransferEncoding);
    }

    private static async Task WriteUnsupportedMediaTypeResponse(HttpContext context, string contentType)
    {
        context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
        context.Response.ContentType = "application/json";

        var response = new
        {
            success = false,
            error = $"Unsupported media type: {contentType}. Supported types: application/json",
            statusCode = 415
        };

        var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await context.Response.WriteAsync(jsonResponse);
    }
}

/// <summary>
/// Extension methods for content-type validation middleware
/// </summary>
public static class ContentTypeValidationMiddlewareExtensions
{
    /// <summary>
    /// Adds content-type validation middleware
    /// </summary>
    public static IApplicationBuilder UseContentTypeValidation(this IApplicationBuilder app)
    {
        return app.UseMiddleware<ContentTypeValidationMiddleware>();
    }
}
