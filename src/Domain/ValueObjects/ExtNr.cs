using FluentValidation;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Extension Number value object
/// </summary>
public class ExtNr : ValueObject
{
    public string Value { get; private set; }

    private ExtNr(string value)
    {
        Value = value;
    }

    public static ExtNr Create(string value)
    {
        var validator = new ExtNrValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid extension number: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new ExtNr(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(ExtNr extNr) => extNr.Value;
}

/// <summary>
/// Validator for extension numbers
/// </summary>
public class ExtNrValidator : AbstractValidator<string>
{
    public ExtNrValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Extension number cannot be empty")
            .MaximumLength(10).WithMessage("Extension number cannot exceed 10 characters")
            .Matches(@"^\d+$").WithMessage("Extension number can only contain digits");
    }
}
