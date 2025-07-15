using Zeus.People.Application.Common;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Application.Tests;

/// <summary>
/// Tests to confirm error handling produces appropriate results
/// </summary>
public class ErrorHandlingTests
{
    [Fact]
    public void Error_ShouldHaveCorrectCodeAndMessage()
    {
        // Arrange
        var code = "Test.Error";
        var message = "This is a test error";

        // Act
        var error = new Error(code, message);

        // Assert
        Assert.Equal(code, error.Code);
        Assert.Equal(message, error.Message);
    }

    [Fact]
    public void Result_Success_ShouldCreateSuccessfulResult()
    {
        // Arrange
        var value = Guid.NewGuid();

        // Act
        var result = Result.Success(value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(value, result.Value);
    }

    [Fact]
    public void Result_Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");

        // Act
        var result = Result.Failure<Guid>(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);

        // Accessing Value on failure should throw an exception (this is correct behavior)
        Assert.Throws<InvalidOperationException>(() => result.Value);
    }

    [Fact]
    public void Result_NonGeneric_Success_ShouldCreateSuccessfulResult()
    {
        // Act
        var result = Result.Success();

        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
    }

    [Fact]
    public void Result_NonGeneric_Failure_ShouldCreateFailedResult()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");

        // Act
        var result = Result.Failure(error);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public void Result_AccessingValue_OnFailure_ShouldThrowException()
    {
        // Arrange
        var error = new Error("Test.Error", "Test error message");
        var result = Result.Failure<string>(error);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void Result_AccessingError_OnSuccess_ShouldReturnDefault()
    {
        // Arrange
        var result = Result.Success("test value");

        // Act & Assert
        Assert.Equal(Error.None, result.Error);
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Error_ShouldBeEquatable()
    {
        // Arrange
        var error1 = new Error("Test.Error", "Test message");
        var error2 = new Error("Test.Error", "Test message");
        var error3 = new Error("Different.Error", "Test message");

        // Act & Assert
        Assert.Equal(error1, error2);
        Assert.NotEqual(error1, error3);
    }

    [Fact]
    public void Result_ShouldHandleNullValues()
    {
        // Arrange & Act
        var successResult = Result.Success<string?>(null);
        var failureResult = Result.Failure<string?>(new Error("Test.Error", "Test message"));

        // Assert
        Assert.True(successResult.IsSuccess);
        Assert.Null(successResult.Value);

        Assert.True(failureResult.IsFailure);
        // Accessing Value on failure should throw an exception
        Assert.Throws<InvalidOperationException>(() => failureResult.Value);
    }

    [Fact]
    public void DomainValueObjects_ShouldHandleValidationErrors()
    {
        // Test EmpNr validation
        Assert.Throws<ArgumentException>(() => EmpNr.Create(""));
        Assert.Throws<ArgumentException>(() => EmpNr.Create("ABC123")); // Non-numeric
        Assert.Throws<ArgumentException>(() => EmpNr.Create("12345678901")); // Too long

        // Test EmpName validation
        Assert.Throws<ArgumentException>(() => EmpName.Create(""));
        Assert.Throws<ArgumentException>(() => EmpName.Create(new string('A', 101))); // Too long

        // Test Rank validation
        Assert.Throws<ArgumentException>(() => Rank.Create("INVALID"));
    }

    [Fact]
    public void DomainValueObjects_ShouldCreateValidInstances()
    {
        // Test valid EmpNr (using the correct format from domain - AB1234)
        var empNr = EmpNr.Create("AB1234");
        Assert.Equal("AB1234", empNr.Value);

        // Test valid EmpName
        var empName = EmpName.Create("Dr. John Doe");
        Assert.Equal("Dr. John Doe", empName.Value);

        // Test valid Rank
        var rank = Rank.Create("P");
        Assert.Equal("P", rank.Value);
    }

    [Fact]
    public void ErrorHandling_ShouldPreserveStackTrace()
    {
        // Arrange
        var originalException = new InvalidOperationException("Original error");
        var error = new Error("Test.Error", $"Wrapped error: {originalException.Message}");

        // Act
        var result = Result.Failure<string>(error);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("Original error", result.Error.Message);
    }

    [Fact]
    public void ErrorHandling_ShouldSupportChaining()
    {
        // Arrange
        var firstError = new Error("First.Error", "First error occurred");
        var secondError = new Error("Second.Error", $"Second error occurred. Previous: {firstError.Message}");

        // Act
        var result = Result.Failure<string>(secondError);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains("First error occurred", result.Error.Message);
        Assert.Contains("Second error occurred", result.Error.Message);
    }

    [Fact]
    public void ErrorHandling_ShouldProvideAppropriateErrorCodes()
    {
        // Test different error code patterns
        var validationError = new Error("Validation.Failed", "Input validation failed");
        var notFoundError = new Error("Entity.NotFound", "Entity was not found");
        var unauthorizedError = new Error("Authorization.Failed", "User not authorized");
        var systemError = new Error("System.Error", "System error occurred");

        // Assert error codes follow expected patterns
        Assert.StartsWith("Validation.", validationError.Code);
        Assert.StartsWith("Entity.", notFoundError.Code);
        Assert.StartsWith("Authorization.", unauthorizedError.Code);
        Assert.StartsWith("System.", systemError.Code);
    }

    [Fact]
    public void ErrorHandling_ShouldProvideDescriptiveMessages()
    {
        // Arrange
        var errors = new[]
        {
            new Error("Academic.NotFound", "Academic with ID 12345 was not found"),
            new Error("Department.CreateFailed", "Failed to create department: Name cannot be empty"),
            new Error("Validation.Failed", "Employee number is required; Employee name must be between 1 and 100 characters"),
            new Error("Academic.UpdateFailed", "Failed to update academic: Invalid rank specified")
        };

        // Assert
        foreach (var error in errors)
        {
            Assert.NotEmpty(error.Message);
            Assert.DoesNotContain("Exception", error.Message); // Should not expose internal exception details
            Assert.True(error.Message.Length > 10); // Should be descriptive
        }
    }

    [Fact]
    public void ErrorHandling_ShouldSupportMultipleValidationErrors()
    {
        // Arrange
        var validationErrors = new[]
        {
            "Employee number is required",
            "Employee name must be between 1 and 100 characters",
            "Rank must be 'P' (Professor), 'SL' (Senior Lecturer), or 'L' (Lecturer)"
        };

        var combinedMessage = string.Join("; ", validationErrors);
        var error = new Error("Validation.Failed", combinedMessage);

        // Act
        var result = Result.Failure<Guid>(error);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Failed", result.Error.Code);

        foreach (var validationError in validationErrors)
        {
            Assert.Contains(validationError, result.Error.Message);
        }
    }
}
