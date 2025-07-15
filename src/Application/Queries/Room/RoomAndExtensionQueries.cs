using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries;
using Zeus.People.Application.Common;
using Zeus.People.Application.Queries.Academic;

namespace Zeus.People.Application.Queries.Room;

/// <summary>
/// Query to get a room by ID
/// </summary>
public sealed record GetRoomQuery(
    Guid Id
) : IQuery<Result<RoomDto>>;

/// <summary>
/// Query to get a room by room number
/// </summary>
public sealed record GetRoomByNumberQuery(
    string RoomNumber
) : IQuery<Result<RoomDto>>;

/// <summary>
/// Query to get all rooms
/// </summary>
public sealed record GetAllRoomsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? RoomNumberFilter = null,
    bool? IsOccupiedFilter = null
) : IQuery<Result<PagedResult<RoomDto>>>;

/// <summary>
/// Query to get room occupancy information
/// </summary>
public sealed record GetRoomOccupancyQuery(
    Guid? RoomId = null
) : IQuery<Result<List<RoomOccupancyDto>>>;

/// <summary>
/// Query to get available rooms
/// </summary>
public sealed record GetAvailableRoomsQuery(
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<Result<PagedResult<RoomDto>>>;

/// <summary>
/// Query to get rooms by building
/// </summary>
public sealed record GetRoomsByBuildingQuery(
    Guid BuildingId,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<Result<PagedResult<RoomDto>>>;

/// <summary>
/// Query to get an extension by ID
/// </summary>
public sealed record GetExtensionQuery(
    Guid Id
) : IQuery<Result<ExtensionDto>>;

/// <summary>
/// Query to get an extension by extension number
/// </summary>
public sealed record GetExtensionByNumberQuery(
    string ExtensionNumber
) : IQuery<Result<ExtensionDto>>;

/// <summary>
/// Query to get all extensions
/// </summary>
public sealed record GetAllExtensionsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? ExtensionNumberFilter = null,
    string? AccessLevelFilter = null,
    bool? IsInUseFilter = null
) : IQuery<Result<PagedResult<ExtensionDto>>>;

/// <summary>
/// Query to get extension access level information
/// </summary>
public sealed record GetExtensionAccessLevelQuery(
    Guid? ExtensionId = null
) : IQuery<Result<List<ExtensionAccessLevelDto>>>;

/// <summary>
/// Query to get available extensions
/// </summary>
public sealed record GetAvailableExtensionsQuery(
    string? AccessLevelFilter = null,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<Result<PagedResult<ExtensionDto>>>;

/// <summary>
/// Query to get extensions by access level
/// </summary>
public sealed record GetExtensionsByAccessLevelQuery(
    string AccessLevelCode,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<Result<PagedResult<ExtensionDto>>>;
