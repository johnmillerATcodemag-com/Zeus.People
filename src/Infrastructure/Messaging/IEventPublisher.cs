using Zeus.People.Domain.Events;

namespace Zeus.People.Infrastructure.Messaging;

/// <summary>
/// Interface for publishing domain events to external systems
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publishes a single domain event
    /// </summary>
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent;

    /// <summary>
    /// Publishes multiple domain events
    /// </summary>
    Task PublishAsync<T>(IEnumerable<T> domainEvents, CancellationToken cancellationToken = default) where T : IDomainEvent;
}
