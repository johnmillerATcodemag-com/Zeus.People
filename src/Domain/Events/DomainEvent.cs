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

    public Guid EventId { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public virtual int Version { get; protected set; }
}
