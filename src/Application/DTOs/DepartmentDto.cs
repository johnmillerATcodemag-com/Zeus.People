namespace Zeus.People.Application.DTOs;

/// <summary>
/// Department data transfer object
/// </summary>
public class DepartmentDto : BaseDto
{
    public string Name { get; set; } = null!;
    public decimal? ResearchBudget { get; set; }
    public decimal? TeachingBudget { get; set; }
    public string? HeadHomePhone { get; set; }

    // Navigation properties
    public Guid? HeadProfessorId { get; set; }
    public string? HeadProfessorName { get; set; }
    public Guid? ChairId { get; set; }
    public string? ChairName { get; set; }

    // Collections
    public List<AcademicSummaryDto> Academics { get; set; } = new();
    public int TotalAcademics { get; set; }
    public int ProfessorCount { get; set; }
    public int SeniorLecturerCount { get; set; }
    public int LecturerCount { get; set; }
}

/// <summary>
/// Department summary DTO for list views
/// </summary>
public class DepartmentSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? HeadProfessorName { get; set; }
    public int TotalAcademics { get; set; }
    public decimal? ResearchBudget { get; set; }
    public decimal? TeachingBudget { get; set; }
}

/// <summary>
/// Department staff count DTO
/// </summary>
public class DepartmentStaffCountDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = null!;
    public int ProfessorCount { get; set; }
    public int SeniorLecturerCount { get; set; }
    public int LecturerCount { get; set; }
    public int TotalStaffCount { get; set; }
}
