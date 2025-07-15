using FluentValidation;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Room Number value object
/// </summary>
public class RoomNr : ValueObject
{
    public string Value { get; private set; }

    private RoomNr(string value)
    {
        Value = value;
    }

    public static RoomNr Create(string value)
    {
        var validator = new RoomNrValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid room number: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new RoomNr(value);
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(RoomNr roomNr) => roomNr.Value;
}

/// <summary>
/// Validator for room numbers
/// </summary>
public class RoomNrValidator : AbstractValidator<string>
{
    public RoomNrValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Room number cannot be empty")
            .MaximumLength(10).WithMessage("Room number cannot exceed 10 characters")
            .Matches(@"^[A-Za-z0-9\-\.]+$").WithMessage("Room number can only contain letters, numbers, hyphens, and periods");
    }
}
