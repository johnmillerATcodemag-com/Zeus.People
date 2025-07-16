using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using Zeus.People.Domain.Events;

namespace Zeus.People.Infrastructure.Messaging;

/// <summary>
/// Azure Service Bus implementation of event publisher
/// </summary>
public class ServiceBusEventPublisher : IEventPublisher, IAsyncDisposable
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ServiceBusSender _sender;
    private readonly ILogger<ServiceBusEventPublisher> _logger;
    private readonly ServiceBusConfiguration _configuration;

    public ServiceBusEventPublisher(
        ServiceBusClient serviceBusClient,
        IOptions<ServiceBusConfiguration> configuration,
        ILogger<ServiceBusEventPublisher> logger)
    {
        _serviceBusClient = serviceBusClient ?? throw new ArgumentNullException(nameof(serviceBusClient));
        _configuration = configuration.Value ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _sender = _serviceBusClient.CreateSender(_configuration.TopicName);
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        try
        {
            var message = CreateMessage(domainEvent);
            await _sender.SendMessageAsync(message, cancellationToken);

            _logger.LogInformation("Published event {EventType} with ID {EventId} to Service Bus",
                typeof(T).Name, domainEvent.EventId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventType} with ID {EventId} to Service Bus",
                typeof(T).Name, domainEvent.EventId);
            throw;
        }
    }

    public async Task PublishAsync<T>(IEnumerable<T> domainEvents, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        var eventList = domainEvents.ToList();
        if (!eventList.Any())
            return;

        try
        {
            var messages = eventList.Select(CreateMessage).ToList();
            await _sender.SendMessagesAsync(messages, cancellationToken);

            _logger.LogInformation("Published {EventCount} events of type {EventType} to Service Bus",
                eventList.Count, typeof(T).Name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish {EventCount} events of type {EventType} to Service Bus",
                eventList.Count, typeof(T).Name);
            throw;
        }
    }

    private static ServiceBusMessage CreateMessage<T>(T domainEvent) where T : IDomainEvent
    {
        var eventData = JsonSerializer.Serialize(domainEvent, typeof(T));

        var message = new ServiceBusMessage(eventData)
        {
            MessageId = domainEvent.EventId.ToString(),
            Subject = typeof(T).Name,
            ContentType = "application/json"
        };

        // Add custom properties for routing and filtering
        message.ApplicationProperties["EventType"] = typeof(T).Name;
        message.ApplicationProperties["EventId"] = domainEvent.EventId.ToString();
        message.ApplicationProperties["OccurredAt"] = domainEvent.OccurredAt.ToString("O");
        message.ApplicationProperties["Version"] = domainEvent.Version;

        return message;
    }

    public async ValueTask DisposeAsync()
    {
        if (_sender != null)
        {
            await _sender.DisposeAsync();
        }

        if (_serviceBusClient != null)
        {
            await _serviceBusClient.DisposeAsync();
        }
    }
}

/// <summary>
/// Configuration options for Azure Service Bus
/// </summary>
public class ServiceBusConfiguration
{
    public const string SectionName = "ServiceBus";

    public string ConnectionString { get; set; } = null!;
    public string TopicName { get; set; } = "academic-events";
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(2);
}
