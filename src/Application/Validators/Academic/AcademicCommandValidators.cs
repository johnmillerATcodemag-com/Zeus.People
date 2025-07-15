using FluentValidation;
using Zeus.People.Application.Commands.Academic;

namespace Zeus.People.Application.Validators.Academic;

/// <summary>
/// Validator for CreateAcademicCommand
/// </summary>
public class CreateAcademicCommandValidator : AbstractValidator<CreateAcademicCommand>
{
    public CreateAcademicCommandValidator()
    {
        RuleFor(x => x.EmpNr)
            .NotEmpty().WithMessage("Employee number is required")
            .Length(1, 10).WithMessage("Employee number must be between 1 and 10 characters")
            .Matches(@"^\d+$").WithMessage("Employee number must contain only digits");

        RuleFor(x => x.EmpName)
            .NotEmpty().WithMessage("Employee name is required")
            .Length(1, 100).WithMessage("Employee name must be between 1 and 100 characters");

        RuleFor(x => x.Rank)
            .NotEmpty().WithMessage("Rank is required")
            .Must(BeValidRank).WithMessage("Rank must be 'P' (Professor), 'SL' (Senior Lecturer), or 'L' (Lecturer)");
    }

    private static bool BeValidRank(string rank)
    {
        return rank is "P" or "SL" or "L";
    }
}

/// <summary>
/// Validator for UpdateAcademicCommand
/// </summary>
public class UpdateAcademicCommandValidator : AbstractValidator<UpdateAcademicCommand>
{
    public UpdateAcademicCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Academic ID is required");

        RuleFor(x => x.EmpName)
            .NotEmpty().WithMessage("Employee name is required")
            .Length(1, 100).WithMessage("Employee name must be between 1 and 100 characters");

        RuleFor(x => x.Rank)
            .NotEmpty().WithMessage("Rank is required")
            .Must(BeValidRank).WithMessage("Rank must be 'P' (Professor), 'SL' (Senior Lecturer), or 'L' (Lecturer)");

        RuleFor(x => x.HomePhone)
            .Matches(@"^\d{6,15}$").When(x => !string.IsNullOrEmpty(x.HomePhone))
            .WithMessage("Home phone must be between 6 and 15 digits");
    }

    private static bool BeValidRank(string rank)
    {
        return rank is "P" or "SL" or "L";
    }
}

/// <summary>
/// Validator for AssignAcademicToRoomCommand
/// </summary>
public class AssignAcademicToRoomCommandValidator : AbstractValidator<AssignAcademicToRoomCommand>
{
    public AssignAcademicToRoomCommandValidator()
    {
        RuleFor(x => x.AcademicId)
            .NotEmpty().WithMessage("Academic ID is required");

        RuleFor(x => x.RoomId)
            .NotEmpty().WithMessage("Room ID is required");
    }
}

/// <summary>
/// Validator for AssignAcademicToExtensionCommand
/// </summary>
public class AssignAcademicToExtensionCommandValidator : AbstractValidator<AssignAcademicToExtensionCommand>
{
    public AssignAcademicToExtensionCommandValidator()
    {
        RuleFor(x => x.AcademicId)
            .NotEmpty().WithMessage("Academic ID is required");

        RuleFor(x => x.ExtensionId)
            .NotEmpty().WithMessage("Extension ID is required");
    }
}

/// <summary>
/// Validator for SetContractEndCommand
/// </summary>
public class SetContractEndCommandValidator : AbstractValidator<SetContractEndCommand>
{
    public SetContractEndCommandValidator()
    {
        RuleFor(x => x.AcademicId)
            .NotEmpty().WithMessage("Academic ID is required");

        RuleFor(x => x.ContractEndDate)
            .GreaterThan(DateTime.Today).When(x => x.ContractEndDate.HasValue)
            .WithMessage("Contract end date must be in the future");
    }
}
