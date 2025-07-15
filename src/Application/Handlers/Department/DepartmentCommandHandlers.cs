using MediatR;
using Microsoft.Extensions.Logging;
using Zeus.People.Application.Commands.Department;
using Zeus.People.Application.Common;
using Zeus.People.Application.Interfaces;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Application.Handlers.Department;

/// <summary>
/// Handler for CreateDepartmentCommand
/// </summary>
public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateDepartmentCommandHandler> _logger;

    public CreateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateDepartmentCommandHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating department with name: {Name}", request.Name);

            // Check if department with same name already exists
            var existsResult = await _departmentRepository.ExistsByNameAsync(request.Name, cancellationToken);
            if (existsResult.IsFailure)
                return Result.Failure<Guid>(existsResult.Error);

            if (existsResult.Value)
                return Result.Failure<Guid>(new Error("Department.NameExists", $"Department with name '{request.Name}' already exists"));

            // Create department
            var department = Domain.Entities.Department.Create(request.Name);

            // Save to repository
            var addResult = await _departmentRepository.AddAsync(department, cancellationToken);
            if (addResult.IsFailure)
                return Result.Failure<Guid>(addResult.Error);

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created department with ID: {DepartmentId}", addResult.Value);
            return Result.Success(addResult.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating department with name: {Name}", request.Name);
            return Result.Failure<Guid>(new Error("Department.CreateFailed", "Failed to create department"));
        }
    }
}

/// <summary>
/// Handler for UpdateDepartmentCommand
/// </summary>
public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Result>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateDepartmentCommandHandler> _logger;

    public UpdateDepartmentCommandHandler(
        IDepartmentRepository departmentRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateDepartmentCommandHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating department with ID: {DepartmentId}", request.Id);

            // Get department
            var departmentResult = await _departmentRepository.GetByIdAsync(request.Id, cancellationToken);
            if (departmentResult.IsFailure)
                return departmentResult;

            if (departmentResult.Value == null)
                return Result.Failure(new Error("Department.NotFound", $"Department with ID {request.Id} not found"));

            var department = departmentResult.Value;

            // Note: The domain doesn't have an UpdateName method
            // For now, we'll just update budgets and phone if provided
            try
            {
                // Set budgets if provided
                if (request.ResearchBudget.HasValue && request.TeachingBudget.HasValue)
                {
                    var researchBudget = MoneyAmt.Create(request.ResearchBudget.Value);
                    var teachingBudget = MoneyAmt.Create(request.TeachingBudget.Value);
                    department.SetBudgets(researchBudget, teachingBudget);
                }

                // Note: Department name update and head home phone update
                // would need to be added to the domain model
            }
            catch (ArgumentException ex)
            {
                return Result.Failure(new Error("Department.InvalidInput", ex.Message));
            }

            // Save changes
            var repositoryResult = await _departmentRepository.UpdateAsync(department, cancellationToken);
            if (repositoryResult.IsFailure)
                return repositoryResult;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully updated department with ID: {DepartmentId}", request.Id);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating department with ID: {DepartmentId}", request.Id);
            return Result.Failure(new Error("Department.UpdateFailed", "Failed to update department"));
        }
    }
}

/// <summary>
/// Handler for AssignDepartmentHeadCommand
/// </summary>
public class AssignDepartmentHeadCommandHandler : IRequestHandler<AssignDepartmentHeadCommand, Result>
{
    private readonly IDepartmentRepository _departmentRepository;
    private readonly IAcademicRepository _academicRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignDepartmentHeadCommandHandler> _logger;

    public AssignDepartmentHeadCommandHandler(
        IDepartmentRepository departmentRepository,
        IAcademicRepository academicRepository,
        IUnitOfWork unitOfWork,
        ILogger<AssignDepartmentHeadCommandHandler> logger)
    {
        _departmentRepository = departmentRepository;
        _academicRepository = academicRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result> Handle(AssignDepartmentHeadCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Assigning professor {ProfessorId} as head of department {DepartmentId}",
                request.ProfessorId, request.DepartmentId);

            // Get department
            var departmentResult = await _departmentRepository.GetByIdAsync(request.DepartmentId, cancellationToken);
            if (departmentResult.IsFailure)
                return departmentResult;

            if (departmentResult.Value == null)
                return Result.Failure(new Error("Department.NotFound", $"Department with ID {request.DepartmentId} not found"));

            // Get professor
            var professorResult = await _academicRepository.GetByIdAsync(request.ProfessorId, cancellationToken);
            if (professorResult.IsFailure)
                return professorResult;

            if (professorResult.Value == null)
                return Result.Failure(new Error("Academic.NotFound", $"Academic with ID {request.ProfessorId} not found"));

            var professor = professorResult.Value;

            // Validate that the academic is a professor
            if (professor.Rank.Value != "P")
                return Result.Failure(new Error("Academic.NotProfessor", "Only professors can be assigned as department heads"));

            // Business rule: A Professor who heads a Dept must work for that Dept
            if (professor.DepartmentId != request.DepartmentId)
                return Result.Failure(new Error("Department.ProfessorNotInDepartment", "Professor must work for the department to be assigned as head"));

            // Assign head
            var department = departmentResult.Value;
            try
            {
                // Note: The domain AssignHead method requires a home phone
                // For now, we'll use a placeholder or get it from the professor
                // This should ideally be part of the command or fetched from the professor
                var placeholderPhone = PhoneNr.Create("000000"); // This is a design issue
                department.AssignHead(request.ProfessorId, placeholderPhone);
            }
            catch (Exception ex)
            {
                return Result.Failure(new Error("Department.AssignHeadFailed", ex.Message));
            }

            // Save changes
            var updateResult = await _departmentRepository.UpdateAsync(department, cancellationToken);
            if (updateResult.IsFailure)
                return updateResult;

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully assigned professor {ProfessorId} as head of department {DepartmentId}",
                request.ProfessorId, request.DepartmentId);
            return Result.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error assigning department head");
            return Result.Failure(new Error("Department.AssignHeadFailed", "Failed to assign department head"));
        }
    }
}
