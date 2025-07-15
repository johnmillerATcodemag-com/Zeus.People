using Zeus.People.Domain.Events;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// Base abstract class for aggregate roots
/// </summary>
public abstract class AggregateRoot : IEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot()
    {
        Id = Guid.NewGuid();
        CreatedAt = DateTime.UtcNow;
        ModifiedAt = DateTime.UtcNow;
    }

    protected AggregateRoot(Guid id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        ModifiedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime ModifiedAt { get; protected set; }

    /// <summary>
    /// Gets the list of domain events that have been raised by this aggregate
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the list of events to be published
    /// </summary>
    /// <param name="domainEvent">The domain event to add</param>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Clears all domain events from the aggregate
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
