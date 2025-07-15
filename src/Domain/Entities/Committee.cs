using Zeus.People.Domain.Entities;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// Committee entity - represents committees served by teaching professors
/// Business rule: Committee is served by zero or more TeachingProfessor
/// </summary>
public class Committee : AggregateRoot
{
    private readonly List<Guid> _teachingProfessorIds = new();

    // Private constructor for EF
    private Committee() { }

    private Committee(string name) : base()
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }

    public string Name { get; private set; } = null!;

    // Collections
    public IReadOnlyList<Guid> TeachingProfessorIds => _teachingProfessorIds.AsReadOnly();

    /// <summary>
    /// Creates a new committee
    /// </summary>
    public static Committee Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Committee name cannot be empty", nameof(name));

        return new Committee(name);
    }

    /// <summary>
    /// Adds a teaching professor to serve on this committee
    /// Business rule: Only teaching professors can serve on committees
    /// </summary>
    public void AddTeachingProfessor(Guid professorId)
    {
        if (professorId == Guid.Empty) throw new ArgumentException("Professor ID cannot be empty", nameof(professorId));

        if (!_teachingProfessorIds.Contains(professorId))
        {
            _teachingProfessorIds.Add(professorId);
        }
    }

    /// <summary>
    /// Removes a teaching professor from this committee
    /// </summary>
    public void RemoveTeachingProfessor(Guid professorId)
    {
        _teachingProfessorIds.Remove(professorId);
    }
}
