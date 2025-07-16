using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Room entity
/// </summary>
public class RoomConfiguration : IEntityTypeConfiguration<Room>
{
    public void Configure(EntityTypeBuilder<Room> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .ValueGeneratedNever();

        builder.Property(r => r.RoomNr)
            .IsRequired()
            .HasConversion(
                roomNr => roomNr.Value,
                value => Domain.ValueObjects.RoomNr.Create(value))
            .HasMaxLength(50);

        // Relationships
        builder.HasOne<Building>()
            .WithMany()
            .HasForeignKey(r => r.BuildingId)
            .OnDelete(DeleteBehavior.Restrict);

        // Ignore domain events
        builder.Ignore(r => r.DomainEvents);

        // Ignore collection properties
        builder.Ignore(r => r.AcademicIds);

        // Concurrency token
        builder.Property<byte[]>("Version")
            .IsRowVersion();
    }
}
