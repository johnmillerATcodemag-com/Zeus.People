using FluentValidation;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Employee Number value object
/// </summary>
public class EmpNr : ValueObject
{
    public string Value { get; private set; }

    private EmpNr(string value)
    {
        Value = value;
    }

    public static EmpNr Create(string value)
    {
        var validator = new EmpNrValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid employee number: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new EmpNr(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(EmpNr empNr) => empNr.Value;
}

/// <summary>
/// Validator for employee numbers
/// </summary>
public class EmpNrValidator : AbstractValidator<string>
{
    public EmpNrValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Employee number cannot be empty")
            .Length(6).WithMessage("Employee number must be exactly 6 characters")
            .Matches(@"^[A-Z]{2}\d{4}$").WithMessage("Employee number must be 2 uppercase letters followed by 4 digits (e.g., AB1234)");
    }
}
