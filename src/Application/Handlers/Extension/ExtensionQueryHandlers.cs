using MediatR;
using Microsoft.Extensions.Logging;
using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Interfaces;
using Zeus.People.Application.Queries.Extension;
using Zeus.People.Application.Queries.Academic;

namespace Zeus.People.Application.Handlers.Extension;

/// <summary>
/// Handler for GetExtensionQuery
/// </summary>
public class GetExtensionQueryHandler : IRequestHandler<GetExtensionQuery, Result<ExtensionDto>>
{
    private readonly IExtensionReadRepository _extensionReadRepository;
    private readonly ILogger<GetExtensionQueryHandler> _logger;

    public GetExtensionQueryHandler(
        IExtensionReadRepository extensionReadRepository,
        ILogger<GetExtensionQueryHandler> logger)
    {
        _extensionReadRepository = extensionReadRepository;
        _logger = logger;
    }

    public async Task<Result<ExtensionDto>> Handle(GetExtensionQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting extension with ID: {ExtensionId}", request.Id);

            var result = await _extensionReadRepository.GetByIdAsync(request.Id, cancellationToken);
            if (result.IsFailure)
                return result;

            if (result.Value == null)
                return Result.Failure<ExtensionDto>(new Error("Extension.NotFound", $"Extension with ID {request.Id} not found"));

            return Result.Success(result.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting extension with ID: {ExtensionId}", request.Id);
            return Result.Failure<ExtensionDto>(new Error("Extension.GetFailed", "Failed to get extension"));
        }
    }
}

/// <summary>
/// Handler for GetExtensionAccessLevelQuery
/// </summary>
public class GetExtensionAccessLevelQueryHandler : IRequestHandler<GetExtensionAccessLevelQuery, Result<List<ExtensionAccessLevelDto>>>
{
    private readonly IExtensionReadRepository _extensionReadRepository;
    private readonly ILogger<GetExtensionAccessLevelQueryHandler> _logger;

    public GetExtensionAccessLevelQueryHandler(
        IExtensionReadRepository extensionReadRepository,
        ILogger<GetExtensionAccessLevelQueryHandler> logger)
    {
        _extensionReadRepository = extensionReadRepository;
        _logger = logger;
    }

    public async Task<Result<List<ExtensionAccessLevelDto>>> Handle(GetExtensionAccessLevelQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting extension access level information");

            var result = await _extensionReadRepository.GetAccessLevelAsync(request.ExtensionId, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting extension access level");
            return Result.Failure<List<ExtensionAccessLevelDto>>(new Error("Extension.GetAccessLevelFailed", "Failed to get extension access level"));
        }
    }
}

/// <summary>
/// Handler for GetAvailableExtensionsQuery
/// </summary>
public class GetAvailableExtensionsQueryHandler : IRequestHandler<GetAvailableExtensionsQuery, Result<PagedResult<ExtensionDto>>>
{
    private readonly IExtensionReadRepository _extensionReadRepository;
    private readonly ILogger<GetAvailableExtensionsQueryHandler> _logger;

    public GetAvailableExtensionsQueryHandler(
        IExtensionReadRepository extensionReadRepository,
        ILogger<GetAvailableExtensionsQueryHandler> logger)
    {
        _extensionReadRepository = extensionReadRepository;
        _logger = logger;
    }

    public async Task<Result<PagedResult<ExtensionDto>>> Handle(GetAvailableExtensionsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting available extensions");

            var result = await _extensionReadRepository.GetAvailableAsync(
                request.AccessLevelFilter,
                request.PageNumber,
                request.PageSize,
                cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available extensions");
            return Result.Failure<PagedResult<ExtensionDto>>(new Error("Extension.GetAvailableFailed", "Failed to get available extensions"));
        }
    }
}
