using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Events;
using Zeus.People.Domain.Exceptions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// Academic aggregate root - represents faculty members with attributes like empNr, EmpName, Rank, and contract details
/// </summary>
public class Academic : AggregateRoot
{
    private readonly List<Guid> _subjectIds = new();
    private readonly List<Guid> _degreeIds = new();

    // Private constructor for EF
    private Academic() { }

    private Academic(EmpNr empNr, EmpName empName, Rank rank) : base()
    {
        EmpNr = empNr ?? throw new ArgumentNullException(nameof(empNr));
        EmpName = empName ?? throw new ArgumentNullException(nameof(empName));
        Rank = rank ?? throw new ArgumentNullException(nameof(rank));
        IsTenured = false;

        RaiseDomainEvent(new AcademicCreatedEvent(Id, EmpNr, EmpName, Rank));
    }

    public EmpNr EmpNr { get; private set; } = null!;
    public EmpName EmpName { get; private set; } = null!;
    public Rank Rank { get; private set; } = null!;
    public bool IsTenured { get; private set; }
    public DateTime? ContractEndDate { get; private set; }
    public PhoneNr? HomePhone { get; private set; }

    // Navigation properties
    public Guid? DepartmentId { get; private set; }
    public Guid? RoomId { get; private set; }
    public Guid? ExtensionId { get; private set; }
    public Guid? ChairId { get; private set; } // For professors who hold a chair

    // Collections
    public IReadOnlyList<Guid> SubjectIds => _subjectIds.AsReadOnly();
    public IReadOnlyList<Guid> DegreeIds => _degreeIds.AsReadOnly();

    /// <summary>
    /// Creates a new academic
    /// </summary>
    public static Academic Create(EmpNr empNr, EmpName empName, Rank rank)
    {
        return new Academic(empNr, empName, rank);
    }

    /// <summary>
    /// Changes the academic's rank
    /// </summary>
    public void ChangeRank(Rank newRank)
    {
        if (newRank == null) throw new ArgumentNullException(nameof(newRank));

        var oldRank = Rank;
        Rank = newRank;

        RaiseDomainEvent(new AcademicRankChangedEvent(Id, oldRank, newRank));
    }

    /// <summary>
    /// Assigns the academic to a department
    /// </summary>
    public void AssignToDepartment(Guid departmentId)
    {
        if (departmentId == Guid.Empty) throw new ArgumentException("Department ID cannot be empty", nameof(departmentId));

        DepartmentId = departmentId;

        RaiseDomainEvent(new AcademicAssignedToDepartmentEvent(Id, departmentId));
    }

    /// <summary>
    /// Assigns the academic to a room
    /// </summary>
    public void AssignToRoom(Guid roomId)
    {
        if (roomId == Guid.Empty) throw new ArgumentException("Room ID cannot be empty", nameof(roomId));

        RoomId = roomId;

        RaiseDomainEvent(new AcademicAssignedToRoomEvent(Id, roomId));
    }

    /// <summary>
    /// Assigns an extension to the academic
    /// </summary>
    public void AssignExtension(Guid extensionId)
    {
        if (extensionId == Guid.Empty) throw new ArgumentException("Extension ID cannot be empty", nameof(extensionId));

        ExtensionId = extensionId;
    }

    /// <summary>
    /// Makes the academic tenured - business rule: tenured academics cannot have contract end date
    /// </summary>
    public void MakeTenured()
    {
        if (ContractEndDate.HasValue)
        {
            throw new BusinessRuleViolationException("Academic who is tenured must not have a Date indicating their contract end");
        }

        IsTenured = true;

        RaiseDomainEvent(new AcademicTenuredEvent(Id));
    }

    /// <summary>
    /// Sets the contract end date - business rule: tenured academics cannot have contract end date
    /// </summary>
    public void SetContractEndDate(DateTime contractEndDate)
    {
        if (IsTenured)
        {
            throw new BusinessRuleViolationException("Academic who is tenured must not have a Date indicating their contract end");
        }

        ContractEndDate = contractEndDate;

        RaiseDomainEvent(new AcademicContractEndDateSetEvent(Id, contractEndDate));
    }

    /// <summary>
    /// Sets the home phone number
    /// </summary>
    public void SetHomePhone(PhoneNr phoneNr)
    {
        HomePhone = phoneNr;
    }

    /// <summary>
    /// Assigns a chair to a professor - business rule: only professors can hold chairs
    /// </summary>
    public void AssignChair(Guid chairId)
    {
        if (!IsProfessor)
        {
            throw new BusinessRuleViolationException("Only professors can hold chairs");
        }

        ChairId = chairId;
    }

    /// <summary>
    /// Removes the chair assignment
    /// </summary>
    public void RemoveChair()
    {
        ChairId = null;
    }

    /// <summary>
    /// Adds a subject that this academic teaches
    /// </summary>
    public void AddSubject(Guid subjectId)
    {
        if (subjectId == Guid.Empty) throw new ArgumentException("Subject ID cannot be empty", nameof(subjectId));

        if (!_subjectIds.Contains(subjectId))
        {
            _subjectIds.Add(subjectId);
        }
    }

    /// <summary>
    /// Removes a subject from this academic's teaching list
    /// </summary>
    public void RemoveSubject(Guid subjectId)
    {
        _subjectIds.Remove(subjectId);
    }

    /// <summary>
    /// Adds a degree to this academic's qualifications
    /// </summary>
    public void AddDegree(Guid degreeId)
    {
        if (degreeId == Guid.Empty) throw new ArgumentException("Degree ID cannot be empty", nameof(degreeId));

        if (!_degreeIds.Contains(degreeId))
        {
            _degreeIds.Add(degreeId);
        }
    }

    // Computed properties
    public bool IsProfessor => Rank.IsProfessor;
    public bool IsSeniorLecturer => Rank.IsSeniorLecturer;
    public bool IsLecturer => Rank.IsLecturer;
    public bool IsTeacher => _subjectIds.Count > 0; // Teacher is an Academic who teaches some Subject
    public bool IsTeachingProfessor => IsProfessor && IsTeacher;

    /// <summary>
    /// Gets the access level ensured by this academic's rank
    /// </summary>
    public AccessLevel GetEnsuredAccessLevel() => Rank.GetEnsuredAccessLevel();
}
