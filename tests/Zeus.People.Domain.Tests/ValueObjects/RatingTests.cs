using FluentAssertions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Tests.ValueObjects;

public class RatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(7)]
    public void Create_WithValidRating_ShouldSucceed(int validRating)
    {
        // Act
        var rating = Rating.Create(validRating);

        // Assert
        rating.Value.Should().Be(validRating);
        ((int)rating).Should().Be(validRating);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(8)]
    [InlineData(-1)]
    [InlineData(10)]
    public void Create_WithInvalidRating_ShouldThrowArgumentException(int invalidRating)
    {
        // Act & Assert
        var act = () => Rating.Create(invalidRating);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Invalid rating:*");
    }

    [Fact]
    public void CreateMinimum_ShouldReturnRatingOfOne()
    {
        // Act
        var rating = Rating.CreateMinimum();

        // Assert
        rating.Value.Should().Be(1);
    }

    [Fact]
    public void CreateMaximum_ShouldReturnRatingOfSeven()
    {
        // Act
        var rating = Rating.CreateMaximum();

        // Assert
        rating.Value.Should().Be(7);
    }

    [Fact]
    public void ToString_ShouldReturnStringValue()
    {
        // Arrange
        var rating = Rating.Create(5);

        // Act
        var result = rating.ToString();

        // Assert
        result.Should().Be("5");
    }

    [Fact]
    public void ComparisonOperators_ShouldWorkCorrectly()
    {
        // Arrange
        var rating1 = Rating.Create(5);
        var rating2 = Rating.Create(3);
        var rating3 = Rating.Create(5);

        // Act & Assert
        (rating1 > rating2).Should().BeTrue();
        (rating2 < rating1).Should().BeTrue();
        (rating1 >= rating3).Should().BeTrue();
        (rating1 <= rating3).Should().BeTrue();
        (rating1 > rating3).Should().BeFalse();
        (rating1 < rating3).Should().BeFalse();
    }

    [Fact]
    public void EqualityComparison_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var rating1 = Rating.Create(5);
        var rating2 = Rating.Create(5);

        // Act & Assert
        rating1.Should().Be(rating2);
        rating1.GetHashCode().Should().Be(rating2.GetHashCode());
        (rating1 == rating2).Should().BeTrue();
        (rating1 != rating2).Should().BeFalse();
    }

    [Fact]
    public void EqualityComparison_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var rating1 = Rating.Create(5);
        var rating2 = Rating.Create(3);

        // Act & Assert
        rating1.Should().NotBe(rating2);
        (rating1 == rating2).Should().BeFalse();
        (rating1 != rating2).Should().BeTrue();
    }
}
