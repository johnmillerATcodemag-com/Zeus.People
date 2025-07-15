using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Exceptions;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// Chair entity - represents leadership positions held by professors
/// Business rule: Chair is held by at most one Professor
/// </summary>
public class Chair : AggregateRoot
{
    // Private constructor for EF
    private Chair() { }

    private Chair(string name) : base()
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; private set; } = null!;
    public Guid? ProfessorId { get; private set; } // At most one professor

    /// <summary>
    /// Creates a new chair
    /// </summary>
    public static Chair Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Chair name cannot be empty", nameof(name));

        return new Chair(name);
    }

    /// <summary>
    /// Assigns this chair to a professor
    /// Business rule: Chair is held by at most one Professor
    /// </summary>
    public void AssignToProfessor(Guid professorId)
    {
        if (professorId == Guid.Empty) throw new ArgumentException("Professor ID cannot be empty", nameof(professorId));

        if (ProfessorId.HasValue)
        {
            throw new BusinessRuleViolationException("Chair is held by at most one Professor");
        }

        ProfessorId = professorId;
    }

    /// <summary>
    /// Removes the professor assignment from this chair
    /// </summary>
    public void RemoveProfessorAssignment()
    {
        ProfessorId = null;
    }

    /// <summary>
    /// Checks if the chair is currently held by a professor
    /// </summary>
    public bool IsHeld => ProfessorId.HasValue;
}
