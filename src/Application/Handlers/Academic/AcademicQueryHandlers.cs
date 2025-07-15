using MediatR;
using Microsoft.Extensions.Logging;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Interfaces;
using Zeus.People.Application.Queries.Academic;

namespace Zeus.People.Application.Handlers.Academic;

/// <summary>
/// Handler for GetAcademicQuery
/// </summary>
public class GetAcademicQueryHandler : IRequestHandler<GetAcademicQuery, Result<AcademicDto>>
{
    private readonly IAcademicReadRepository _academicReadRepository;
    private readonly ILogger<GetAcademicQueryHandler> _logger;

    public GetAcademicQueryHandler(
        IAcademicReadRepository academicReadRepository,
        ILogger<GetAcademicQueryHandler> logger)
    {
        _academicReadRepository = academicReadRepository;
        _logger = logger;
    }

    public async Task<Result<AcademicDto>> Handle(GetAcademicQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting academic with ID: {AcademicId}", request.Id);

            var result = await _academicReadRepository.GetByIdAsync(request.Id, cancellationToken);
            if (result.IsFailure)
                return result;

            if (result.Value == null)
                return Result.Failure<AcademicDto>(new Error("Academic.NotFound", $"Academic with ID {request.Id} not found"));

            return Result.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting academic with ID: {AcademicId}", request.Id);
            return Result.Failure<AcademicDto>(new Error("Academic.GetFailed", "Failed to get academic"));
        }
    }
}

/// <summary>
/// Handler for GetAcademicByEmpNrQuery
/// </summary>
public class GetAcademicByEmpNrQueryHandler : IRequestHandler<GetAcademicByEmpNrQuery, Result<AcademicDto>>
{
    private readonly IAcademicReadRepository _academicReadRepository;
    private readonly ILogger<GetAcademicByEmpNrQueryHandler> _logger;

    public GetAcademicByEmpNrQueryHandler(
        IAcademicReadRepository academicReadRepository,
        ILogger<GetAcademicByEmpNrQueryHandler> logger)
    {
        _academicReadRepository = academicReadRepository;
        _logger = logger;
    }

    public async Task<Result<AcademicDto>> Handle(GetAcademicByEmpNrQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting academic with EmpNr: {EmpNr}", request.EmpNr);

            var result = await _academicReadRepository.GetByEmpNrAsync(request.EmpNr, cancellationToken);
            if (result.IsFailure)
                return result;

            if (result.Value == null)
                return Result.Failure<AcademicDto>(new Error("Academic.NotFound", $"Academic with EmpNr {request.EmpNr} not found"));

            return Result.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting academic with EmpNr: {EmpNr}", request.EmpNr);
            return Result.Failure<AcademicDto>(new Error("Academic.GetFailed", "Failed to get academic"));
        }
    }
}

/// <summary>
/// Handler for GetAllAcademicsQuery
/// </summary>
public class GetAllAcademicsQueryHandler : IRequestHandler<GetAllAcademicsQuery, Result<PagedResult<AcademicSummaryDto>>>
{
    private readonly IAcademicReadRepository _academicReadRepository;
    private readonly ILogger<GetAllAcademicsQueryHandler> _logger;

    public GetAllAcademicsQueryHandler(
        IAcademicReadRepository academicReadRepository,
        ILogger<GetAllAcademicsQueryHandler> logger)
    {
        _academicReadRepository = academicReadRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> Handle(GetAllAcademicsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting all academics - Page: {PageNumber}, Size: {PageSize}", request.PageNumber, request.PageSize);

            var result = await _academicReadRepository.GetAllAsync(
                request.PageNumber,
                request.PageSize,
                request.NameFilter,
                request.RankFilter,
                request.IsTenuredFilter,
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all academics");
            return Result.Failure<PagedResult<AcademicSummaryDto>>(new Error("Academic.GetAllFailed", "Failed to get academics"));
        }
    }
}

/// <summary>
/// Handler for GetAcademicsByDepartmentQuery
/// </summary>
public class GetAcademicsByDepartmentQueryHandler : IRequestHandler<GetAcademicsByDepartmentQuery, Result<PagedResult<AcademicSummaryDto>>>
{
    private readonly IAcademicReadRepository _academicReadRepository;
    private readonly ILogger<GetAcademicsByDepartmentQueryHandler> _logger;

    public GetAcademicsByDepartmentQueryHandler(
        IAcademicReadRepository academicReadRepository,
        ILogger<GetAcademicsByDepartmentQueryHandler> logger)
    {
        _academicReadRepository = academicReadRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<AcademicSummaryDto>>> Handle(GetAcademicsByDepartmentQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting academics by department: {DepartmentId}", request.DepartmentId);

            var result = await _academicReadRepository.GetByDepartmentAsync(
                request.DepartmentId,
                request.PageNumber,
                request.PageSize,
                cancellationToken);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting academics by department: {DepartmentId}", request.DepartmentId);
            return Result.Failure<PagedResult<AcademicSummaryDto>>(new Error("Academic.GetByDepartmentFailed", "Failed to get academics by department"));
        }
    }
}

/// <summary>
/// Handler for GetAcademicCountByDepartmentQuery
/// </summary>
public class GetAcademicCountByDepartmentQueryHandler : IRequestHandler<GetAcademicCountByDepartmentQuery, Result<List<AcademicCountByDepartmentDto>>>
{
    private readonly IAcademicReadRepository _academicReadRepository;
    private readonly ILogger<GetAcademicCountByDepartmentQueryHandler> _logger;

    public GetAcademicCountByDepartmentQueryHandler(
        IAcademicReadRepository academicReadRepository,
        ILogger<GetAcademicCountByDepartmentQueryHandler> logger)
    {
        _academicReadRepository = academicReadRepository;
        _logger = logger;
    }

    public async Task<Result<List<AcademicCountByDepartmentDto>>> Handle(GetAcademicCountByDepartmentQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting academic count by department");

            var result = await _academicReadRepository.GetCountByDepartmentAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting academic count by department");
            return Result.Failure<List<AcademicCountByDepartmentDto>>(new Error("Academic.GetCountByDepartmentFailed", "Failed to get academic count by department"));
        }
    }
}
