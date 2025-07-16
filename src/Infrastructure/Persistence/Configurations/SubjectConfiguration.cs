using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Subject entity
/// </summary>
public class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> builder)
    {
        builder.ToTable("Subjects");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .ValueGeneratedNever();

        builder.Property(s => s.Code)
            .IsRequired()
            .HasMaxLength(100);

        // Ignore navigation properties (handled in Academic configuration)
        builder.Ignore(s => s.AcademicIds);
        builder.Ignore(s => s.Teachings);

        // Ignore domain events
        builder.Ignore(s => s.DomainEvents);

        // Concurrency token
        builder.Property<byte[]>("Version")
            .IsRowVersion();
    }
}
