using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries;
using Zeus.People.Application.Common;
using Zeus.People.Application.Queries.Academic;

namespace Zeus.People.Application.Queries.Extension;

/// <summary>
/// Query to get an extension by ID
/// </summary>
public sealed record GetExtensionQuery(
    Guid Id
) : IQuery<Result<ExtensionDto>>;

/// <summary>
/// Query to get an extension by extension number
/// </summary>
public sealed record GetExtensionByNumberQuery(
    string ExtensionNumber
) : IQuery<Result<ExtensionDto>>;

/// <summary>
/// Query to get all extensions
/// </summary>
public sealed record GetAllExtensionsQuery(
    int PageNumber = 1,
    int PageSize = 10,
    string? ExtensionNumberFilter = null,
    string? AccessLevelFilter = null,
    bool? IsInUseFilter = null
) : IQuery<Result<PagedResult<ExtensionDto>>>;

/// <summary>
/// Query to get extension access level information
/// </summary>
public sealed record GetExtensionAccessLevelQuery(
    Guid? ExtensionId = null
) : IQuery<Result<List<ExtensionAccessLevelDto>>>;

/// <summary>
/// Query to get available extensions
/// </summary>
public sealed record GetAvailableExtensionsQuery(
    string? AccessLevelFilter = null,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<Result<PagedResult<ExtensionDto>>>;

/// <summary>
/// Query to get extensions by access level
/// </summary>
public sealed record GetExtensionsByAccessLevelQuery(
    string AccessLevelCode,
    int PageNumber = 1,
    int PageSize = 10
) : IQuery<Result<PagedResult<ExtensionDto>>>;
