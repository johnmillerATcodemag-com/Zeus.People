using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Zeus.People.Domain.Events;

namespace Zeus.People.Infrastructure.EventStore;

/// <summary>
/// SQL Server implementation of the event store
/// </summary>
public class SqlEventStore : IEventStore
{
    private readonly EventStoreContext _context;

    public SqlEventStore(EventStoreContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task AppendEventsAsync(Guid aggregateId, string aggregateType, IEnumerable<IDomainEvent> events, int expectedVersion, CancellationToken cancellationToken = default)
    {
        var eventList = events.ToList();
        if (!eventList.Any())
            return;

        // Check current version to prevent concurrency issues
        var currentVersion = await GetCurrentVersionAsync(aggregateId, cancellationToken);
        if (currentVersion != expectedVersion)
        {
            throw new InvalidOperationException($"Concurrency conflict. Expected version {expectedVersion}, but current version is {currentVersion}");
        }

        var storedEvents = new List<StoredEvent>();
        var version = expectedVersion;

        foreach (var domainEvent in eventList)
        {
            version++;
            var eventData = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());

            var storedEvent = new StoredEvent
            {
                AggregateId = aggregateId,
                AggregateType = aggregateType,
                EventType = domainEvent.GetType().Name,
                EventData = eventData,
                Version = version,
                Timestamp = domainEvent.OccurredAt,
                EventId = domainEvent.EventId
            };

            storedEvents.Add(storedEvent);
        }

        _context.Events.AddRange(storedEvents);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var storedEvents = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        return DeserializeEvents(storedEvents);
    }

    public async Task<IEnumerable<IDomainEvent>> GetEventsFromVersionAsync(Guid aggregateId, int fromVersion, CancellationToken cancellationToken = default)
    {
        var storedEvents = await _context.Events
            .Where(e => e.AggregateId == aggregateId && e.Version > fromVersion)
            .OrderBy(e => e.Version)
            .ToListAsync(cancellationToken);

        return DeserializeEvents(storedEvents);
    }

    public async Task<IEnumerable<IDomainEvent>> GetEventsFromTimestampAsync(DateTime timestamp, CancellationToken cancellationToken = default)
    {
        var storedEvents = await _context.Events
            .Where(e => e.Timestamp >= timestamp)
            .OrderBy(e => e.Timestamp)
            .ThenBy(e => e.Version)
            .ToListAsync(cancellationToken);

        return DeserializeEvents(storedEvents);
    }

    private async Task<int> GetCurrentVersionAsync(Guid aggregateId, CancellationToken cancellationToken = default)
    {
        var latestEvent = await _context.Events
            .Where(e => e.AggregateId == aggregateId)
            .OrderByDescending(e => e.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return latestEvent?.Version ?? 0;
    }

    private static IEnumerable<IDomainEvent> DeserializeEvents(IEnumerable<StoredEvent> storedEvents)
    {
        var events = new List<IDomainEvent>();

        foreach (var storedEvent in storedEvents)
        {
            try
            {
                // Get the event type from the assembly
                var eventType = GetEventType(storedEvent.EventType);
                if (eventType != null)
                {
                    var domainEvent = JsonSerializer.Deserialize(storedEvent.EventData, eventType) as IDomainEvent;
                    if (domainEvent != null)
                    {
                        events.Add(domainEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error but continue processing other events
                // In production, you might want to use a proper logging framework
                Console.WriteLine($"Failed to deserialize event {storedEvent.EventType}: {ex.Message}");
            }
        }

        return events;
    }

    private static Type? GetEventType(string eventTypeName)
    {
        // Look for the event type in the Domain assembly
        var domainAssembly = typeof(IDomainEvent).Assembly;
        return domainAssembly.GetTypes()
            .FirstOrDefault(t => t.Name == eventTypeName && typeof(IDomainEvent).IsAssignableFrom(t));
    }
}
