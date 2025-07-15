using FluentValidation;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Building Name value object
/// </summary>
public class BldgName : ValueObject
{
    public string Value { get; private set; }

    private BldgName(string value)
    {
        Value = value;
    }

    public static BldgName Create(string value)
    {
        var validator = new BldgNameValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid building name: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new BldgName(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(BldgName bldgName) => bldgName.Value;
}

/// <summary>
/// Validator for building names
/// </summary>
public class BldgNameValidator : AbstractValidator<string>
{
    public BldgNameValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Building name cannot be empty")
            .MaximumLength(100).WithMessage("Building name cannot exceed 100 characters");
    }
}
