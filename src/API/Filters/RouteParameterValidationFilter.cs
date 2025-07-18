using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;

namespace Zeus.People.API.Filters;

/// <summary>
/// Action filter to validate route parameters
/// </summary>
public class RouteParameterValidationFilter : IActionFilter
{
    private readonly ILogger<RouteParameterValidationFilter> _logger;

    public RouteParameterValidationFilter(ILogger<RouteParameterValidationFilter> logger)
    {
        _logger = logger;
    }

    public void OnActionExecuting(ActionExecutingContext context)
    {
        // Check for GUID parameters that failed to bind
        foreach (var parameter in context.ActionDescriptor.Parameters)
        {
            var parameterName = parameter.Name;
            var parameterType = parameter.ParameterType;

            // Check if it's a GUID parameter
            if (parameterType == typeof(Guid) || parameterType == typeof(Guid?))
            {
                // Get the raw route value
                if (context.RouteData.Values.TryGetValue(parameterName, out var rawValue))
                {
                    var stringValue = rawValue?.ToString();

                    // If the raw value exists but the parameter wasn't bound, it means parsing failed
                    if (!string.IsNullOrEmpty(stringValue) &&
                        !context.ActionArguments.ContainsKey(parameterName))
                    {
                        _logger.LogWarning("Invalid GUID format for parameter {ParameterName}: {Value}",
                            parameterName, stringValue);

                        context.Result = new BadRequestObjectResult(new
                        {
                            success = false,
                            error = $"Invalid {parameterName} format. Expected a valid GUID.",
                            statusCode = 400
                        });
                        return;
                    }
                }
            }
        }

        // Validate query parameters for common issues
        ValidateQueryParameters(context);
    }

    public void OnActionExecuted(ActionExecutedContext context)
    {
        // Not needed for this filter
    }

    private void ValidateQueryParameters(ActionExecutingContext context)
    {
        var request = context.HttpContext.Request;

        // Check for common problematic query parameters
        foreach (var queryParam in request.Query)
        {
            var key = queryParam.Key.ToLowerInvariant();
            var values = queryParam.Value;

            // Validate pagination parameters
            if (key is "pagenumber" or "pagesize")
            {
                foreach (var value in values)
                {
                    if (int.TryParse(value, out var intValue) && intValue <= 0)
                    {
                        _logger.LogWarning("Invalid {ParameterName}: {Value}", key, value);

                        context.Result = new BadRequestObjectResult(new
                        {
                            success = false,
                            error = $"Invalid {key}. Must be greater than 0.",
                            statusCode = 400
                        });
                        return;
                    }
                }
            }

            // Validate GUID query parameters
            if (key.EndsWith("id") && !string.IsNullOrEmpty(values.FirstOrDefault()))
            {
                var value = values.FirstOrDefault();
                if (!Guid.TryParse(value, out _))
                {
                    _logger.LogWarning("Invalid GUID format for query parameter {ParameterName}: {Value}",
                        key, value);

                    context.Result = new BadRequestObjectResult(new
                    {
                        success = false,
                        error = $"Invalid {key} format. Expected a valid GUID.",
                        statusCode = 400
                    });
                    return;
                }
            }
        }
    }
}
