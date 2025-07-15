using FluentValidation;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Money Amount value object - must be a positive value in US Dollars
/// </summary>
public class MoneyAmt : ValueObject
{
    public decimal Value { get; private set; }

    private MoneyAmt(decimal value)
    {
        Value = value;
    }

    public static MoneyAmt Create(decimal value)
    {
        var validator = new MoneyAmtValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid money amount: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new MoneyAmt(value);
    }

    public static MoneyAmt Zero => new(0);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => $"${Value:F2}";

    public static implicit operator decimal(MoneyAmt moneyAmt) => moneyAmt.Value;

    public static MoneyAmt operator +(MoneyAmt left, MoneyAmt right) => new(left.Value + right.Value);
    public static MoneyAmt operator -(MoneyAmt left, MoneyAmt right) => new(left.Value - right.Value);
    public static MoneyAmt operator *(MoneyAmt left, decimal multiplier) => new(left.Value * multiplier);
    public static MoneyAmt operator /(MoneyAmt left, decimal divisor) => new(left.Value / divisor);

    public static bool operator >(MoneyAmt left, MoneyAmt right) => left.Value > right.Value;
    public static bool operator <(MoneyAmt left, MoneyAmt right) => left.Value < right.Value;
    public static bool operator >=(MoneyAmt left, MoneyAmt right) => left.Value >= right.Value;
    public static bool operator <=(MoneyAmt left, MoneyAmt right) => left.Value <= right.Value;
}

/// <summary>
/// Validator for money amounts
/// </summary>
public class MoneyAmtValidator : AbstractValidator<decimal>
{
    public MoneyAmtValidator()
    {
        RuleFor(x => x)
            .GreaterThanOrEqualTo(0).WithMessage("Money amount must be a positive value");
    }
}
