using Microsoft.EntityFrameworkCore;

namespace Zeus.People.Infrastructure.EventStore;

/// <summary>
/// Event store entity for persisting domain events
/// </summary>
public class StoredEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string EventData { get; set; } = null!;
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid EventId { get; set; }
}
