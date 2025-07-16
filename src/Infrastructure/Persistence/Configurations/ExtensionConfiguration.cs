using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Extension entity
/// </summary>
public class ExtensionConfiguration : IEntityTypeConfiguration<Extension>
{
    public void Configure(EntityTypeBuilder<Extension> builder)
    {
        builder.ToTable("Extensions");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .ValueGeneratedNever();

        builder.Property(e => e.ExtNr)
            .IsRequired()
            .HasConversion(
                extNr => extNr.Value,
                value => Domain.ValueObjects.ExtNr.Create(value))
            .HasMaxLength(50);

        builder.Property(e => e.AcademicId)
            .IsRequired(false);

        // Ignore domain events
        builder.Ignore(e => e.DomainEvents);

        // Concurrency token
        builder.Property<byte[]>("Version")
            .IsRowVersion();
    }
}
