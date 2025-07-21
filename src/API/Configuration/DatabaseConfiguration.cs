using System.ComponentModel.DataAnnotations;

namespace Zeus.People.API.Configuration;

/// <summary>
/// Database configuration settings with validation
/// </summary>
public class DatabaseConfiguration
{
    public const string SectionName = "DatabaseSettings";

    /// <summary>
    /// Write database connection string (for commands and writes)
    /// </summary>
    [Required(ErrorMessage = "Write database connection string is required")]
    public string WriteConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Read database connection string (for queries and reads)
    /// </summary>
    [Required(ErrorMessage = "Read database connection string is required")]
    public string ReadConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Event store database connection string
    /// </summary>
    [Required(ErrorMessage = "Event store database connection string is required")]
    public string EventStoreConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Command timeout in seconds for database operations
    /// </summary>
    [Range(1, 300, ErrorMessage = "Command timeout must be between 1 and 300 seconds")]
    public int CommandTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Enable sensitive data logging (should be false in production)
    /// </summary>
    public bool EnableSensitiveDataLogging { get; set; } = false;

    /// <summary>
    /// Maximum retry count for database operations
    /// </summary>
    [Range(0, 10, ErrorMessage = "Max retry count must be between 0 and 10")]
    public int MaxRetryCount { get; set; } = 3;

    /// <summary>
    /// Maximum retry delay for database operations
    /// </summary>
    public TimeSpan MaxRetryDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Connection pool minimum size
    /// </summary>
    [Range(1, 100, ErrorMessage = "Connection pool minimum size must be between 1 and 100")]
    public int ConnectionPoolMinSize { get; set; } = 5;

    /// <summary>
    /// Connection pool maximum size
    /// </summary>
    [Range(1, 1000, ErrorMessage = "Connection pool maximum size must be between 1 and 1000")]
    public int ConnectionPoolMaxSize { get; set; } = 100;

    /// <summary>
    /// Connection lifetime in minutes
    /// </summary>
    [Range(1, 60, ErrorMessage = "Connection lifetime must be between 1 and 60 minutes")]
    public int ConnectionLifetimeMinutes { get; set; } = 15;

    /// <summary>
    /// Validate the database configuration
    /// </summary>
    public void Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Database configuration validation failed: {errors}");
        }

        // Additional business logic validation
        if (CommandTimeoutSeconds < 5)
        {
            throw new InvalidOperationException("Command timeout should be at least 5 seconds for production use");
        }

        if (ConnectionPoolMaxSize < ConnectionPoolMinSize)
        {
            throw new InvalidOperationException("Connection pool maximum size must be greater than or equal to minimum size");
        }
    }
}
