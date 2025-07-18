using Zeus.People.Application.Commands;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;

namespace Zeus.People.Application.Commands.Room;

public record CreateRoomCommand(RoomDto Room) : ICommand<Result<RoomDto>>;
public record UpdateRoomCommand(RoomDto Room) : ICommand<Result<RoomDto>>;
public record DeleteRoomCommand(Guid Id) : ICommand<Result<bool>>;
public record AssignRoomToAcademicCommand(Guid RoomId, Guid AcademicId) : ICommand<Result<bool>>;
public record UnassignRoomCommand(Guid RoomId) : ICommand<Result<bool>>;
