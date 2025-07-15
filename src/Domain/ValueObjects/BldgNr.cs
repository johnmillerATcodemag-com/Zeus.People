using FluentValidation;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Building Number value object
/// </summary>
public class BldgNr : ValueObject
{
    public string Value { get; private set; }

    private BldgNr(string value)
    {
        Value = value;
    }

    public static BldgNr Create(string value)
    {
        var validator = new BldgNrValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid building number: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new BldgNr(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(BldgNr bldgNr) => bldgNr.Value;
}

/// <summary>
/// Validator for building numbers
/// </summary>
public class BldgNrValidator : AbstractValidator<string>
{
    public BldgNrValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Building number cannot be empty")
            .MaximumLength(20).WithMessage("Building number cannot exceed 20 characters");
    }
}
