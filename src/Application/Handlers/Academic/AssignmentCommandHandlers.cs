using MediatR;
using Microsoft.Extensions.Logging;
using Zeus.People.Application.Commands.Academic;
using Zeus.People.Application.Common;
using Zeus.People.Application.Interfaces;

namespace Zeus.People.Application.Handlers.Academic;

/// <summary>
/// Handler for AssignAcademicToRoomCommand
/// </summary>
public class AssignAcademicToRoomCommandHandler : IRequestHandler<AssignAcademicToRoomCommand, Result>
{
    private readonly IAcademicRepository _academicRepository;
    private readonly IRoomRepository _roomRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignAcademicToRoomCommandHandler> _logger;

    public AssignAcademicToRoomCommandHandler(
        IAcademicRepository academicRepository,
        IRoomRepository roomRepository,
        IUnitOfWork unitOfWork,
        ILogger<AssignAcademicToRoomCommandHandler> logger)
    {
        _academicRepository = academicRepository;
        _roomRepository = roomRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignAcademicToRoomCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Assigning academic {AcademicId} to room {RoomId}", request.AcademicId, request.RoomId);

            // Get academic
            var academicResult = await _academicRepository.GetByIdAsync(request.AcademicId, cancellationToken);
            if (academicResult.IsFailure)
                return academicResult;

            if (academicResult.Value == null)
                return Result.Failure(new Error("Academic.NotFound", $"Academic with ID {request.AcademicId} not found"));

            // Get room
            var roomResult = await _roomRepository.GetByIdAsync(request.RoomId, cancellationToken);
            if (roomResult.IsFailure)
                return roomResult;

            if (roomResult.Value == null)
                return Result.Failure(new Error("Room.NotFound", $"Room with ID {request.RoomId} not found"));

            // Check if room is already occupied
            var isOccupiedResult = await _roomRepository.IsRoomOccupiedAsync(request.RoomId, cancellationToken);
            if (isOccupiedResult.IsFailure)
                return isOccupiedResult;

            if (isOccupiedResult.Value)
                return Result.Failure(new Error("Room.AlreadyOccupied", $"Room with ID {request.RoomId} is already occupied"));

            // Assign room to academic
            var academic = academicResult.Value;
            try
            {
                academic.AssignToRoom(request.RoomId);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(new Error("Academic.AssignToRoomFailed", ex.Message));
            }

            // Save changes
            var updateResult = await _academicRepository.UpdateAsync(academic, cancellationToken);
            if (updateResult.IsFailure)
                return updateResult;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully assigned academic {AcademicId} to room {RoomId}", request.AcademicId, request.RoomId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning academic {AcademicId} to room {RoomId}", request.AcademicId, request.RoomId);
            return Result.Failure(new Error("Academic.AssignToRoomFailed", "Failed to assign academic to room"));
        }
    }
}

/// <summary>
/// Handler for AssignAcademicToExtensionCommand
/// </summary>
public class AssignAcademicToExtensionCommandHandler : IRequestHandler<AssignAcademicToExtensionCommand, Result>
{
    private readonly IAcademicRepository _academicRepository;
    private readonly IExtensionRepository _extensionRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignAcademicToExtensionCommandHandler> _logger;

    public AssignAcademicToExtensionCommandHandler(
        IAcademicRepository academicRepository,
        IExtensionRepository extensionRepository,
        IUnitOfWork unitOfWork,
        ILogger<AssignAcademicToExtensionCommandHandler> logger)
    {
        _academicRepository = academicRepository;
        _extensionRepository = extensionRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignAcademicToExtensionCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Assigning academic {AcademicId} to extension {ExtensionId}", request.AcademicId, request.ExtensionId);

            // Get academic
            var academicResult = await _academicRepository.GetByIdAsync(request.AcademicId, cancellationToken);
            if (academicResult.IsFailure)
                return academicResult;

            if (academicResult.Value == null)
                return Result.Failure(new Error("Academic.NotFound", $"Academic with ID {request.AcademicId} not found"));

            // Get extension
            var extensionResult = await _extensionRepository.GetByIdAsync(request.ExtensionId, cancellationToken);
            if (extensionResult.IsFailure)
                return extensionResult;

            if (extensionResult.Value == null)
                return Result.Failure(new Error("Extension.NotFound", $"Extension with ID {request.ExtensionId} not found"));

            // Check if extension is already in use
            var isInUseResult = await _extensionRepository.IsExtensionInUseAsync(request.ExtensionId, cancellationToken);
            if (isInUseResult.IsFailure)
                return isInUseResult;

            if (isInUseResult.Value)
                return Result.Failure(new Error("Extension.AlreadyInUse", $"Extension with ID {request.ExtensionId} is already in use"));

            // Assign extension to academic
            var academic = academicResult.Value;
            try
            {
                academic.AssignExtension(request.ExtensionId);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(new Error("Academic.AssignToExtensionFailed", ex.Message));
            }

            // Save changes
            var updateResult = await _academicRepository.UpdateAsync(academic, cancellationToken);
            if (updateResult.IsFailure)
                return updateResult;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully assigned academic {AcademicId} to extension {ExtensionId}", request.AcademicId, request.ExtensionId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning academic {AcademicId} to extension {ExtensionId}", request.AcademicId, request.ExtensionId);
            return Result.Failure(new Error("Academic.AssignToExtensionFailed", "Failed to assign academic to extension"));
        }
    }
}

