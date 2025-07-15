using Zeus.People.Domain.Entities;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// University entity - represents institutions from which degrees are obtained
/// </summary>
public class University : AggregateRoot
{
    private readonly List<Guid> _degreeIds = new();

    // Private constructor for EF
    private University() { }

    private University(string code) : base()
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Code { get; private set; } = null!;

    // Collections
    public IReadOnlyList<Guid> DegreeIds => _degreeIds.AsReadOnly();

    /// <summary>
    /// Creates a new university
    /// </summary>
    public static University Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("University code cannot be empty", nameof(code));

        return new University(code);
    }

    /// <summary>
    /// Adds a degree offered by this university
    /// </summary>
    public void AddDegree(Guid degreeId)
    {
        if (degreeId == Guid.Empty) throw new ArgumentException("Degree ID cannot be empty", nameof(degreeId));

        if (!_degreeIds.Contains(degreeId))
        {
            _degreeIds.Add(degreeId);
        }
    }

    /// <summary>
    /// Removes a degree from this university
    /// </summary>
    public void RemoveDegree(Guid degreeId)
    {
        _degreeIds.Remove(degreeId);
    }
}
