using FluentValidation;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Employee Name value object
/// </summary>
public class EmpName : ValueObject
{
    public string Value { get; private set; }

    private EmpName(string value)
    {
        Value = value;
    }

    public static EmpName Create(string value)
    {
        var validator = new EmpNameValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid employee name: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new EmpName(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(EmpName empName) => empName.Value;
}

/// <summary>
/// Validator for employee names
/// </summary>
public class EmpNameValidator : AbstractValidator<string>
{
    public EmpNameValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Employee name cannot be empty")
            .MaximumLength(100).WithMessage("Employee name cannot exceed 100 characters")
            .Matches(@"^[a-zA-Z\s\-\.]+$").WithMessage("Employee name can only contain letters, spaces, hyphens, and periods");
    }
}
