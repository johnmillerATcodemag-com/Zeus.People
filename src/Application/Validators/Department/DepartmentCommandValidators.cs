using FluentValidation;
using Zeus.People.Application.Commands.Department;

namespace Zeus.People.Application.Validators.Department;

/// <summary>
/// Validator for CreateDepartmentCommand
/// </summary>
public class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required")
            .Length(1, 100).WithMessage("Department name must be between 1 and 100 characters");
    }
}

/// <summary>
/// Validator for UpdateDepartmentCommand
/// </summary>
public class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Department ID is required");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Department name is required")
            .Length(1, 100).WithMessage("Department name must be between 1 and 100 characters");

        RuleFor(x => x.ResearchBudget)
            .GreaterThan(0).When(x => x.ResearchBudget.HasValue)
            .WithMessage("Research budget must be positive");

        RuleFor(x => x.TeachingBudget)
            .GreaterThan(0).When(x => x.TeachingBudget.HasValue)
            .WithMessage("Teaching budget must be positive");

        RuleFor(x => x.HeadHomePhone)
            .Matches(@"^\d{6,15}$").When(x => !string.IsNullOrEmpty(x.HeadHomePhone))
            .WithMessage("Head home phone must be between 6 and 15 digits");
    }
}

/// <summary>
/// Validator for SetDepartmentBudgetsCommand
/// </summary>
public class SetDepartmentBudgetsCommandValidator : AbstractValidator<SetDepartmentBudgetsCommand>
{
    public SetDepartmentBudgetsCommandValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required");

        RuleFor(x => x.ResearchBudget)
            .GreaterThan(0).WithMessage("Research budget must be positive");

        RuleFor(x => x.TeachingBudget)
            .GreaterThan(0).WithMessage("Teaching budget must be positive");
    }
}

/// <summary>
/// Validator for AssignDepartmentHeadCommand
/// </summary>
public class AssignDepartmentHeadCommandValidator : AbstractValidator<AssignDepartmentHeadCommand>
{
    public AssignDepartmentHeadCommandValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required");

        RuleFor(x => x.ProfessorId)
            .NotEmpty().WithMessage("Professor ID is required");
    }
}
