using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Exceptions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// Extension entity - represents communication extensions with access levels
/// Business rule: Extension is used by one and only one Academic
/// Derived rule: Extension provides AccessLevel as Extension is used by an Academic who has a Rank that ensures AccessLevel
/// </summary>
public class Extension : AggregateRoot
{
    // Private constructor for EF
    private Extension() { }

    private Extension(ExtNr extNr) : base()
    {
        ExtNr = extNr ?? throw new ArgumentNullException(nameof(extNr));
    }

    public ExtNr ExtNr { get; private set; } = null!;
    public Guid? AcademicId { get; private set; } // One and only one academic

    /// <summary>
    /// Creates a new extension
    /// </summary>
    public static Extension Create(ExtNr extNr)
    {
        return new Extension(extNr);
    }

    /// <summary>
    /// Assigns this extension to an academic
    /// Business rule: Extension is used by one and only one Academic
    /// </summary>
    public void AssignToAcademic(Guid academicId)
    {
        if (academicId == Guid.Empty) throw new ArgumentException("Academic ID cannot be empty", nameof(academicId));

        if (AcademicId.HasValue)
        {
            throw new BusinessRuleViolationException("Extension is used by one and only one Academic");
        }

        AcademicId = academicId;
    }

    /// <summary>
    /// Removes the academic assignment from this extension
    /// </summary>
    public void RemoveAcademicAssignment()
    {
        AcademicId = null;
    }

    /// <summary>
    /// Gets the access level provided by this extension based on the assigned academic's rank
    /// Derived rule: Extension provides AccessLevel as Extension is used by an Academic who has a Rank that ensures AccessLevel
    /// </summary>
    public AccessLevel GetProvidedAccessLevel(Academic academic)
    {
        if (academic == null) throw new ArgumentNullException(nameof(academic));

        if (AcademicId != academic.Id)
        {
            throw new InvalidDomainOperationException("Extension is not assigned to the provided academic");
        }

        return academic.GetEnsuredAccessLevel();
    }

    /// <summary>
    /// Business rule validation: Extension must be assigned to an academic
    /// </summary>
    public void ValidateHasAcademicAssignment()
    {
        if (!AcademicId.HasValue)
        {
            throw new BusinessRuleViolationException("Extension is used by one and only one Academic");
        }
    }
}
