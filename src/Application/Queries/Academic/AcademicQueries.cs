using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries;
using Zeus.People.Application.Common;

namespace Zeus.People.Application.Queries.Academic;

/// <summary>
/// Query to get an academic by ID
/// </summary>
public sealed record GetAcademicQuery(
    Guid Id
) : IQuery<Result<AcademicDto>>;

/// <summary>
/// Query to get an academic by employee number
/// </summary>
public sealed record GetAcademicByEmpNrQuery(
    string EmpNr
) : IQuery<Result<AcademicDto>>;

/// <summary>
/// Query to get all academics
/// </summary>
public sealed record GetAllAcademicsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? NameFilter = null,
    string? RankFilter = null,
    bool? IsTenuredFilter = null
) : IQuery<Result<PagedResult<AcademicSummaryDto>>>;

/// <summary>
/// Query to get academics by department
/// </summary>
public sealed record GetAcademicsByDepartmentQuery(
    Guid DepartmentId,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<Result<PagedResult<AcademicSummaryDto>>>;

/// <summary>
/// Query to get academics by rank
/// </summary>
public sealed record GetAcademicsByRankQuery(
    string Rank,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<Result<PagedResult<AcademicSummaryDto>>>;

/// <summary>
/// Query to get tenured academics
/// </summary>
public sealed record GetTenuredAcademicsQuery(
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<Result<PagedResult<AcademicSummaryDto>>>;

/// <summary>
/// Query to get academics with expiring contracts
/// </summary>
public sealed record GetAcademicsWithExpiringContractsQuery(
    DateTime? BeforeDate = null,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<Result<PagedResult<AcademicSummaryDto>>>;

/// <summary>
/// Query to get academic count by department
/// </summary>
public sealed record GetAcademicCountByDepartmentQuery() : IQuery<Result<List<AcademicCountByDepartmentDto>>>;

/// <summary>
/// Paged result wrapper
/// </summary>
public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasNextPage => PageNumber < TotalPages;
    public bool HasPreviousPage => PageNumber > 1;
}
