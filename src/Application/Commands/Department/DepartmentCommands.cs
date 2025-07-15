using Zeus.People.Application.Commands;
using Zeus.People.Application.Common;

namespace Zeus.People.Application.Commands.Department;

/// <summary>
/// Command to create a new department
/// </summary>
public sealed record CreateDepartmentCommand(
    string Name
) : ICommand<Result<Guid>>;

/// <summary>
/// Command to update a department
/// </summary>
public sealed record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    decimal? ResearchBudget,
    decimal? TeachingBudget,
    string? HeadHomePhone
) : ICommand<Result>;

/// <summary>
/// Command to delete a department
/// </summary>
public sealed record DeleteDepartmentCommand(
    Guid Id
) : ICommand<Result>;

/// <summary>
/// Command to assign department head
/// </summary>
public sealed record AssignDepartmentHeadCommand(
    Guid DepartmentId,
    Guid ProfessorId
) : ICommand<Result>;

/// <summary>
/// Command to set department budgets
/// </summary>
public sealed record SetDepartmentBudgetsCommand(
    Guid DepartmentId,
    decimal ResearchBudget,
    decimal TeachingBudget
) : ICommand<Result>;

/// <summary>
/// Command to assign chair to department
/// </summary>
public sealed record AssignChairToDepartmentCommand(
    Guid DepartmentId,
    Guid ChairId
) : ICommand<Result>;
