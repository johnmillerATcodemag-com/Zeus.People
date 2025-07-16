using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Chair entity
/// </summary>
public class ChairConfiguration : IEntityTypeConfiguration<Chair>
{
    public void Configure(EntityTypeBuilder<Chair> builder)
    {
        builder.ToTable("Chairs");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .ValueGeneratedNever();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.ProfessorId)
            .IsRequired(false);

        // Relationships
        builder.HasOne<Academic>()
            .WithMany()
            .HasForeignKey(c => c.ProfessorId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Ignore domain events
        builder.Ignore(c => c.DomainEvents);

        // Concurrency token
        builder.Property<byte[]>("Version")
            .IsRowVersion();
    }
}
