using Zeus.People.Application.Commands.Academic;
using Zeus.People.Application.Validators.Academic;
using Zeus.People.Application.DTOs;

namespace Zeus.People.Application.Tests;

/// <summary>
/// Basic tests for application layer components
/// </summary>
public class ApplicationLayerTests
{
    [Fact]
    public void CreateAcademicCommand_ShouldHaveRequiredProperties()
    {
        // Arrange
        var empNr = "123";
        var empName = "John Doe";
        var rank = "P";

        // Act
        var command = new CreateAcademicCommand(empNr, empName, rank);

        // Assert
        Assert.Equal(empNr, command.EmpNr);
        Assert.Equal(empName, command.EmpName);
        Assert.Equal(rank, command.Rank);
    }

    [Fact]
    public void CreateAcademicCommandValidator_ShouldValidateRequiredFields()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();
        var command = new CreateAcademicCommand("", "", "");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAcademicCommand.EmpNr));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAcademicCommand.EmpName));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAcademicCommand.Rank));
    }

    [Fact]
    public void CreateAcademicCommandValidator_ShouldAcceptValidInput()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();
        var command = new CreateAcademicCommand("123", "John Doe", "P");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AcademicDto_ShouldInheritFromBaseDto()
    {
        // Arrange & Act
        var dto = new AcademicDto();

        // Assert
        Assert.IsAssignableFrom<BaseDto>(dto);
        Assert.NotEqual(Guid.Empty, dto.Id);
    }

    [Fact]
    public void CreateAcademicCommandValidator_ShouldRejectInvalidRank()
    {
        // Arrange
        var validator = new CreateAcademicCommandValidator();
        var command = new CreateAcademicCommand("123", "John Doe", "INVALID");

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(CreateAcademicCommand.Rank));
    }
}
