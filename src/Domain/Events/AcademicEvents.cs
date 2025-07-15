using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Events;

/// <summary>
/// Event raised when an academic is created
/// </summary>
public class AcademicCreatedEvent : DomainEvent
{
    public Guid AcademicId { get; }
    public EmpNr EmpNr { get; }
    public EmpName EmpName { get; }
    public Rank Rank { get; }

    public AcademicCreatedEvent(Guid academicId, EmpNr empNr, EmpName empName, Rank rank)
    {
        AcademicId = academicId;
        EmpNr = empNr;
        EmpName = empName;
        Rank = rank;
    }
}

/// <summary>
/// Event raised when an academic's rank is changed
/// </summary>
public class AcademicRankChangedEvent : DomainEvent
{
    public Guid AcademicId { get; }
    public Rank OldRank { get; }
    public Rank NewRank { get; }

    public AcademicRankChangedEvent(Guid academicId, Rank oldRank, Rank newRank)
    {
        AcademicId = academicId;
        OldRank = oldRank;
        NewRank = newRank;
    }
}

/// <summary>
/// Event raised when an academic is assigned to a department
/// </summary>
public class AcademicAssignedToDepartmentEvent : DomainEvent
{
    public Guid AcademicId { get; }
    public Guid DepartmentId { get; }

    public AcademicAssignedToDepartmentEvent(Guid academicId, Guid departmentId)
    {
        AcademicId = academicId;
        DepartmentId = departmentId;
    }
}

/// <summary>
/// Event raised when an academic is assigned to a room
/// </summary>
public class AcademicAssignedToRoomEvent : DomainEvent
{
    public Guid AcademicId { get; }
    public Guid RoomId { get; }

    public AcademicAssignedToRoomEvent(Guid academicId, Guid roomId)
    {
        AcademicId = academicId;
        RoomId = roomId;
    }
}

/// <summary>
/// Event raised when an academic becomes tenured
/// </summary>
public class AcademicTenuredEvent : DomainEvent
{
    public Guid AcademicId { get; }

    public AcademicTenuredEvent(Guid academicId)
    {
        AcademicId = academicId;
    }
}

/// <summary>
/// Event raised when an academic's contract end date is set
/// </summary>
public class AcademicContractEndDateSetEvent : DomainEvent
{
    public Guid AcademicId { get; }
    public DateTime ContractEndDate { get; }

    public AcademicContractEndDateSetEvent(Guid academicId, DateTime contractEndDate)
    {
        AcademicId = academicId;
        ContractEndDate = contractEndDate;
    }
}
