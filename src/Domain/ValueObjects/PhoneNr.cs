using FluentValidation;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Phone Number value object
/// </summary>
public class PhoneNr : ValueObject
{
    public string Value { get; private set; }

    private PhoneNr(string value)
    {
        Value = value;
    }

    public static PhoneNr Create(string value)
    {
        var validator = new PhoneNrValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid phone number: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new PhoneNr(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(PhoneNr phoneNr) => phoneNr.Value;
}

/// <summary>
/// Validator for phone numbers
/// </summary>
public class PhoneNrValidator : AbstractValidator<string>
{
    public PhoneNrValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Phone number cannot be empty")
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters")
            .Matches(@"^[\d\-\(\)\+\s]+$").WithMessage("Phone number can only contain digits, spaces, hyphens, parentheses, and plus signs");
    }
}
