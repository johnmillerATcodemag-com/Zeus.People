using FluentAssertions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Tests.ValueObjects;

public class RankTests
{
    [Theory]
    [InlineData(Rank.Professor)]
    [InlineData(Rank.SeniorLecturer)]
    [InlineData(Rank.Lecturer)]
    public void Create_WithValidRank_ShouldSucceed(string validRank)
    {
        // Act
        var rank = Rank.Create(validRank);

        // Assert
        rank.Value.Should().Be(validRank);
        rank.ToString().Should().Be(validRank);
        ((string)rank).Should().Be(validRank);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("INVALID")]
    [InlineData("Professor")]
    public void Create_WithInvalidRank_ShouldThrowArgumentException(string invalidRank)
    {
        // Act & Assert
        var act = () => Rank.Create(invalidRank);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Invalid rank:*");
    }

    [Fact]
    public void Create_WithNullRank_ShouldThrowArgumentException()
    {
        // Act & Assert
        var act = () => Rank.Create(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void CreateProfessor_ShouldCreateProfessorRank()
    {
        // Act
        var rank = Rank.CreateProfessor();

        // Assert
        rank.Value.Should().Be(Rank.Professor);
        rank.IsProfessor.Should().BeTrue();
        rank.IsSeniorLecturer.Should().BeFalse();
        rank.IsLecturer.Should().BeFalse();
    }

    [Fact]
    public void CreateSeniorLecturer_ShouldCreateSeniorLecturerRank()
    {
        // Act
        var rank = Rank.CreateSeniorLecturer();

        // Assert
        rank.Value.Should().Be(Rank.SeniorLecturer);
        rank.IsProfessor.Should().BeFalse();
        rank.IsSeniorLecturer.Should().BeTrue();
        rank.IsLecturer.Should().BeFalse();
    }

    [Fact]
    public void CreateLecturer_ShouldCreateLecturerRank()
    {
        // Act
        var rank = Rank.CreateLecturer();

        // Assert
        rank.Value.Should().Be(Rank.Lecturer);
        rank.IsProfessor.Should().BeFalse();
        rank.IsSeniorLecturer.Should().BeFalse();
        rank.IsLecturer.Should().BeTrue();
    }

    [Fact]
    public void GetEnsuredAccessLevel_ForProfessor_ShouldReturnNational()
    {
        // Arrange
        var rank = Rank.CreateProfessor();

        // Act
        var accessLevel = rank.GetEnsuredAccessLevel();

        // Assert
        accessLevel.IsNational.Should().BeTrue();
    }

    [Fact]
    public void GetEnsuredAccessLevel_ForSeniorLecturer_ShouldReturnInternational()
    {
        // Arrange
        var rank = Rank.CreateSeniorLecturer();

        // Act
        var accessLevel = rank.GetEnsuredAccessLevel();

        // Assert
        accessLevel.IsInternational.Should().BeTrue();
    }

    [Fact]
    public void GetEnsuredAccessLevel_ForLecturer_ShouldReturnLocal()
    {
        // Arrange
        var rank = Rank.CreateLecturer();

        // Act
        var accessLevel = rank.GetEnsuredAccessLevel();

        // Assert
        accessLevel.IsLocal.Should().BeTrue();
    }

    [Fact]
    public void EqualityComparison_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var rank1 = Rank.CreateProfessor();
        var rank2 = Rank.CreateProfessor();

        // Act & Assert
        rank1.Should().Be(rank2);
        rank1.GetHashCode().Should().Be(rank2.GetHashCode());
        (rank1 == rank2).Should().BeTrue();
        (rank1 != rank2).Should().BeFalse();
    }

    [Fact]
    public void EqualityComparison_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var rank1 = Rank.CreateProfessor();
        var rank2 = Rank.CreateLecturer();

        // Act & Assert
        rank1.Should().NotBe(rank2);
        (rank1 == rank2).Should().BeFalse();
        (rank1 != rank2).Should().BeTrue();
    }
}