/// <summary>
/// Handler for SetAcademicTenureCommand
/// </summary>
public class SetAcademicTenureCommandHandler : IRequestHandler<SetAcademicTenureCommand, Result>
{
    private readonly IAcademicRepository _academicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetAcademicTenureCommandHandler> _logger;

    public SetAcademicTenureCommandHandler(
        IAcademicRepository academicRepository,
        IUnitOfWork unitOfWork,
        ILogger<SetAcademicTenureCommandHandler> logger)
    {
        _academicRepository = academicRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(SetAcademicTenureCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Setting tenure status for academic {AcademicId} to {IsTenured}", request.AcademicId, request.IsTenured);

            // Get academic
            var academicResult = await _academicRepository.GetByIdAsync(request.AcademicId, cancellationToken);
            if (academicResult.IsFailure)
                return academicResult;

            if (academicResult.Value == null)
                return Result.Failure(new Error("Academic.NotFound", $"Academic with ID {request.AcademicId} not found"));

            // Set tenure
            var academic = academicResult.Value;
            try
            {
                if (request.IsTenured)
                {
                    academic.MakeTenured();
                }
                else
                {
                    // Note: Domain doesn't have a method to remove tenure
                    // This would need to be added to the domain model
                    return Result.Failure(new Error("Academic.RemoveTenureNotSupported", "Removing tenure is not currently supported"));
                }
            }
            catch (Exception ex)
            {
                return Result.Failure(new Error("Academic.SetTenureFailed", ex.Message));
            }

            // Save changes
            var updateResult = await _academicRepository.UpdateAsync(academic, cancellationToken);
            if (updateResult.IsFailure)
                return updateResult;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully set tenure status for academic {AcademicId} to {IsTenured}", request.AcademicId, request.IsTenured);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting tenure for academic {AcademicId}", request.AcademicId);
            return Result.Failure(new Error("Academic.SetTenureFailed", "Failed to set academic tenure"));
        }
    }
}

/// <summary>
/// Handler for SetContractEndCommand
/// </summary>
public class SetContractEndCommandHandler : IRequestHandler<SetContractEndCommand, Result>
{
    private readonly IAcademicRepository _academicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<SetContractEndCommandHandler> _logger;

    public SetContractEndCommandHandler(
        IAcademicRepository academicRepository,
        IUnitOfWork unitOfWork,
        ILogger<SetContractEndCommandHandler> logger)
    {
        _academicRepository = academicRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(SetContractEndCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Setting contract end date for academic {AcademicId} to {ContractEndDate}",
                request.AcademicId, request.ContractEndDate);

            // Get academic
            var academicResult = await _academicRepository.GetByIdAsync(request.AcademicId, cancellationToken);
            if (academicResult.IsFailure)
                return academicResult;

            if (academicResult.Value == null)
                return Result.Failure(new Error("Academic.NotFound", $"Academic with ID {request.AcademicId} not found"));

            // Set contract end date
            var academic = academicResult.Value;
            try
            {
                if (request.ContractEndDate.HasValue)
                {
                    academic.SetContractEndDate(request.ContractEndDate.Value);
                }
                else
                {
                    // Note: Domain doesn't have a method to clear contract end date
                    // This would need to be added to the domain model
                    return Result.Failure(new Error("Academic.ClearContractEndDateNotSupported", "Clearing contract end date is not currently supported"));
                }
            }
            catch (Exception ex)
            {
                return Result.Failure(new Error("Academic.SetContractEndFailed", ex.Message));
            }

            // Save changes
            var updateResult = await _academicRepository.UpdateAsync(academic, cancellationToken);
            if (updateResult.IsFailure)
                return updateResult;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully set contract end date for academic {AcademicId}", request.AcademicId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting contract end date for academic {AcademicId}", request.AcademicId);
            return Result.Failure(new Error("Academic.SetContractEndFailed", "Failed to set contract end date"));
        }
    }
}
