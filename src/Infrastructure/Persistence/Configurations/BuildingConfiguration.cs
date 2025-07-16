using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Building entity
/// </summary>
public class BuildingConfiguration : IEntityTypeConfiguration<Building>
{
    public void Configure(EntityTypeBuilder<Building> builder)
    {
        builder.ToTable("Buildings");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .ValueGeneratedNever();

        builder.Property(b => b.BldgNr)
            .IsRequired()
            .HasConversion(
                bldgNr => bldgNr.Value,
                value => Domain.ValueObjects.BldgNr.Create(value))
            .HasMaxLength(50);

        builder.Property(b => b.BldgName)
            .IsRequired()
            .HasConversion(
                bldgName => bldgName.Value,
                value => Domain.ValueObjects.BldgName.Create(value))
            .HasMaxLength(100);

        // Ignore domain events
        builder.Ignore(b => b.DomainEvents);

        // Ignore collection properties
        builder.Ignore(b => b.RoomIds);

        // Concurrency token
        builder.Property<byte[]>("Version")
            .IsRowVersion();
    }
}
