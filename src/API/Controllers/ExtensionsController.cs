using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeus.People.Application.Commands.Extension;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries.Extension;

namespace Zeus.People.API.Controllers;

/// <summary>
/// Controller for extension management operations
/// </summary>
[Authorize]
public class ExtensionsController : BaseController
{
    /// <summary>
    /// Gets all extensions with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of extensions</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ExtensionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ExtensionDto>>> GetAllExtensions(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAllExtensionsQuery(pageNumber, pageSize);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets a specific extension by ID
    /// </summary>
    /// <param name="id">Extension ID</param>
    /// <returns>Extension details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ExtensionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExtensionDto>> GetExtension(Guid id)
    {
        var query = new GetExtensionQuery(id);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a new extension
    /// </summary>
    /// <param name="extensionDto">Extension creation data</param>
    /// <returns>Created extension</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ExtensionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ExtensionDto>> CreateExtension([FromBody] ExtensionDto extensionDto)
    {
        var command = new CreateExtensionCommand(extensionDto);
        var result = await Mediator.Send(command);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetExtension), new { id = result.Value.Id }, result.Value);
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Updates an existing extension
    /// </summary>
    /// <param name="id">Extension ID</param>
    /// <param name="extensionDto">Extension update data</param>
    /// <returns>Updated extension</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ExtensionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExtensionDto>> UpdateExtension(Guid id, [FromBody] ExtensionDto extensionDto)
    {
        if (id != extensionDto.Id)
        {
            return BadRequest("ID mismatch");
        }

        var command = new UpdateExtensionCommand(extensionDto);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Deletes an extension
    /// </summary>
    /// <param name="id">Extension ID</param>
    /// <returns>Success status</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteExtension(Guid id)
    {
        var command = new DeleteExtensionCommand(id);
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return NoContent();
        }

        return HandleResultAsIActionResult(result);
    }
}
