namespace Zeus.People.Application.DTOs;

/// <summary>
/// Base class for all Data Transfer Objects
/// </summary>
public abstract class BaseDto
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime ModifiedAt { get; set; }
}
