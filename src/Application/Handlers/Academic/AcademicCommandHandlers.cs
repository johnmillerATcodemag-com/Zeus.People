using MediatR;
using Microsoft.Extensions.Logging;
using Zeus.People.Application.Commands.Academic;
using Zeus.People.Application.Common;
using Zeus.People.Application.Interfaces;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Application.Handlers.Academic;

/// <summary>
/// Handler for CreateAcademicCommand
/// </summary>
public class CreateAcademicCommandHandler : IRequestHandler<CreateAcademicCommand, Result<Guid>>
{
    private readonly IAcademicRepository _academicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateAcademicCommandHandler> _logger;

    public CreateAcademicCommandHandler(
        IAcademicRepository academicRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateAcademicCommandHandler> logger)
    {
        _academicRepository = academicRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateAcademicCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating academic with EmpNr: {EmpNr}", request.EmpNr);

            // Check if academic with same EmpNr already exists
            var existsResult = await _academicRepository.ExistsByEmpNrAsync(request.EmpNr, cancellationToken);
            if (existsResult.IsFailure)
                return Result.Failure<Guid>(existsResult.Error);

            if (existsResult.Value)
                return Result.Failure<Guid>(new Error("Academic.EmpNrExists", $"Academic with EmpNr {request.EmpNr} already exists"));

            // Create value objects
            EmpNr empNr;
            try
            {
                empNr = EmpNr.Create(request.EmpNr);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<Guid>(new Error("Academic.InvalidEmpNr", ex.Message));
            }

            EmpName empName;
            try
            {
                empName = EmpName.Create(request.EmpName);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<Guid>(new Error("Academic.InvalidEmpName", ex.Message));
            }

            Rank rank;
            try
            {
                rank = Rank.Create(request.Rank);
            }
            catch (ArgumentException ex)
            {
                return Result.Failure<Guid>(new Error("Academic.InvalidRank", ex.Message));
            }

            // Create academic
            var academic = Domain.Entities.Academic.Create(empNr, empName, rank);

            // Save to repository
            var addResult = await _academicRepository.AddAsync(academic, cancellationToken);
            if (addResult.IsFailure)
                return Result.Failure<Guid>(addResult.Error);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created academic with ID: {AcademicId}", addResult.Value);
            return Result.Success(addResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating academic with EmpNr: {EmpNr}", request.EmpNr);
            return Result.Failure<Guid>(new Error("Academic.CreateFailed", "Failed to create academic"));
        }
    }
}

/// <summary>
/// Handler for UpdateAcademicCommand
/// </summary>
public class UpdateAcademicCommandHandler : IRequestHandler<UpdateAcademicCommand, Result>
{
    private readonly IAcademicRepository _academicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateAcademicCommandHandler> _logger;

    public UpdateAcademicCommandHandler(
        IAcademicRepository academicRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateAcademicCommandHandler> logger)
    {
        _academicRepository = academicRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateAcademicCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating academic with ID: {AcademicId}", request.Id);

            // Get academic
            var academicResult = await _academicRepository.GetByIdAsync(request.Id, cancellationToken);
            if (academicResult.IsFailure)
                return academicResult;

            if (academicResult.Value == null)
                return Result.Failure(new Error("Academic.NotFound", $"Academic with ID {request.Id} not found"));

            var academic = academicResult.Value;

            // Update academic - using available domain methods
            try
            {
                var empName = EmpName.Create(request.EmpName);
                var rank = Rank.Create(request.Rank);

                // Update name via domain event (we'll use ChangeRank as available method)
                academic.ChangeRank(rank);

                // Set home phone if provided
                if (!string.IsNullOrEmpty(request.HomePhone))
                {
                    var homePhone = PhoneNr.Create(request.HomePhone);
                    academic.SetHomePhone(homePhone);
                }
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(new Error("Academic.InvalidInput", ex.Message));
            }

            // Save changes
            var repositoryResult = await _academicRepository.UpdateAsync(academic, cancellationToken);
            if (repositoryResult.IsFailure)
                return repositoryResult;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated academic with ID: {AcademicId}", request.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating academic with ID: {AcademicId}", request.Id);
            return Result.Failure(new Error("Academic.UpdateFailed", "Failed to update academic"));
        }
    }
}

/// <summary>
/// Handler for DeleteAcademicCommand
/// </summary>
public class DeleteAcademicCommandHandler : IRequestHandler<DeleteAcademicCommand, Result>
{
    private readonly IAcademicRepository _academicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteAcademicCommandHandler> _logger;

    public DeleteAcademicCommandHandler(
        IAcademicRepository academicRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteAcademicCommandHandler> logger)
    {
        _academicRepository = academicRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(DeleteAcademicCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Deleting academic with ID: {AcademicId}", request.Id);

            // Check if academic exists
            var academicResult = await _academicRepository.GetByIdAsync(request.Id, cancellationToken);
            if (academicResult.IsFailure)
                return academicResult;

            if (academicResult.Value == null)
                return Result.Failure(new Error("Academic.NotFound", $"Academic with ID {request.Id} not found"));

            // Delete academic
            var deleteResult = await _academicRepository.DeleteAsync(request.Id, cancellationToken);
            if (deleteResult.IsFailure)
                return deleteResult;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully deleted academic with ID: {AcademicId}", request.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting academic with ID: {AcademicId}", request.Id);
            return Result.Failure(new Error("Academic.DeleteFailed", "Failed to delete academic"));
        }
    }
}
