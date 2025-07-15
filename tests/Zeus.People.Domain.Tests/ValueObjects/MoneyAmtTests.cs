using FluentAssertions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Tests.ValueObjects;

public class MoneyAmtTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100.50)]
    [InlineData(1000000)]
    [InlineData(0.01)]
    public void Create_WithValidAmount_ShouldSucceed(decimal validAmount)
    {
        // Act
        var moneyAmt = MoneyAmt.Create(validAmount);

        // Assert
        moneyAmt.Value.Should().Be(validAmount);
        ((decimal)moneyAmt).Should().Be(validAmount);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100.50)]
    public void Create_WithNegativeAmount_ShouldThrowArgumentException(decimal negativeAmount)
    {
        // Act & Assert
        var act = () => MoneyAmt.Create(negativeAmount);
        act.Should().Throw<ArgumentException>()
           .WithMessage("Invalid money amount:*");
    }

    [Fact]
    public void Zero_ShouldReturnZeroAmount()
    {
        // Act
        var zero = MoneyAmt.Zero;

        // Assert
        zero.Value.Should().Be(0);
    }

    [Fact]
    public void ToString_ShouldFormatAsCurrency()
    {
        // Arrange
        var moneyAmt = MoneyAmt.Create(123.45m);

        // Act
        var result = moneyAmt.ToString();

        // Assert
        result.Should().Be("$123.45");
    }

    [Fact]
    public void Addition_ShouldWorkCorrectly()
    {
        // Arrange
        var amount1 = MoneyAmt.Create(100);
        var amount2 = MoneyAmt.Create(50);

        // Act
        var result = amount1 + amount2;

        // Assert
        result.Value.Should().Be(150);
    }

    [Fact]
    public void Subtraction_ShouldWorkCorrectly()
    {
        // Arrange
        var amount1 = MoneyAmt.Create(100);
        var amount2 = MoneyAmt.Create(30);

        // Act
        var result = amount1 - amount2;

        // Assert
        result.Value.Should().Be(70);
    }

    [Fact]
    public void Multiplication_ShouldWorkCorrectly()
    {
        // Arrange
        var amount = MoneyAmt.Create(100);

        // Act
        var result = amount * 2.5m;

        // Assert
        result.Value.Should().Be(250);
    }

    [Fact]
    public void Division_ShouldWorkCorrectly()
    {
        // Arrange
        var amount = MoneyAmt.Create(100);

        // Act
        var result = amount / 4;

        // Assert
        result.Value.Should().Be(25);
    }

    [Fact]
    public void ComparisonOperators_ShouldWorkCorrectly()
    {
        // Arrange
        var amount1 = MoneyAmt.Create(100);
        var amount2 = MoneyAmt.Create(50);
        var amount3 = MoneyAmt.Create(100);

        // Act & Assert
        (amount1 > amount2).Should().BeTrue();
        (amount2 < amount1).Should().BeTrue();
        (amount1 >= amount3).Should().BeTrue();
        (amount1 <= amount3).Should().BeTrue();
        (amount1 > amount3).Should().BeFalse();
        (amount1 < amount3).Should().BeFalse();
    }

    [Fact]
    public void EqualityComparison_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var amount1 = MoneyAmt.Create(100.50m);
        var amount2 = MoneyAmt.Create(100.50m);

        // Act & Assert
        amount1.Should().Be(amount2);
        amount1.GetHashCode().Should().Be(amount2.GetHashCode());
        (amount1 == amount2).Should().BeTrue();
        (amount1 != amount2).Should().BeFalse();
    }

    [Fact]
    public void EqualityComparison_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var amount1 = MoneyAmt.Create(100);
        var amount2 = MoneyAmt.Create(200);

        // Act & Assert
        amount1.Should().NotBe(amount2);
        (amount1 == amount2).Should().BeFalse();
        (amount1 != amount2).Should().BeTrue();
    }
}
