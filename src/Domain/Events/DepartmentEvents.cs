using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Events;

/// <summary>
/// Event raised when a department is created
/// </summary>
public class DepartmentCreatedEvent : DomainEvent
{
    public Guid DepartmentId { get; }
    public string Name { get; }

    public DepartmentCreatedEvent(Guid departmentId, string name)
    {
        DepartmentId = departmentId;
        Name = name;
    }
}

/// <summary>
/// Event raised when a professor is assigned as department head
/// </summary>
public class ProfessorAssignedAsDepartmentHeadEvent : DomainEvent
{
    public Guid DepartmentId { get; }
    public Guid ProfessorId { get; }

    public ProfessorAssignedAsDepartmentHeadEvent(Guid departmentId, Guid professorId)
    {
        DepartmentId = departmentId;
        ProfessorId = professorId;
    }
}

/// <summary>
/// Event raised when department budgets are set
/// </summary>
public class DepartmentBudgetsSetEvent : DomainEvent
{
    public Guid DepartmentId { get; }
    public MoneyAmt ResearchBudget { get; }
    public MoneyAmt TeachingBudget { get; }

    public DepartmentBudgetsSetEvent(Guid departmentId, MoneyAmt researchBudget, MoneyAmt teachingBudget)
    {
        DepartmentId = departmentId;
        ResearchBudget = researchBudget;
        TeachingBudget = teachingBudget;
    }
}

/// <summary>
/// Event raised when a chair is assigned to a department
/// </summary>
public class ChairAssignedToDepartmentEvent : DomainEvent
{
    public Guid DepartmentId { get; }
    public Guid ChairId { get; }

    public ChairAssignedToDepartmentEvent(Guid departmentId, Guid chairId)
    {
        DepartmentId = departmentId;
        ChairId = chairId;
    }
}
