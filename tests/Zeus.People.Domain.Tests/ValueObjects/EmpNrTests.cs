using FluentAssertions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Tests.ValueObjects;

public class EmpNrTests
{
    [Fact]
    public void Create_WithValidEmployeeNumber_ShouldSucceed()
    {
        // Arrange
        var validEmpNr = "AB1234";

        // Act
        var result = EmpNr.Create(validEmpNr);

        // Assert
        result.Value.Should().Be(validEmpNr);
        ((string)result).Should().Be(validEmpNr);
    }

    [Theory]
    [InlineData("")]
    [InlineData("AB123")]
    [InlineData("AB12345")]
    [InlineData("ab1234")]
    [InlineData("A1234")]
    [InlineData("ABC1234")]
    [InlineData("12ABCD")]
    public void Create_WithInvalidEmployeeNumber_ShouldThrowException(string invalidEmpNr)
    {
        // Act & Assert
        Action act = () => EmpNr.Create(invalidEmpNr);
        act.Should().Throw<ArgumentException>()
            .WithMessage("Invalid employee number:*");
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var empNr1 = EmpNr.Create("AB1234");
        var empNr2 = EmpNr.Create("AB1234");

        // Act & Assert
        empNr1.Should().Be(empNr2);
        (empNr1 == empNr2).Should().BeTrue();
        empNr1.GetHashCode().Should().Be(empNr2.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var empNr1 = EmpNr.Create("AB1234");
        var empNr2 = EmpNr.Create("CD5678");

        // Act & Assert
        empNr1.Should().NotBe(empNr2);
        (empNr1 != empNr2).Should().BeTrue();
    }
}
