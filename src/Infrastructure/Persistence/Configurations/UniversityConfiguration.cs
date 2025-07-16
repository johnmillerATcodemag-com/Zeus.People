using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for University entity
/// </summary>
public class UniversityConfiguration : IEntityTypeConfiguration<University>
{
    public void Configure(EntityTypeBuilder<University> builder)
    {
        builder.ToTable("Universities");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .ValueGeneratedNever();

        builder.Property(u => u.Code)
            .IsRequired()
            .HasMaxLength(20);

        // Ignore domain events
        builder.Ignore(u => u.DomainEvents);

        // Ignore collection properties
        builder.Ignore(u => u.DegreeIds);

        // Concurrency token
        builder.Property<byte[]>("Version")
            .IsRowVersion();
    }
}
