using System.Text.Json.Serialization;

namespace Zeus.People.Domain.Events;

/// <summary>
/// Base abstract class for all domain events
/// </summary>
public abstract class DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        OccurredAt = DateTime.UtcNow;
        Version = 1;
    }

    [JsonConstructor]
    protected DomainEvent(Guid eventId, DateTime occurredAt, int version)
    {
        EventId = eventId;
        OccurredAt = occurredAt;
        Version = version;
    }

    public Guid EventId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public virtual int Version { get; protected set; }
}
