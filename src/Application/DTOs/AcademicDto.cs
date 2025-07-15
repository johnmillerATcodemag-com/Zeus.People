using Zeus.People.Application.DTOs;

namespace Zeus.People.Application.DTOs;

/// <summary>
/// Academic data transfer object
/// </summary>
public class AcademicDto : BaseDto
{
    public string EmpNr { get; set; } = null!;
    public string EmpName { get; set; } = null!;
    public string Rank { get; set; } = null!;
    public bool IsTenured { get; set; }
    public DateTime? ContractEndDate { get; set; }
    public string? HomePhone { get; set; }

    // Navigation properties
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public Guid? RoomId { get; set; }
    public string? RoomNumber { get; set; }
    public Guid? ExtensionId { get; set; }
    public string? ExtensionNumber { get; set; }
    public Guid? ChairId { get; set; }
    public string? ChairName { get; set; }

    // Collections
    public List<Guid> SubjectIds { get; set; } = new();
    public List<string> SubjectNames { get; set; } = new();
    public List<Guid> DegreeIds { get; set; } = new();
    public List<DegreeDto> Degrees { get; set; } = new();
}

/// <summary>
/// Academic summary DTO for list views
/// </summary>
public class AcademicSummaryDto
{
    public Guid Id { get; set; }
    public string EmpNr { get; set; } = null!;
    public string EmpName { get; set; } = null!;
    public string Rank { get; set; } = null!;
    public bool IsTenured { get; set; }
    public string? DepartmentName { get; set; }
    public string? RoomNumber { get; set; }
}

/// <summary>
/// Academic count by department DTO
/// </summary>
public class AcademicCountByDepartmentDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public int ProfessorCount { get; set; }
    public int SeniorLecturerCount { get; set; }
    public int LecturerCount { get; set; }
    public int TotalCount { get; set; }
}
