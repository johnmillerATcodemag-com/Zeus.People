using Zeus.People.Domain.Entities;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// Subject entity - represents subjects taught by academics
/// Business rule: Subject is taught by zero or more Academic
/// </summary>
public class Subject : AggregateRoot
{
    private readonly List<Guid> _academicIds = new();
    private readonly List<Teaching> _teachings = new();

    // Private constructor for EF
    private Subject() { }

    private Subject(string code) : base()
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Code { get; private set; } = null!;

    // Collections
    public IReadOnlyList<Guid> AcademicIds => _academicIds.AsReadOnly();
    public IReadOnlyList<Teaching> Teachings => _teachings.AsReadOnly();

    /// <summary>
    /// Creates a new subject
    /// </summary>
    public static Subject Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Subject code cannot be empty", nameof(code));

        return new Subject(code);
    }

    /// <summary>
    /// Adds an academic as a teacher of this subject
    /// </summary>
    public void AddTeacher(Guid academicId)
    {
        if (academicId == Guid.Empty) throw new ArgumentException("Academic ID cannot be empty", nameof(academicId));

        if (!_academicIds.Contains(academicId))
        {
            _academicIds.Add(academicId);
            _teachings.Add(new Teaching(Id, academicId));
        }
    }

    /// <summary>
    /// Removes an academic from teaching this subject
    /// </summary>
    public void RemoveTeacher(Guid academicId)
    {
        _academicIds.Remove(academicId);
        _teachings.RemoveAll(t => t.AcademicId == academicId);
    }

    /// <summary>
    /// Assigns a rating to a specific teaching
    /// </summary>
    public void AssignTeachingRating(Guid academicId, Rating rating)
    {
        var teaching = _teachings.FirstOrDefault(t => t.AcademicId == academicId);
        if (teaching == null)
        {
            throw new ArgumentException($"Academic {academicId} is not teaching this subject");
        }

        teaching.AssignRating(rating);
    }
}

/// <summary>
/// Teaching value object - represents the fact that an Academic teaches a Subject
/// Business rule: Teaching gets a Rating
/// </summary>
public class Teaching
{
    public Guid SubjectId { get; }
    public Guid AcademicId { get; }
    public Rating? Rating { get; private set; }

    public Teaching(Guid subjectId, Guid academicId)
    {
        SubjectId = subjectId;
        AcademicId = academicId;
    }

    /// <summary>
    /// Assigns a rating to this teaching
    /// </summary>
    public void AssignRating(Rating rating)
    {
        Rating = rating ?? throw new ArgumentNullException(nameof(rating));
    }
}
