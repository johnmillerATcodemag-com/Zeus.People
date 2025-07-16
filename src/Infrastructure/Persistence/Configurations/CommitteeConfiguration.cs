using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Committee entity
/// </summary>
public class CommitteeConfiguration : IEntityTypeConfiguration<Committee>
{
    public void Configure(EntityTypeBuilder<Committee> builder)
    {
        builder.ToTable("Committees");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Many-to-many relationship with teaching professors
        builder.HasMany<Academic>()
            .WithMany()
            .UsingEntity(
                "CommitteeTeachingProfessors",
                l => l.HasOne(typeof(Academic)).WithMany().HasForeignKey("AcademicId"),
                r => r.HasOne(typeof(Committee)).WithMany().HasForeignKey("CommitteeId"),
                j => j.HasKey("CommitteeId", "AcademicId"));

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);

        // Ignore collection properties
        builder.Ignore(c => c.TeachingProfessorIds);

        // Concurrency token
        builder.Property<byte[]>("Version")
            .IsRowVersion();
    }
}
