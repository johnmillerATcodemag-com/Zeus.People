namespace Zeus.People.Application.DTOs;

/// <summary>
/// Room data transfer object
/// </summary>
public class RoomDto : BaseDto
{
    public string RoomNumber { get; set; } = null!;
    public int Capacity { get; set; }
    public string? RoomType { get; set; }

    // Navigation properties
    public Guid? BuildingId { get; set; }
    public string? BuildingName { get; set; }
    public string? BuildingNumber { get; set; }

    // Occupancy
    public Guid? OccupiedByAcademicId { get; set; }
    public string? OccupiedByAcademicName { get; set; }
    public bool IsOccupied { get; set; }
}

/// <summary>
/// Room occupancy DTO
/// </summary>
public class RoomOccupancyDto
{
    public Guid RoomId { get; set; }
    public string RoomNumber { get; set; } = null!;
    public string? BuildingName { get; set; }
    public bool IsOccupied { get; set; }
    public Guid? OccupiedByAcademicId { get; set; }
    public string? OccupiedByAcademicName { get; set; }
    public string? OccupiedByEmpNr { get; set; }
    public DateTime? OccupiedSince { get; set; }
}

/// <summary>
/// Extension data transfer object
/// </summary>
public class ExtensionDto : BaseDto
{
    public string ExtensionNumber { get; set; } = null!;
    public string AccessLevelCode { get; set; } = null!;
    public string AccessLevelDescription { get; set; } = null!;

    // Usage
    public Guid? UsedByAcademicId { get; set; }
    public string? UsedByAcademicName { get; set; }
    public string? UsedByEmpNr { get; set; }
    public bool IsInUse { get; set; }
}

/// <summary>
/// Extension access level DTO
/// </summary>
public class ExtensionAccessLevelDto
{
    public Guid ExtensionId { get; set; }
    public string ExtensionNumber { get; set; } = null!;
    public string AccessLevelCode { get; set; } = null!;
    public string AccessLevelDescription { get; set; } = null!;
    public bool IsInUse { get; set; }
    public string? UsedByAcademicName { get; set; }
}
