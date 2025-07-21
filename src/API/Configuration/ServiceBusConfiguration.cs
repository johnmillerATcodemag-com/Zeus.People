using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Zeus.People.API.Configuration;

/// <summary>
/// Service Bus configuration settings with validation and timeouts
/// </summary>
public class ServiceBusConfiguration
{
    public const string SectionName = "ServiceBusSettings";

    /// <summary>
    /// Service Bus connection string
    /// </summary>
    [Required(ErrorMessage = "Service Bus connection string is required")]
    [JsonIgnore] // Sensitive data should not be serialized
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Service Bus namespace (when using managed identity)
    /// </summary>
    public string Namespace { get; set; } = string.Empty;

    /// <summary>
    /// Topic name for domain events
    /// </summary>
    [Required(ErrorMessage = "Topic name is required")]
    public string TopicName { get; set; } = "domain-events";

    /// <summary>
    /// Subscription name for this service
    /// </summary>
    [Required(ErrorMessage = "Subscription name is required")]
    public string SubscriptionName { get; set; } = "academic-management";

    /// <summary>
    /// Number of message retry attempts
    /// </summary>
    [Range(0, 10, ErrorMessage = "Message retry count must be between 0 and 10")]
    public int MessageRetryCount { get; set; } = 3;

    /// <summary>
    /// Message timeout duration
    /// </summary>
    public TimeSpan MessageTimeout { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Maximum concurrent calls for message processing
    /// </summary>
    [Range(1, 100, ErrorMessage = "Max concurrent calls must be between 1 and 100")]
    public int MaxConcurrentCalls { get; set; } = 10;

    /// <summary>
    /// Use managed identity for authentication
    /// </summary>
    public bool UseManagedIdentity { get; set; } = true;

    /// <summary>
    /// Auto-complete messages after successful processing
    /// </summary>
    public bool AutoCompleteMessages { get; set; } = true;

    /// <summary>
    /// Prefetch count for message batching
    /// </summary>
    [Range(0, 1000, ErrorMessage = "Prefetch count must be between 0 and 1000")]
    public int PrefetchCount { get; set; } = 10;

    /// <summary>
    /// Maximum wait time for messages
    /// </summary>
    public TimeSpan MaxWaitTime { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Session handling for ordered message processing
    /// </summary>
    public bool RequiresSession { get; set; } = false;

    /// <summary>
    /// Dead letter handling configuration
    /// </summary>
    public bool EnableDeadLetterQueue { get; set; } = true;

    /// <summary>
    /// Maximum delivery count before dead lettering
    /// </summary>
    [Range(1, 100, ErrorMessage = "Max delivery count must be between 1 and 100")]
    public int MaxDeliveryCount { get; set; } = 5;

    /// <summary>
    /// Validate the Service Bus configuration
    /// </summary>
    public void Validate()
    {
        var validationContext = new ValidationContext(this);
        var validationResults = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, validationContext, validationResults, true))
        {
            var errors = string.Join("; ", validationResults.Select(r => r.ErrorMessage));
            throw new InvalidOperationException($"Service Bus configuration validation failed: {errors}");
        }

        // Additional business logic validation
        if (UseManagedIdentity && string.IsNullOrEmpty(Namespace))
        {
            throw new InvalidOperationException("Namespace is required when using managed identity");
        }

        if (!UseManagedIdentity && string.IsNullOrEmpty(ConnectionString))
        {
            throw new InvalidOperationException("Connection string is required when not using managed identity");
        }

        if (MessageTimeout < TimeSpan.FromSeconds(30))
        {
            throw new InvalidOperationException("Message timeout should be at least 30 seconds");
        }

        if (MaxWaitTime > TimeSpan.FromMinutes(1))
        {
            throw new InvalidOperationException("Max wait time should not exceed 1 minute for optimal performance");
        }
    }
}
