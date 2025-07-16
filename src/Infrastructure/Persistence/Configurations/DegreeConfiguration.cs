using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Degree entity
/// </summary>
public class DegreeConfiguration : IEntityTypeConfiguration<Degree>
{
    public void Configure(EntityTypeBuilder<Degree> builder)
    {
        builder.ToTable("Degrees");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.Code)
            .IsRequired()
            .HasMaxLength(10);

        // Ignore navigation properties (handled in Academic configuration)
        builder.Ignore(d => d.AcademicDegrees);

        // Ignore domain events
        builder.Ignore(d => d.DomainEvents);

        // Concurrency token
        builder.Property<byte[]>("Version")
            .IsRowVersion();
    }
}
