using FluentAssertions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Tests.ValueObjects;

public class AccessLevelTests
{
    [Theory]
    [InlineData(AccessLevel.Local)]
    [InlineData(AccessLevel.International)]
    [InlineData(AccessLevel.National)]
    public void Create_WithValidAccessLevel_ShouldSucceed(string validAccessLevel)
    {
        // Act
        var accessLevel = AccessLevel.Create(validAccessLevel);

        // Assert
        accessLevel.Value.Should().Be(validAccessLevel);
        accessLevel.ToString().Should().Be(validAccessLevel);
        ((string)accessLevel).Should().Be(validAccessLevel);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("INVALID")]
    [InlineData("LOCAL")]
    public void Create_WithInvalidAccessLevel_ShouldThrowArgumentException(string invalidAccessLevel)
    {
        // Act & Assert
        var act = () => AccessLevel.Create(invalidAccessLevel);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Invalid access level:*");
    }

    [Fact]
    public void Create_WithNullAccessLevel_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => AccessLevel.Create(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateLocal_ShouldCreateLocalAccessLevel()
    {
        // Act
        var accessLevel = AccessLevel.CreateLocal();

        // Assert
        accessLevel.Value.Should().Be(AccessLevel.Local);
        accessLevel.IsLocal.Should().BeTrue();
        accessLevel.IsInternational.Should().BeFalse();
        accessLevel.IsNational.Should().BeFalse();
    }

    [Fact]
    public void CreateInternational_ShouldCreateInternationalAccessLevel()
    {
        // Act
        var accessLevel = AccessLevel.CreateInternational();

        // Assert
        accessLevel.Value.Should().Be(AccessLevel.International);
        accessLevel.IsLocal.Should().BeFalse();
        accessLevel.IsInternational.Should().BeTrue();
        accessLevel.IsNational.Should().BeFalse();
    }

    [Fact]
    public void CreateNational_ShouldCreateNationalAccessLevel()
    {
        // Act
        var accessLevel = AccessLevel.CreateNational();

        // Assert
        accessLevel.Value.Should().Be(AccessLevel.National);
        accessLevel.IsLocal.Should().BeFalse();
        accessLevel.IsInternational.Should().BeFalse();
        accessLevel.IsNational.Should().BeTrue();
    }

    [Fact]
    public void EqualityComparison_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var accessLevel1 = AccessLevel.CreateLocal();
        var accessLevel2 = AccessLevel.CreateLocal();

        // Act & Assert
        accessLevel1.Should().Be(accessLevel2);
        accessLevel1.GetHashCode().Should().Be(accessLevel2.GetHashCode());
        (accessLevel1 == accessLevel2).Should().BeTrue();
        (accessLevel1 != accessLevel2).Should().BeFalse();
    }

    [Fact]
    public void EqualityComparison_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var accessLevel1 = AccessLevel.CreateLocal();
        var accessLevel2 = AccessLevel.CreateNational();

        // Act & Assert
        accessLevel1.Should().NotBe(accessLevel2);
        (accessLevel1 == accessLevel2).Should().BeFalse();
        (accessLevel1 != accessLevel2).Should().BeTrue();
    }
}
