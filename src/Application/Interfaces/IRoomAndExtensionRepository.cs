using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries.Academic;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Application.Interfaces;

/// <summary>
/// Repository interface for Room aggregate
/// </summary>
public interface IRoomRepository
{
    Task<Result<Room?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Room?>> GetByRoomNumberAsync(string roomNumber, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsRoomOccupiedAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<Result<List<Room>>> GetAvailableRoomsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only repository interface for Room queries
/// </summary>
public interface IRoomReadRepository
{
    Task<Result<RoomDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<RoomDto>> GetByRoomNumberAsync(string roomNumber, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<RoomDto>>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? roomNumberFilter = null,
        bool? isOccupiedFilter = null,
        CancellationToken cancellationToken = default);
    Task<Result<List<RoomOccupancyDto>>> GetOccupancyAsync(Guid? roomId = null, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<RoomDto>>> GetAvailableAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<RoomDto>>> GetByBuildingAsync(
        Guid buildingId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Repository interface for Extension aggregate
/// </summary>
public interface IExtensionRepository
{
    Task<Result<Extension?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Extension?>> GetByExtensionNumberAsync(string extensionNumber, CancellationToken cancellationToken = default);
    Task<Result<bool>> IsExtensionInUseAsync(Guid extensionId, CancellationToken cancellationToken = default);
    Task<Result<List<Extension>>> GetAvailableExtensionsAsync(string? accessLevelFilter = null, CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only repository interface for Extension queries
/// </summary>
public interface IExtensionReadRepository
{
    Task<Result<ExtensionDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<ExtensionDto>> GetByExtensionNumberAsync(string extensionNumber, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ExtensionDto>>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? extensionNumberFilter = null,
        string? accessLevelFilter = null,
        bool? isInUseFilter = null,
        CancellationToken cancellationToken = default);
    Task<Result<List<ExtensionAccessLevelDto>>> GetAccessLevelAsync(Guid? extensionId = null, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ExtensionDto>>> GetAvailableAsync(
        string? accessLevelFilter = null,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    Task<Result<PagedResult<ExtensionDto>>> GetByAccessLevelAsync(
        string accessLevelCode,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
}
