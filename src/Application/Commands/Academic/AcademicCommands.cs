using Zeus.People.Application.Commands;
using Zeus.People.Application.Common;

namespace Zeus.People.Application.Commands.Academic;

/// <summary>
/// Command to create a new academic
/// </summary>
public sealed record CreateAcademicCommand(
    string EmpNr,
    string EmpName,
    string Rank
) : ICommand<Result<Guid>>;

/// <summary>
/// Command to update an academic
/// </summary>
public sealed record UpdateAcademicCommand(
    Guid Id,
    string EmpName,
    string Rank,
    string? HomePhone
) : ICommand<Result>;

/// <summary>
/// Command to delete an academic
/// </summary>
public sealed record DeleteAcademicCommand(
    Guid Id
) : ICommand<Result>;

/// <summary>
/// Command to assign an academic to a room
/// </summary>
public sealed record AssignAcademicToRoomCommand(
    Guid AcademicId,
    Guid RoomId
) : ICommand<Result>;

/// <summary>
/// Command to assign an academic to an extension
/// </summary>
public sealed record AssignAcademicToExtensionCommand(
    Guid AcademicId,
    Guid ExtensionId
) : ICommand<Result>;

/// <summary>
/// Command to set academic tenure status
/// </summary>
public sealed record SetAcademicTenureCommand(
    Guid AcademicId,
    bool IsTenured
) : ICommand<Result>;

/// <summary>
/// Command to set academic contract end date
/// </summary>
public sealed record SetContractEndCommand(
    Guid AcademicId,
    DateTime? ContractEndDate
) : ICommand<Result>;

/// <summary>
/// Command to assign academic to department
/// </summary>
public sealed record AssignAcademicToDepartmentCommand(
    Guid AcademicId,
    Guid DepartmentId
) : ICommand<Result>;

/// <summary>
/// Command to assign academic to chair
/// </summary>
public sealed record AssignAcademicToChairCommand(
    Guid AcademicId,
    Guid ChairId
) : ICommand<Result>;

/// <summary>
/// Command to add degree to academic
/// </summary>
public sealed record AddDegreeToAcademicCommand(
    Guid AcademicId,
    Guid DegreeId
) : ICommand<Result>;

/// <summary>
/// Command to add subject to academic
/// </summary>
public sealed record AddSubjectToAcademicCommand(
    Guid AcademicId,
    Guid SubjectId
) : ICommand<Result>;
