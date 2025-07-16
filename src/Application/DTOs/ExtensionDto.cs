namespace Zeus.People.Application.DTOs;

/// <summary>
/// Data Transfer Object for Extension
/// </summary>
public class ExtensionDto : BaseDto
{
    public string ExtensionNumber { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public bool IsInUse { get; set; }
    public string? Description { get; set; }
}

/// <summary>
/// Data Transfer Object for Extension Access Level information
/// </summary>
public class ExtensionAccessLevelDto
{
    public Guid ExtensionId { get; set; }
    public string ExtensionNumber { get; set; } = string.Empty;
    public string AccessLevel { get; set; } = string.Empty;
    public string AccessLevelDescription { get; set; } = string.Empty;
    public DateTime AssignedDate { get; set; }
    public string? AssignedBy { get; set; }
}
