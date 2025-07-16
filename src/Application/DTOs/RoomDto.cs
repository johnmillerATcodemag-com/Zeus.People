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
