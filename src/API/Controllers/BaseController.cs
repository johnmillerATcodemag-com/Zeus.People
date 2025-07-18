using MediatR;
using Microsoft.AspNetCore.Mvc;
using Zeus.People.Application.Common;

namespace Zeus.People.API.Controllers;

/// <summary>
/// Base controller for API endpoints
/// </summary>
[ApiController]
[Route("api/[controller]")]
public abstract class BaseController : ControllerBase
{
    private IMediator? _mediator;

    /// <summary>
    /// Gets the mediator instance
    /// </summary>
    protected IMediator Mediator =>
        _mediator ??= HttpContext.RequestServices.GetRequiredService<IMediator>();

    /// <summary>
    /// Handles a Result object and returns appropriate HTTP response with typed result
    /// </summary>
    /// <typeparam name="T">Type of the result</typeparam>
    /// <param name="result">The result to handle</param>
    /// <returns>An appropriate action result</returns>
    protected ActionResult<T> HandleResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Handles a Result object and returns IActionResult
    /// </summary>
    /// <typeparam name="T">Type of the result</typeparam>
    /// <param name="result">The result to handle</param>
    /// <returns>An IActionResult</returns>
    protected IActionResult HandleResultAsIActionResult<T>(Result<T> result)
    {
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Handles a Result object without return value
    /// </summary>
    /// <param name="result">The result to handle</param>
    /// <returns>An appropriate action result</returns>
    protected ActionResult HandleResult(Result result)
    {
        if (result.IsSuccess)
        {
            return Ok();
        }

        return BadRequest(new { error = result.Error.Message });
    }

    /// <summary>
    /// Creates an action result based on the provided result
    /// </summary>
    /// <typeparam name="T">Type of the result</typeparam>
    /// <param name="result">The result to convert</param>
    /// <returns>An appropriate action result</returns>
    protected ActionResult<T> CreateResponse<T>(T result)
    {
        if (result == null)
            return NotFound();

        return Ok(result);
    }

    /// <summary>
    /// Creates an action result for commands that don't return data
    /// </summary>
    /// <param name="success">Whether the operation was successful</param>
    /// <returns>An appropriate action result</returns>
    protected ActionResult CreateResponse(bool success)
    {
        return success ? Ok() : BadRequest();
    }
}
