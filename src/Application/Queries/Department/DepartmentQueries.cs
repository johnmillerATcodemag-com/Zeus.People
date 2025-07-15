using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries;
using Zeus.People.Application.Common;
using Zeus.People.Application.Queries.Academic;

namespace Zeus.People.Application.Queries.Department;

/// <summary>
/// Query to get a department by ID
/// </summary>
public sealed record GetDepartmentQuery(
    Guid Id
) : IQuery<Result<DepartmentDto>>;

/// <summary>
/// Query to get a department by name
/// </summary>
public sealed record GetDepartmentByNameQuery(
    string Name
) : IQuery<Result<DepartmentDto>>;

/// <summary>
/// Query to get all departments
/// </summary>
public sealed record GetAllDepartmentsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? NameFilter = null
) : IQuery<Result<PagedResult<DepartmentSummaryDto>>>;

/// <summary>
/// Query to get department staff count
/// </summary>
public sealed record GetDepartmentStaffCountQuery(
    Guid DepartmentId
) : IQuery<Result<DepartmentStaffCountDto>>;

/// <summary>
/// Query to get all department staff counts
/// </summary>
public sealed record GetAllDepartmentStaffCountsQuery() : IQuery<Result<List<DepartmentStaffCountDto>>>;

/// <summary>
/// Query to get departments with budget information
/// </summary>
public sealed record GetDepartmentsWithBudgetQuery(
    decimal? MinResearchBudget = null,
    decimal? MinTeachingBudget = null
) : IQuery<Result<List<DepartmentSummaryDto>>>;

/// <summary>
/// Query to get departments without heads
/// </summary>
public sealed record GetDepartmentsWithoutHeadsQuery() : IQuery<Result<List<DepartmentSummaryDto>>>;
