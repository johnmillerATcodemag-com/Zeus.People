namespace Zeus.People.Infrastructure.Configuration;

/// <summary>
/// Configuration settings for Azure Service Bus
/// </summary>
public class ServiceBusConfiguration
{
    public const string SectionName = "ServiceBus";

    public string Namespace { get; set; } = string.Empty;
    public string TopicName { get; set; } = "domain-events";
    public string SubscriptionName { get; set; } = "zeus-people-subscription";
    public bool UseManagedIdentity { get; set; } = true;
    public int MaxRetryAttempts { get; set; } = 3;
    public int DelayBetweenRetriesInSeconds { get; set; } = 2;
    public int MaxDelayInSeconds { get; set; } = 30;
}
