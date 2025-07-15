using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Events;
using Zeus.People.Domain.Exceptions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// Department aggregate root - represents academic departments with attributes like name, budget, and leadership
/// </summary>
public class Department : AggregateRoot
{
    private readonly List<Guid> _academicIds = new();

    // Private constructor for EF
    private Department() { }

    private Department(string name) : base()
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));

        RaiseDomainEvent(new DepartmentCreatedEvent(Id, Name));
    }

    public string Name { get; private set; } = null!;
    public MoneyAmt? ResearchBudget { get; private set; }
    public MoneyAmt? TeachingBudget { get; private set; }
    public PhoneNr? HeadHomePhone { get; private set; }

    // Navigation properties
    public Guid? HeadProfessorId { get; private set; } // Professor who heads the department
    public Guid? ChairId { get; private set; } // Chair associated with the department

    // Collections
    public IReadOnlyList<Guid> AcademicIds => _academicIds.AsReadOnly();

    /// <summary>
    /// Creates a new department
    /// </summary>
    public static Department Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Department name cannot be empty", nameof(name));

        return new Department(name);
    }

    /// <summary>
    /// Sets the research and teaching budgets - business rule: must be positive values
    /// </summary>
    public void SetBudgets(MoneyAmt researchBudget, MoneyAmt teachingBudget)
    {
        ResearchBudget = researchBudget ?? throw new ArgumentNullException(nameof(researchBudget));
        TeachingBudget = teachingBudget ?? throw new ArgumentNullException(nameof(teachingBudget));

        RaiseDomainEvent(new DepartmentBudgetsSetEvent(Id, researchBudget, teachingBudget));
    }

    /// <summary>
    /// Assigns a professor as the department head
    /// Business rule: Professor who heads a Dept must work for that Dept
    /// </summary>
    public void AssignHead(Guid professorId, PhoneNr homePhone)
    {
        if (professorId == Guid.Empty) throw new ArgumentException("Professor ID cannot be empty", nameof(professorId));

        // Business rule check: Professor must work for this department
        if (!_academicIds.Contains(professorId))
        {
            throw new BusinessRuleViolationException("Professor who heads a Dept must work for that Dept");
        }

        HeadProfessorId = professorId;
        HeadHomePhone = homePhone ?? throw new ArgumentNullException(nameof(homePhone));

        RaiseDomainEvent(new ProfessorAssignedAsDepartmentHeadEvent(Id, professorId));
    }

    /// <summary>
    /// Assigns a chair to the department
    /// </summary>
    public void AssignChair(Guid chairId)
    {
        if (chairId == Guid.Empty) throw new ArgumentException("Chair ID cannot be empty", nameof(chairId));

        ChairId = chairId;

        RaiseDomainEvent(new ChairAssignedToDepartmentEvent(Id, chairId));
    }

    /// <summary>
    /// Adds an academic to the department
    /// Business rule: Each Academic that works for a Dept must have a unique EmpName in that Dept
    /// </summary>
    public void AddAcademic(Guid academicId)
    {
        if (academicId == Guid.Empty) throw new ArgumentException("Academic ID cannot be empty", nameof(academicId));

        if (!_academicIds.Contains(academicId))
        {
            _academicIds.Add(academicId);
        }
    }

    /// <summary>
    /// Removes an academic from the department
    /// </summary>
    public void RemoveAcademic(Guid academicId)
    {
        _academicIds.Remove(academicId);

        // If the removed academic was the head, clear the head assignment
        if (HeadProfessorId == academicId)
        {
            HeadProfessorId = null;
            HeadHomePhone = null;
        }
    }

    /// <summary>
    /// Validates that the academic name is unique within this department
    /// This method would typically be called by the domain service
    /// </summary>
    public void ValidateUniqueEmpNameInDepartment(EmpName empName, IEnumerable<EmpName> existingEmpNamesInDept)
    {
        if (existingEmpNamesInDept.Contains(empName))
        {
            throw new BusinessRuleViolationException($"Each Academic that works for a Dept must have a unique EmpName in that Dept. '{empName}' already exists in department '{Name}'");
        }
    }

    // Computed properties for business rule: Dept employs academics of Rank in Quantity
    public int GetProfessorCount(IEnumerable<Academic> academics)
        => academics.Count(a => _academicIds.Contains(a.Id) && a.IsProfessor);

    public int GetSeniorLecturerCount(IEnumerable<Academic> academics)
        => academics.Count(a => _academicIds.Contains(a.Id) && a.IsSeniorLecturer);

    public int GetLecturerCount(IEnumerable<Academic> academics)
        => academics.Count(a => _academicIds.Contains(a.Id) && a.IsLecturer);
}
