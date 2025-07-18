using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zeus.People.API.Controllers;
using Zeus.People.Application.Commands.Room;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries.Room;

namespace Zeus.People.API.Controllers;

/// <summary>
/// Rooms controller for managing office rooms
/// </summary>
[Authorize]
public class RoomsController : BaseController
{
    /// <summary>
    /// Get all rooms
    /// </summary>
    /// <returns>List of rooms</returns>
    [HttpGet]
    public async Task<IActionResult> GetRooms()
    {
        var query = new GetAllRoomsQuery();
        var result = await Mediator.Send(query);
        return HandleResultAsIActionResult(result);
    }

    /// <summary>
    /// Get room by ID
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <returns>Room details</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetRoom(Guid id)
    {
        var query = new GetRoomQuery(id);
        var result = await Mediator.Send(query);
        return HandleResultAsIActionResult(result);
    }

    /// <summary>
    /// Get rooms by building
    /// </summary>
    /// <param name="building">Building ID</param>
    /// <returns>List of rooms in the building</returns>
    [HttpGet("building/{building}")]
    public async Task<IActionResult> GetRoomsByBuilding(Guid building)
    {
        var query = new GetRoomsByBuildingQuery(building);
        var result = await Mediator.Send(query);
        return HandleResultAsIActionResult(result);
    }

    /// <summary>
    /// Get available rooms
    /// </summary>
    /// <returns>List of available rooms</returns>
    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableRooms()
    {
        var query = new GetAvailableRoomsQuery();
        var result = await Mediator.Send(query);
        return HandleResultAsIActionResult(result);
    }

    /// <summary>
    /// Get room with occupant
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <returns>Room with occupant details</returns>
    [HttpGet("{id}/occupant")]
    public async Task<IActionResult> GetRoomWithOccupant(Guid id)
    {
        var query = new GetRoomOccupancyQuery(id);
        var result = await Mediator.Send(query);
        return HandleResultAsIActionResult(result);
    }

    /// <summary>
    /// Create a new room
    /// </summary>
    /// <param name="dto">Room data</param>
    /// <returns>Created room</returns>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateRoom([FromBody] RoomDto dto)
    {
        var command = new CreateRoomCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResultAsIActionResult(result);
    }

    /// <summary>
    /// Update an existing room
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <param name="dto">Room data</param>
    /// <returns>Updated room</returns>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateRoom(Guid id, [FromBody] RoomDto dto)
    {
        dto.Id = id;
        var command = new UpdateRoomCommand(dto);
        var result = await Mediator.Send(command);
        return HandleResultAsIActionResult(result);
    }

    /// <summary>
    /// Delete a room
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteRoom(Guid id)
    {
        var command = new DeleteRoomCommand(id);
        var result = await Mediator.Send(command);
        return HandleResultAsIActionResult(result);
    }

    /// <summary>
    /// Assign room to academic
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <param name="academicId">Academic ID</param>
    /// <returns>Success result</returns>
    [HttpPost("{id}/assign/{academicId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AssignRoomToAcademic(Guid id, Guid academicId)
    {
        var command = new AssignRoomToAcademicCommand(id, academicId);
        var result = await Mediator.Send(command);
        return HandleResultAsIActionResult(result);
    }

    /// <summary>
    /// Unassign room from academic
    /// </summary>
    /// <param name="id">Room ID</param>
    /// <returns>Success result</returns>
    [HttpDelete("{id}/assign")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UnassignRoom(Guid id)
    {
        var command = new UnassignRoomCommand(id);
        var result = await Mediator.Send(command);
        return HandleResultAsIActionResult(result);
    }
}
