using FluentAssertions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Tests.ValueObjects;

public class EmpNameTests
{
    [Fact]
    public void Create_WithValidName_ShouldSucceed()
    {
        // Arrange
        var validName = "Smith J.";

        // Act
        var empName = EmpName.Create(validName);

        // Assert
        empName.Value.Should().Be(validName);
        empName.ToString().Should().Be(validName);
        ((string)empName).Should().Be(validName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_WithInvalidName_ShouldThrowArgumentException(string invalidName)
    {
        // Act & Assert
        var act = () => EmpName.Create(invalidName);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Invalid employee name:*");
    }

    [Fact]
    public void Create_WithNullName_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => EmpName.Create(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_WithTooLongName_ShouldThrowArgumentException()
    {
        // Arrange
        var tooLongName = new string('A', 101);

        // Act & Assert
        var act = () => EmpName.Create(tooLongName);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Invalid employee name:*");
    }

    [Theory]
    [InlineData("Smith123")]
    [InlineData("Smith@")]
    [InlineData("Smith#")]
    public void Create_WithInvalidCharacters_ShouldThrowArgumentException(string invalidName)
    {
        // Act & Assert
        var act = () => EmpName.Create(invalidName);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Invalid employee name:*");
    }

    [Fact]
    public void EqualityComparison_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var name1 = EmpName.Create("Smith J.");
        var name2 = EmpName.Create("Smith J.");

        // Act & Assert
        name1.Should().Be(name2);
        name1.GetHashCode().Should().Be(name2.GetHashCode());
        (name1 == name2).Should().BeTrue();
        (name1 != name2).Should().BeFalse();
    }

    [Fact]
    public void EqualityComparison_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var name1 = EmpName.Create("Smith J.");
        var name2 = EmpName.Create("Jones A.");

        // Act & Assert
        name1.Should().NotBe(name2);
        (name1 == name2).Should().BeFalse();
        (name1 != name2).Should().BeTrue();
    }
}
