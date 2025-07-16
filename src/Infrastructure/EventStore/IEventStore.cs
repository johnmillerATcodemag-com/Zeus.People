using Zeus.People.Domain.Events;

namespace Zeus.People.Infrastructure.EventStore;

/// <summary>
/// Event store interface for domain event persistence
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Appends events to the event store
    /// </summary>
    Task AppendEventsAsync(Guid aggregateId, string aggregateType, IEnumerable<IDomainEvent> events, int expectedVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events for an aggregate
    /// </summary>
    Task<IEnumerable<IDomainEvent>> GetEventsAsync(Guid aggregateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets events for an aggregate from a specific version
    /// </summary>
    Task<IEnumerable<IDomainEvent>> GetEventsFromVersionAsync(Guid aggregateId, int fromVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all events from a specific timestamp
    /// </summary>
    Task<IEnumerable<IDomainEvent>> GetEventsFromTimestampAsync(DateTime timestamp, CancellationToken cancellationToken = default);
}
