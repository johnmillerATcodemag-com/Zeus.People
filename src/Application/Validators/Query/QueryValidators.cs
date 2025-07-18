using FluentValidation;
using Zeus.People.Application.Queries.Academic;
using Zeus.People.Application.Queries.Department;
using Zeus.People.Application.Queries.Room;

namespace Zeus.People.Application.Validators.Query;

/// <summary>
/// Validator for GetAllAcademicsQuery
/// </summary>
public class GetAllAcademicsQueryValidator : AbstractValidator<GetAllAcademicsQuery>
{
    public GetAllAcademicsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");

        RuleFor(x => x.RankFilter)
            .Must(BeValidRank).When(x => !string.IsNullOrEmpty(x.RankFilter))
            .WithMessage("Rank filter must be 'P' (Professor), 'SL' (Senior Lecturer), or 'L' (Lecturer)");
    }

    private static bool BeValidRank(string? rank)
    {
        return rank is "P" or "SL" or "L";
    }
}

/// <summary>
/// Validator for GetAcademicsByDepartmentQuery
/// </summary>
public class GetAcademicsByDepartmentQueryValidator : AbstractValidator<GetAcademicsByDepartmentQuery>
{
    public GetAcademicsByDepartmentQueryValidator()
    {
        RuleFor(x => x.DepartmentId)
            .NotEmpty().WithMessage("Department ID is required");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
    }
}

/// <summary>
/// Validator for GetAcademicsByRankQuery
/// </summary>
public class GetAcademicsByRankQueryValidator : AbstractValidator<GetAcademicsByRankQuery>
{
    public GetAcademicsByRankQueryValidator()
    {
        RuleFor(x => x.Rank)
            .NotEmpty().WithMessage("Rank is required")
            .Must(BeValidRank).WithMessage("Rank must be 'P' (Professor), 'SL' (Senior Lecturer), or 'L' (Lecturer)");

        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
    }

    private static bool BeValidRank(string rank)
    {
        return rank is "P" or "SL" or "L";
    }
}

/// <summary>
/// Validator for GetTenuredAcademicsQuery
/// </summary>
public class GetTenuredAcademicsQueryValidator : AbstractValidator<GetTenuredAcademicsQuery>
{
    public GetTenuredAcademicsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
    }
}

/// <summary>
/// Validator for GetAcademicsWithExpiringContractsQuery
/// </summary>
public class GetAcademicsWithExpiringContractsQueryValidator : AbstractValidator<GetAcademicsWithExpiringContractsQuery>
{
    public GetAcademicsWithExpiringContractsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");

        RuleFor(x => x.BeforeDate)
            .GreaterThan(DateTime.Now).When(x => x.BeforeDate.HasValue)
            .WithMessage("Before date must be in the future");
    }
}

/// <summary>
/// Validator for GetAllDepartmentsQuery
/// </summary>
public class GetAllDepartmentsQueryValidator : AbstractValidator<GetAllDepartmentsQuery>
{
    public GetAllDepartmentsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
    }
}

/// <summary>
/// Validator for GetAllRoomsQuery
/// </summary>
public class GetAllRoomsQueryValidator : AbstractValidator<GetAllRoomsQuery>
{
    public GetAllRoomsQueryValidator()
    {
        RuleFor(x => x.PageNumber)
            .GreaterThan(0).WithMessage("Page number must be greater than 0");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("Page size must be greater than 0")
            .LessThanOrEqualTo(100).WithMessage("Page size cannot exceed 100");
    }
}
