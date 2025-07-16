using Microsoft.EntityFrameworkCore;

namespace Zeus.People.Infrastructure.EventStore;

/// <summary>
/// Event Store DbContext for persisting domain events
/// </summary>
public class EventStoreContext : DbContext
{
    public EventStoreContext(DbContextOptions<EventStoreContext> options) : base(options)
    {
    }

    public DbSet<StoredEvent> Events { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<StoredEvent>(entity =>
        {
            entity.ToTable("EventStore");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.AggregateId)
                .IsRequired();

            entity.Property(e => e.AggregateType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.EventType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.EventData)
                .IsRequired();

            entity.Property(e => e.Version)
                .IsRequired();

            entity.Property(e => e.Timestamp)
                .IsRequired();

            entity.Property(e => e.EventId)
                .IsRequired();

            // Indexes for performance
            entity.HasIndex(e => e.AggregateId)
                .HasDatabaseName("IX_EventStore_AggregateId");

            entity.HasIndex(e => new { e.AggregateId, e.Version })
                .IsUnique()
                .HasDatabaseName("IX_EventStore_AggregateId_Version");

            entity.HasIndex(e => e.Timestamp)
                .HasDatabaseName("IX_EventStore_Timestamp");

            entity.HasIndex(e => e.EventId)
                .IsUnique()
                .HasDatabaseName("IX_EventStore_EventId");
        });
    }
}
