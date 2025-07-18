using MediatR;
using Microsoft.Extensions.Logging;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Interfaces;
using Zeus.People.Application.Queries.Academic;
using Zeus.People.Application.Queries.Room;

namespace Zeus.People.Application.Handlers.Room;

/// <summary>
/// Handler for GetRoomQuery
/// </summary>
public class GetRoomQueryHandler : IRequestHandler<GetRoomQuery, Result<RoomDto>>
{
    private readonly IRoomReadRepository _roomReadRepository;
    private readonly ILogger<GetRoomQueryHandler> _logger;

    public GetRoomQueryHandler(
        IRoomReadRepository roomReadRepository,
        ILogger<GetRoomQueryHandler> logger)
    {
        _roomReadRepository = roomReadRepository;
        _logger = logger;
    }

    public async Task<Result<RoomDto>> Handle(GetRoomQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting room with ID: {RoomId}", request.Id);

            var result = await _roomReadRepository.GetByIdAsync(request.Id, cancellationToken);
            if (result.IsFailure)
                return result;

            if (result.Value == null)
                return Result.Failure<RoomDto>(new Error("Room.NotFound", $"Room with ID {request.Id} not found"));

            return Result.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room with ID: {RoomId}", request.Id);
            return Result.Failure<RoomDto>(new Error("Room.GetFailed", "Failed to get room"));
        }
    }
}

/// <summary>
/// Handler for GetRoomOccupancyQuery
/// </summary>
public class GetRoomOccupancyQueryHandler : IRequestHandler<GetRoomOccupancyQuery, Result<List<RoomOccupancyDto>>>
{
    private readonly IRoomReadRepository _roomReadRepository;
    private readonly ILogger<GetRoomOccupancyQueryHandler> _logger;

    public GetRoomOccupancyQueryHandler(
        IRoomReadRepository roomReadRepository,
        ILogger<GetRoomOccupancyQueryHandler> logger)
    {
        _roomReadRepository = roomReadRepository;
        _logger = logger;
    }

    public async Task<Result<List<RoomOccupancyDto>>> Handle(GetRoomOccupancyQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting room occupancy information");

            var result = await _roomReadRepository.GetOccupancyAsync(request.RoomId, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting room occupancy");
            return Result.Failure<List<RoomOccupancyDto>>(new Error("Room.GetOccupancyFailed", "Failed to get room occupancy"));
        }
    }
}

/// <summary>
/// Handler for GetAvailableRoomsQuery
/// </summary>
public class GetAvailableRoomsQueryHandler : IRequestHandler<GetAvailableRoomsQuery, Result<PagedResult<RoomDto>>>
{
    private readonly IRoomReadRepository _roomReadRepository;
    private readonly ILogger<GetAvailableRoomsQueryHandler> _logger;

    public GetAvailableRoomsQueryHandler(
        IRoomReadRepository roomReadRepository,
        ILogger<GetAvailableRoomsQueryHandler> logger)
    {
        _roomReadRepository = roomReadRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<RoomDto>>> Handle(GetAvailableRoomsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting available rooms");

            var result = await _roomReadRepository.GetAvailableAsync(request.PageNumber, request.PageSize, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available rooms");
            return Result.Failure<PagedResult<RoomDto>>(new Error("Room.GetAvailableFailed", "Failed to get available rooms"));
        }
    }
}

/// <summary>
/// Handler for GetAllRoomsQuery
/// </summary>
public class GetAllRoomsQueryHandler : IRequestHandler<GetAllRoomsQuery, Result<PagedResult<RoomDto>>>
{
    private readonly IRoomReadRepository _roomReadRepository;
    private readonly ILogger<GetAllRoomsQueryHandler> _logger;

    public GetAllRoomsQueryHandler(
        IRoomReadRepository roomReadRepository,
        ILogger<GetAllRoomsQueryHandler> logger)
    {
        _roomReadRepository = roomReadRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<RoomDto>>> Handle(GetAllRoomsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all rooms - Page: {PageNumber}, Size: {PageSize}", request.PageNumber, request.PageSize);

            var result = await _roomReadRepository.GetAllAsync(
                request.PageNumber,
                request.PageSize,
                request.RoomNumberFilter,
                request.IsOccupiedFilter,
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all rooms");
            return Result.Failure<PagedResult<RoomDto>>(new Error("Room.GetAllFailed", "Failed to get all rooms"));
        }
    }
}
