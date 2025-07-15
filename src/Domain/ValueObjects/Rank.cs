using FluentValidation;

namespace Zeus.People.Domain.ValueObjects;

/// <summary>
/// Academic Rank value object - 'P' (Professor), 'SL' (Senior Lecturer), 'L' (Lecturer)
/// </summary>
public class Rank : ValueObject
{
    public const string Professor = "P";
    public const string SeniorLecturer = "SL";
    public const string Lecturer = "L";

    public string Value { get; private set; }

    private Rank(string value)
    {
        Value = value;
    }

    public static Rank Create(string value)
    {
        var validator = new RankValidator();
        var validationResult = validator.Validate(value);

        if (!validationResult.IsValid)
        {
            throw new ArgumentException($"Invalid rank: {string.Join(", ", validationResult.Errors.Select(e => e.ErrorMessage))}");
        }

        return new Rank(value);
    }

    public static Rank CreateProfessor() => new(Professor);
    public static Rank CreateSeniorLecturer() => new(SeniorLecturer);
    public static Rank CreateLecturer() => new(Lecturer);

    public bool IsProfessor => Value == Professor;
    public bool IsSeniorLecturer => Value == SeniorLecturer;
    public bool IsLecturer => Value == Lecturer;

    /// <summary>
    /// Gets the access level ensured by this rank
    /// </summary>
    public AccessLevel GetEnsuredAccessLevel()
    {
        return Value switch
        {
            Professor => AccessLevel.CreateNational(),
            SeniorLecturer => AccessLevel.CreateInternational(),
            Lecturer => AccessLevel.CreateLocal(),
            _ => throw new InvalidOperationException($"Unknown rank: {Value}")
        };
    }

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Rank rank) => rank.Value;
}

/// <summary>
/// Validator for academic ranks
/// </summary>
public class RankValidator : AbstractValidator<string>
{
    public RankValidator()
    {
        RuleFor(x => x)
            .NotEmpty().WithMessage("Rank cannot be empty")
            .Must(x => x == Rank.Professor || x == Rank.SeniorLecturer || x == Rank.Lecturer)
            .WithMessage("Rank must be 'P' (Professor), 'SL' (Senior Lecturer), or 'L' (Lecturer)");
    }
}
