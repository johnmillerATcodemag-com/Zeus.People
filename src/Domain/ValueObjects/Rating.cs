using FluentValidation;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Rating value object - must be between 1 and 7
/// </summary>
public class Rating : ValueObject
{
    public int Value { get; private set; }

    private Rating(int value)
    {
        Value = value;
    }

    public static Rating Create(int value)
    {
        var validator = new RatingValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid rating: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new Rating(value);
    }

    public static Rating CreateMinimum() => new(1);
    public static Rating CreateMaximum() => new(7);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator int(Rating rating) => rating.Value;

    public static bool operator >(Rating left, Rating right) => left.Value > right.Value;
    public static bool operator <(Rating left, Rating right) => left.Value < right.Value;
    public static bool operator >=(Rating left, Rating right) => left.Value >= right.Value;
    public static bool operator <=(Rating left, Rating right) => left.Value <= right.Value;
}

/// <summary>
/// Validator for ratings
/// </summary>
public class RatingValidator : AbstractValidator<int>
{
    public RatingValidator()
    {
        RuleFor(x => x)
            .InclusiveBetween(1, 7).WithMessage("Rating must be between 1 and 7");
    }
}
