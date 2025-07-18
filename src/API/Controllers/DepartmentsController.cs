using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeus.People.Application.Commands.Department;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries.Department;

namespace Zeus.People.API.Controllers;

/// <summary>
/// Controller for department management operations
/// </summary>
[Authorize]
public class DepartmentsController : BaseController
{
    /// <summary>
    /// Gets all departments with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of departments</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<DepartmentSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<DepartmentSummaryDto>>> GetAllDepartments(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAllDepartmentsQuery(pageNumber, pageSize);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets a department by ID
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <returns>Department details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(DepartmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DepartmentDto>> GetDepartment(Guid id)
    {
        var query = new GetDepartmentQuery(id);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets department statistics
    /// </summary>
    /// <returns>Department staff count statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(List<DepartmentStaffCountDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<DepartmentStaffCountDto>>> GetDepartmentStatistics()
    {
        var query = new GetAllDepartmentStaffCountsQuery();
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a new department
    /// </summary>
    /// <param name="command">Department creation data</param>
    /// <returns>Created department ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> CreateDepartment([FromBody] CreateDepartmentCommand command)
    {
        var result = await Mediator.Send(command);
        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetDepartment), new { id = result.Value }, result.Value);
        }
        return HandleResult(result);
    }

    /// <summary>
    /// Updates a department
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <param name="command">Department update data</param>
    /// <returns>No content if successful</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateDepartment(Guid id, [FromBody] UpdateDepartmentCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID in URL must match ID in request body");

        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    /// <summary>
    /// Deletes a department
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <returns>No content if successful</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteDepartment(Guid id)
    {
        var command = new DeleteDepartmentCommand(id);
        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    /// <summary>
    /// Assigns a department head
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <param name="professorId">Professor ID to assign as head</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/assign-head/{professorId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AssignDepartmentHead(Guid id, Guid professorId)
    {
        var command = new AssignDepartmentHeadCommand(id, professorId);
        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }

    /// <summary>
    /// Sets department budgets
    /// </summary>
    /// <param name="id">Department ID</param>
    /// <param name="request">Budget information</param>
    /// <returns>No content if successful</returns>
    [HttpPost("{id:guid}/set-budgets")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetDepartmentBudgets(Guid id, [FromBody] SetBudgetsRequest request)
    {
        var command = new SetDepartmentBudgetsCommand(id, request.ResearchBudget, request.TeachingBudget);
        var result = await Mediator.Send(command);
        return result.IsSuccess ? NoContent() : HandleResult(result);
    }
}

/// <summary>
/// Request model for setting department budgets
/// </summary>
public record SetBudgetsRequest(decimal ResearchBudget, decimal TeachingBudget);
