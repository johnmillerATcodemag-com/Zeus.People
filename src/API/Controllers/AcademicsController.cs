using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeus.People.Application.Commands.Academic;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries.Academic;

namespace Zeus.People.API.Controllers;

/// <summary>
/// Controller for academic management operations
/// </summary>
[Authorize]
public class AcademicsController : BaseController
{
    /// <summary>
    /// Gets all academics with optional filtering and pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <param name="nameFilter">Optional name filter</param>
    /// <param name="rankFilter">Optional rank filter</param>
    /// <param name="isTenuredFilter">Optional tenure status filter</param>
    /// <returns>Paginated list of academics</returns>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AcademicSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AcademicSummaryDto>>> GetAllAcademics(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? nameFilter = null,
        [FromQuery] string? rankFilter = null,
        [FromQuery] bool? isTenuredFilter = null)
    {
        var query = new GetAllAcademicsQuery(pageNumber, pageSize, nameFilter, rankFilter, isTenuredFilter);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets an academic by ID
    /// </summary>
    /// <param name="id">Academic ID</param>
    /// <returns>Academic details</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AcademicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AcademicDto>> GetAcademic(Guid id)
    {
        var query = new GetAcademicQuery(id);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets an academic by employee number
    /// </summary>
    /// <param name="empNr">Employee number</param>
    /// <returns>Academic details</returns>
    [HttpGet("by-emp-nr/{empNr}")]
    [ProducesResponseType(typeof(AcademicDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AcademicDto>> GetAcademicByEmpNr(string empNr)
    {
        var query = new GetAcademicByEmpNrQuery(empNr);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Gets academics by department with pagination
    /// </summary>
    /// <param name="departmentId">Department ID</param>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of academics in the department</returns>
    [HttpGet("by-department/{departmentId:guid}")]
    [ProducesResponseType(typeof(PagedResult<AcademicSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<AcademicSummaryDto>>> GetAcademicsByDepartment(
        Guid departmentId,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var query = new GetAcademicsByDepartmentQuery(departmentId, pageNumber, pageSize);
        var result = await Mediator.Send(query);
        return HandleResult(result);
    }

    /// <summary>
    /// Creates a new academic
    /// </summary>
    /// <param name="command">Academic creation data</param>
    /// <returns>Created academic ID</returns>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateAcademic([FromBody] CreateAcademicCommand command)
    {
        var result = await Mediator.Send(command);

        if (result.IsSuccess)
        {
            return CreatedAtAction(nameof(GetAcademic), new { id = result.Value }, result.Value);
        }

        return HandleResult(result);
    }

    /// <summary>
    /// Updates an existing academic
    /// </summary>
    /// <param name="id">Academic ID</param>
    /// <param name="command">Academic update data</param>
    /// <returns>No content on success</returns>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> UpdateAcademic(Guid id, [FromBody] UpdateAcademicCommand command)
    {
        if (id != command.Id)
            return BadRequest("ID mismatch");

        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Deletes an academic
    /// </summary>
    /// <param name="id">Academic ID</param>
    /// <returns>No content on success</returns>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult> DeleteAcademic(Guid id)
    {
        var command = new DeleteAcademicCommand(id);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Assigns an academic to a room
    /// </summary>
    /// <param name="academicId">Academic ID</param>
    /// <param name="roomId">Room ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{academicId:guid}/assign-room/{roomId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AssignToRoom(Guid academicId, Guid roomId)
    {
        var command = new AssignAcademicToRoomCommand(academicId, roomId);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Assigns an extension to an academic
    /// </summary>
    /// <param name="academicId">Academic ID</param>
    /// <param name="extensionId">Extension ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{academicId:guid}/assign-extension/{extensionId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AssignExtension(Guid academicId, Guid extensionId)
    {
        var command = new AssignAcademicToExtensionCommand(academicId, extensionId);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Sets the tenure status of an academic
    /// </summary>
    /// <param name="academicId">Academic ID</param>
    /// <param name="isTenured">Tenure status</param>
    /// <returns>No content on success</returns>
    [HttpPost("{academicId:guid}/set-tenure")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetTenure(Guid academicId, [FromBody] bool isTenured)
    {
        var command = new SetAcademicTenureCommand(academicId, isTenured);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Sets the contract end date for an academic
    /// </summary>
    /// <param name="academicId">Academic ID</param>
    /// <param name="contractEndDate">Contract end date</param>
    /// <returns>No content on success</returns>
    [HttpPost("{academicId:guid}/set-contract-end")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> SetContractEnd(Guid academicId, [FromBody] DateTime? contractEndDate)
    {
        var command = new SetContractEndCommand(academicId, contractEndDate);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Assigns an academic to a department
    /// </summary>
    /// <param name="academicId">Academic ID</param>
    /// <param name="departmentId">Department ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{academicId:guid}/assign-department/{departmentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AssignToDepartment(Guid academicId, Guid departmentId)
    {
        var command = new AssignAcademicToDepartmentCommand(academicId, departmentId);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Assigns a chair to an academic (professors only)
    /// </summary>
    /// <param name="academicId">Academic ID</param>
    /// <param name="chairId">Chair ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{academicId:guid}/assign-chair/{chairId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AssignToChair(Guid academicId, Guid chairId)
    {
        var command = new AssignAcademicToChairCommand(academicId, chairId);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Adds a degree to an academic
    /// </summary>
    /// <param name="academicId">Academic ID</param>
    /// <param name="degreeId">Degree ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{academicId:guid}/add-degree/{degreeId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddDegree(Guid academicId, Guid degreeId)
    {
        var command = new AddDegreeToAcademicCommand(academicId, degreeId);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }

    /// <summary>
    /// Adds a subject to an academic
    /// </summary>
    /// <param name="academicId">Academic ID</param>
    /// <param name="subjectId">Subject ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{academicId:guid}/add-subject/{subjectId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> AddSubject(Guid academicId, Guid subjectId)
    {
        var command = new AddSubjectToAcademicCommand(academicId, subjectId);
        var result = await Mediator.Send(command);
        return HandleResult(result);
    }
}
