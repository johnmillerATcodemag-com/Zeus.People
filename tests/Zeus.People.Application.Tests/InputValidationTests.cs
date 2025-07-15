using FluentValidation.TestHelper;
using Zeus.People.Application.Commands.Academic;
using Zeus.People.Application.Commands.Department;
using Zeus.People.Application.Validators.Academic;
using Zeus.People.Application.Validators.Department;
using Zeus.People.Application.Behaviors;
using MediatR;
using Moq;
using Microsoft.Extensions.Logging;
using Zeus.People.Application.Handlers.Academic;
using Zeus.People.Application.Interfaces;
using Zeus.People.Application.Common;
using FluentValidation;

namespace Zeus.People.Application.Tests;

/// <summary>
/// Tests to validate that input validation works properly using FluentValidation
/// </summary>
public class InputValidationTests
{
    [Fact]
    public void CreateAcademicCommandValidator_ShouldRequireEmpNr()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();
        var command = new CreateAcademicCommand("", "John Doe", "P");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmpNr)
              .WithErrorMessage("Employee number is required");
    }

    [Fact]
    public void CreateAcademicCommandValidator_ShouldRequireEmpNrLength()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();
        var command = new CreateAcademicCommand("12345678901", "John Doe", "P"); // Too long

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmpNr)
              .WithErrorMessage("Employee number must be between 1 and 10 characters");
    }

    [Fact]
    public void CreateAcademicCommandValidator_ShouldRequireNumericEmpNr()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();
        var command = new CreateAcademicCommand("ABC123", "John Doe", "P"); // Contains letters

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmpNr)
              .WithErrorMessage("Employee number must contain only digits");
    }

    [Fact]
    public void CreateAcademicCommandValidator_ShouldRequireEmpName()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();
        var command = new CreateAcademicCommand("123456", "", "P");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmpName)
              .WithErrorMessage("Employee name is required");
    }

    [Fact]
    public void CreateAcademicCommandValidator_ShouldValidateEmpNameLength()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();
        var longName = new string('A', 101); // Too long
        var command = new CreateAcademicCommand("123456", longName, "P");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmpName)
              .WithErrorMessage("Employee name must be between 1 and 100 characters");
    }

    [Fact]
    public void CreateAcademicCommandValidator_ShouldRequireValidRank()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();
        var command = new CreateAcademicCommand("123456", "John Doe", "INVALID");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Rank)
              .WithErrorMessage("Rank must be 'P' (Professor), 'SL' (Senior Lecturer), or 'L' (Lecturer)");
    }

    [Fact]
    public void CreateAcademicCommandValidator_ShouldAcceptValidRanks()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();

        // Act & Assert
        var professorCommand = new CreateAcademicCommand("123456", "John Doe", "P");
        var professorResult = validator.TestValidate(professorCommand);
        professorResult.ShouldNotHaveValidationErrorFor(x => x.Rank);

        var seniorLecturerCommand = new CreateAcademicCommand("123456", "Jane Smith", "SL");
        var seniorLecturerResult = validator.TestValidate(seniorLecturerCommand);
        seniorLecturerResult.ShouldNotHaveValidationErrorFor(x => x.Rank);

        var lecturerCommand = new CreateAcademicCommand("123456", "Bob Johnson", "L");
        var lecturerResult = validator.TestValidate(lecturerCommand);
        lecturerResult.ShouldNotHaveValidationErrorFor(x => x.Rank);
    }

    [Fact]
    public void CreateAcademicCommandValidator_ShouldPassWithValidInput()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();
        var command = new CreateAcademicCommand("123456", "Dr. John Doe", "P");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void UpdateAcademicCommandValidator_ShouldRequireId()
    {
        // Arrange
        var validator = new UpdateAcademicCommandValidator();
        var command = new UpdateAcademicCommand(Guid.Empty, "John Doe", "P", null);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Academic ID is required");
    }

    [Fact]
    public void CreateDepartmentCommandValidator_ShouldRequireName()
    {
        // Arrange
        var validator = new CreateDepartmentCommandValidator();
        var command = new CreateDepartmentCommand("");

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Department name is required");
    }

    [Fact]
    public void CreateDepartmentCommandValidator_ShouldValidateNameLength()
    {
        // Arrange
        var validator = new CreateDepartmentCommandValidator();
        var longName = new string('A', 101); // Too long
        var command = new CreateDepartmentCommand(longName);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
              .WithErrorMessage("Department name must be between 1 and 100 characters");
    }

    [Fact]
    public void UpdateDepartmentCommandValidator_ShouldValidateBudgets()
    {
        // Arrange
        var validator = new UpdateDepartmentCommandValidator();
        var command = new UpdateDepartmentCommand(
            Guid.NewGuid(),
            "Computer Science",
            -1000, // Invalid negative budget
            -500, // Invalid negative budget
            null);

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ResearchBudget)
              .WithErrorMessage("Research budget must be positive");
        result.ShouldHaveValidationErrorFor(x => x.TeachingBudget)
              .WithErrorMessage("Teaching budget must be positive");
    }

    [Fact]
    public void UpdateDepartmentCommandValidator_ShouldValidatePhoneNumber()
    {
        // Arrange
        var validator = new UpdateDepartmentCommandValidator();
        var command = new UpdateDepartmentCommand(
            Guid.NewGuid(),
            "Computer Science",
            null,
            null,
            "12345"); // Too short phone number

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.HeadHomePhone)
              .WithErrorMessage("Head home phone must be between 6 and 15 digits");
    }

    [Fact]
    public void UpdateDepartmentCommandValidator_ShouldAllowNullOptionalFields()
    {
        // Arrange
        var validator = new UpdateDepartmentCommandValidator();
        var command = new UpdateDepartmentCommand(
            Guid.NewGuid(),
            "Computer Science",
            null, // Null budget should be allowed
            null, // Null budget should be allowed
            null); // Null phone should be allowed

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.ResearchBudget);
        result.ShouldNotHaveValidationErrorFor(x => x.TeachingBudget);
        result.ShouldNotHaveValidationErrorFor(x => x.HeadHomePhone);
    }

    [Fact]
    public async Task ValidationBehavior_ShouldReturnFailureResultForInvalidCommand()
    {
        // Arrange
        var validators = new List<IValidator<CreateAcademicCommand>>
        {
            new CreateAcademicCommandValidator()
        };
        var validationBehavior = new ValidationBehavior<CreateAcademicCommand, Result<Guid>>(validators);

        var invalidCommand = new CreateAcademicCommand("", "", "INVALID"); // All invalid

        var mockNext = new Mock<RequestHandlerDelegate<Result<Guid>>>();
        mockNext.Setup(x => x()).ReturnsAsync(Result.Success(Guid.NewGuid()));

        // Act
        var result = await validationBehavior.Handle(invalidCommand, mockNext.Object, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Failed", result.Error.Code);
        Assert.Contains("Employee number is required", result.Error.Message);
        Assert.Contains("Employee name is required", result.Error.Message);
        Assert.Contains("Rank must be", result.Error.Message);

        // Verify that the next handler was never called
        mockNext.Verify(x => x(), Times.Never);
    }

    [Fact]
    public async Task ValidationBehavior_ShouldCallNextHandlerForValidCommand()
    {
        // Arrange
        var validators = new List<IValidator<CreateAcademicCommand>>
        {
            new CreateAcademicCommandValidator()
        };
        var validationBehavior = new ValidationBehavior<CreateAcademicCommand, Result<Guid>>(validators);

        var validCommand = new CreateAcademicCommand("123456", "Dr. John Doe", "P"); // All valid

        var expectedResult = Result.Success(Guid.NewGuid());
        var mockNext = new Mock<RequestHandlerDelegate<Result<Guid>>>();
        mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        var result = await validationBehavior.Handle(validCommand, mockNext.Object, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult.Value, result.Value);

        // Verify that the next handler was called exactly once
        mockNext.Verify(x => x(), Times.Once);
    }

    [Fact]
    public async Task ValidationBehavior_ShouldHandleMultipleValidationErrors()
    {
        // Arrange
        var validators = new List<IValidator<CreateDepartmentCommand>>
        {
            new CreateDepartmentCommandValidator()
        };
        var validationBehavior = new ValidationBehavior<CreateDepartmentCommand, Result<Guid>>(validators);

        var invalidCommand = new CreateDepartmentCommand(""); // Empty name

        var mockNext = new Mock<RequestHandlerDelegate<Result<Guid>>>();

        // Act
        var result = await validationBehavior.Handle(invalidCommand, mockNext.Object, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Failed", result.Error.Code);
        Assert.Contains("Department name is required", result.Error.Message);
    }

    [Fact]
    public async Task ValidationBehavior_ShouldPassThroughWhenNoValidators()
    {
        // Arrange
        var validators = new List<IValidator<CreateAcademicCommand>>(); // No validators
        var validationBehavior = new ValidationBehavior<CreateAcademicCommand, Result<Guid>>(validators);

        var command = new CreateAcademicCommand("", "", ""); // Invalid but no validators

        var expectedResult = Result.Success(Guid.NewGuid());
        var mockNext = new Mock<RequestHandlerDelegate<Result<Guid>>>();
        mockNext.Setup(x => x()).ReturnsAsync(expectedResult);

        // Act
        var result = await validationBehavior.Handle(command, mockNext.Object, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedResult.Value, result.Value);

        // Verify that the next handler was called
        mockNext.Verify(x => x(), Times.Once);
    }

    [Fact]
    public void Validator_ShouldHandleComplexValidationScenarios()
    {
        // Arrange
        var validator = new UpdateDepartmentCommandValidator();
        var command = new UpdateDepartmentCommand(
            Guid.NewGuid(),
            "CS", // Valid short name
            50000, // Valid positive budget
            25000, // Valid positive budget
            "1234567890"); // Valid phone number

        // Act
        var result = validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validator_ShouldValidateConditionalRules()
    {
        // Arrange
        var validator = new UpdateDepartmentCommandValidator();

        // Test with valid optional phone number
        var commandWithValidPhone = new UpdateDepartmentCommand(
            Guid.NewGuid(),
            "Computer Science",
            null,
            null,
            "123456789012345"); // Valid 15-digit phone

        // Act
        var resultWithValidPhone = validator.TestValidate(commandWithValidPhone);

        // Assert
        resultWithValidPhone.ShouldNotHaveValidationErrorFor(x => x.HeadHomePhone);

        // Test with invalid phone number (too long)
        var commandWithInvalidPhone = new UpdateDepartmentCommand(
            Guid.NewGuid(),
            "Computer Science",
            null,
            null,
            "1234567890123456"); // Too long (16 digits)

        var resultWithInvalidPhone = validator.TestValidate(commandWithInvalidPhone);
        resultWithInvalidPhone.ShouldHaveValidationErrorFor(x => x.HeadHomePhone);
    }
}
