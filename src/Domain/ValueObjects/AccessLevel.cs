using FluentValidation;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Access Level value object - 'LOC' (Local), 'INT' (International), 'NAT' (National)
/// </summary>
public class AccessLevel : ValueObject
{
    public const string Local = "LOC";
    public const string International = "INT";
    public const string National = "NAT";

    public string Value { get; private set; }

    private AccessLevel(string value)
    {
        Value = value;
    }

    public static AccessLevel Create(string value)
    {
        var validator = new AccessLevelValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid access level: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new AccessLevel(value);
    }

    public static AccessLevel CreateLocal() => new(Local);
    public static AccessLevel CreateInternational() => new(International);
    public static AccessLevel CreateNational() => new(National);

    public bool IsLocal => Value == Local;
    public bool IsInternational => Value == International;
    public bool IsNational => Value == National;

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(AccessLevel accessLevel) => accessLevel.Value;
}

/// <summary>
/// Validator for access levels
/// </summary>
public class AccessLevelValidator : AbstractValidator<string>
{
    public AccessLevelValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Access level cannot be empty")
            .Must(x => x == AccessLevel.Local || x == AccessLevel.International || x == AccessLevel.National)
            .WithMessage("Access level must be 'LOC' (Local), 'INT' (International), or 'NAT' (National)");
    }
}
