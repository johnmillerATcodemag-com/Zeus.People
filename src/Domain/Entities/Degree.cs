using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Exceptions;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// Degree entity - represents academic qualifications obtained by academics
/// Business rule: An Academic obtains that Degree from at most one University
/// </summary>
public class Degree : AggregateRoot
{
    private readonly List<AcademicDegree> _academicDegrees = new();

    // Private constructor for EF
    private Degree() { }

    private Degree(string code) : base()
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
    }

    public string Code { get; private set; } = null!;

    // Collections
    public IReadOnlyList<AcademicDegree> AcademicDegrees => _academicDegrees.AsReadOnly();

    /// <summary>
    /// Creates a new degree
    /// </summary>
    public static Degree Create(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Degree code cannot be empty", nameof(code));

        return new Degree(code);
    }

    /// <summary>
    /// Records that an academic obtained this degree from a university
    /// Business rule: An Academic obtains that Degree from at most one University
    /// </summary>
    public void AddAcademicObtainment(Guid academicId, Guid universityId)
    {
        if (academicId == Guid.Empty) throw new ArgumentException("Academic ID cannot be empty", nameof(academicId));
        if (universityId == Guid.Empty) throw new ArgumentException("University ID cannot be empty", nameof(universityId));

        // Check if academic already has this degree from another university
        var existingDegree = _academicDegrees.FirstOrDefault(ad => ad.AcademicId == academicId);
        if (existingDegree != null)
        {
            throw new BusinessRuleViolationException("An Academic obtains that Degree from at most one University");
        }

        _academicDegrees.Add(new AcademicDegree(academicId, Id, universityId));
    }

    /// <summary>
    /// Removes an academic's obtainment of this degree
    /// </summary>
    public void RemoveAcademicObtainment(Guid academicId)
    {
        _academicDegrees.RemoveAll(ad => ad.AcademicId == academicId);
    }
}

/// <summary>
/// Academic Degree value object - represents the relationship between Academic, Degree, and University
/// </summary>
public class AcademicDegree
{
    public Guid AcademicId { get; }
    public Guid DegreeId { get; }
    public Guid UniversityId { get; }

    public AcademicDegree(Guid academicId, Guid degreeId, Guid universityId)
    {
        AcademicId = academicId;
        DegreeId = degreeId;
        UniversityId = universityId;
    }
}
