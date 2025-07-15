namespace Zeus.People.Application.DTOs;

/// <summary>
/// Degree data transfer object
/// </summary>
public class DegreeDto : BaseDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public Guid? UniversityId { get; set; }
    public string? UniversityName { get; set; }
}

/// <summary>
/// University data transfer object
/// </summary>
public class UniversityDto : BaseDto
{
    public string Name { get; set; } = null!;
    public string? Country { get; set; }
    public List<DegreeDto> Degrees { get; set; } = new();
}
