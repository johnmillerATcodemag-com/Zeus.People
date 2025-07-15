using MediatR;
using Microsoft.Extensions.Logging;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Interfaces;
using Zeus.People.Application.Queries.Academic;
using Zeus.People.Application.Queries.Department;

namespace Zeus.People.Application.Handlers.Department;

/// <summary>
/// Handler for GetDepartmentQuery
/// </summary>
public class GetDepartmentQueryHandler : IRequestHandler<GetDepartmentQuery, Result<DepartmentDto>>
{
    private readonly IDepartmentReadRepository _departmentReadRepository;
    private readonly ILogger<GetDepartmentQueryHandler> _logger;

    public GetDepartmentQueryHandler(
        IDepartmentReadRepository departmentReadRepository,
        ILogger<GetDepartmentQueryHandler> logger)
    {
        _departmentReadRepository = departmentReadRepository;
        _logger = logger;
    }

    public async Task<Result<DepartmentDto>> Handle(GetDepartmentQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting department with ID: {DepartmentId}", request.Id);

            var result = await _departmentReadRepository.GetByIdAsync(request.Id, cancellationToken);
            if (result.IsFailure)
                return result;

            if (result.Value == null)
                return Result.Failure<DepartmentDto>(new Error("Department.NotFound", $"Department with ID {request.Id} not found"));

            return Result.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department with ID: {DepartmentId}", request.Id);
            return Result.Failure<DepartmentDto>(new Error("Department.GetFailed", "Failed to get department"));
        }
    }
}

/// <summary>
/// Handler for GetDepartmentByNameQuery
/// </summary>
public class GetDepartmentByNameQueryHandler : IRequestHandler<GetDepartmentByNameQuery, Result<DepartmentDto>>
{
    private readonly IDepartmentReadRepository _departmentReadRepository;
    private readonly ILogger<GetDepartmentByNameQueryHandler> _logger;

    public GetDepartmentByNameQueryHandler(
        IDepartmentReadRepository departmentReadRepository,
        ILogger<GetDepartmentByNameQueryHandler> logger)
    {
        _departmentReadRepository = departmentReadRepository;
        _logger = logger;
    }

    public async Task<Result<DepartmentDto>> Handle(GetDepartmentByNameQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting department with name: {Name}", request.Name);

            var result = await _departmentReadRepository.GetByNameAsync(request.Name, cancellationToken);
            if (result.IsFailure)
                return result;

            if (result.Value == null)
                return Result.Failure<DepartmentDto>(new Error("Department.NotFound", $"Department with name '{request.Name}' not found"));

            return Result.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department with name: {Name}", request.Name);
            return Result.Failure<DepartmentDto>(new Error("Department.GetFailed", "Failed to get department"));
        }
    }
}

/// <summary>
/// Handler for GetAllDepartmentsQuery
/// </summary>
public class GetAllDepartmentsQueryHandler : IRequestHandler<GetAllDepartmentsQuery, Result<PagedResult<DepartmentSummaryDto>>>
{
    private readonly IDepartmentReadRepository _departmentReadRepository;
    private readonly ILogger<GetAllDepartmentsQueryHandler> _logger;

    public GetAllDepartmentsQueryHandler(
        IDepartmentReadRepository departmentReadRepository,
        ILogger<GetAllDepartmentsQueryHandler> logger)
    {
        _departmentReadRepository = departmentReadRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<DepartmentSummaryDto>>> Handle(GetAllDepartmentsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all departments - Page: {PageNumber}, Size: {PageSize}", request.PageNumber, request.PageSize);

            var result = await _departmentReadRepository.GetAllAsync(
                request.PageNumber,
                request.PageSize,
                request.NameFilter,
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all departments");
            return Result.Failure<PagedResult<DepartmentSummaryDto>>(new Error("Department.GetAllFailed", "Failed to get departments"));
        }
    }
}

/// <summary>
/// Handler for GetDepartmentStaffCountQuery
/// </summary>
public class GetDepartmentStaffCountQueryHandler : IRequestHandler<GetDepartmentStaffCountQuery, Result<DepartmentStaffCountDto>>
{
    private readonly IDepartmentReadRepository _departmentReadRepository;
    private readonly ILogger<GetDepartmentStaffCountQueryHandler> _logger;

    public GetDepartmentStaffCountQueryHandler(
        IDepartmentReadRepository departmentReadRepository,
        ILogger<GetDepartmentStaffCountQueryHandler> logger)
    {
        _departmentReadRepository = departmentReadRepository;
        _logger = logger;
    }

    public async Task<Result<DepartmentStaffCountDto>> Handle(GetDepartmentStaffCountQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting staff count for department: {DepartmentId}", request.DepartmentId);

            var result = await _departmentReadRepository.GetStaffCountAsync(request.DepartmentId, cancellationToken);
            if (result.IsFailure)
                return result;

            if (result.Value == null)
                return Result.Failure<DepartmentStaffCountDto>(new Error("Department.NotFound", $"Department with ID {request.DepartmentId} not found"));

            return Result.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting staff count for department: {DepartmentId}", request.DepartmentId);
            return Result.Failure<DepartmentStaffCountDto>(new Error("Department.GetStaffCountFailed", "Failed to get department staff count"));
        }
    }
}

/// <summary>
/// Handler for GetAllDepartmentStaffCountsQuery
/// </summary>
public class GetAllDepartmentStaffCountsQueryHandler : IRequestHandler<GetAllDepartmentStaffCountsQuery, Result<List<DepartmentStaffCountDto>>>
{
    private readonly IDepartmentReadRepository _departmentReadRepository;
    private readonly ILogger<GetAllDepartmentStaffCountsQueryHandler> _logger;

    public GetAllDepartmentStaffCountsQueryHandler(
        IDepartmentReadRepository departmentReadRepository,
        ILogger<GetAllDepartmentStaffCountsQueryHandler> logger)
    {
        _departmentReadRepository = departmentReadRepository;
        _logger = logger;
    }

    public async Task<Result<List<DepartmentStaffCountDto>>> Handle(GetAllDepartmentStaffCountsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all department staff counts");

            var result = await _departmentReadRepository.GetAllStaffCountsAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all department staff counts");
            return Result.Failure<List<DepartmentStaffCountDto>>(new Error("Department.GetAllStaffCountsFailed", "Failed to get department staff counts"));
        }
    }
}
