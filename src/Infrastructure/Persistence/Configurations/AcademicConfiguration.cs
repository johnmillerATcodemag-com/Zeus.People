using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Academic entity
/// </summary>
public class AcademicConfiguration : IEntityTypeConfiguration<Academic>
{
    public void Configure(EntityTypeBuilder<Academic> builder)
    {
        builder.ToTable("Academics");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedNever();

        builder.Property(a => a.EmpNr)
            .IsRequired()
            .HasConversion(
                empNr => empNr.Value,
                value => Domain.ValueObjects.EmpNr.Create(value))
            .HasMaxLength(50);

        builder.OwnsOne(a => a.EmpName, empName =>
        {
            empName.Property(e => e.Value).HasColumnName("EmpName").IsRequired().HasMaxLength(100);
        });

        builder.OwnsOne(a => a.HomePhone, phone =>
        {
            phone.Property(p => p.Value).HasColumnName("HomePhoneNumber").HasMaxLength(20);
        });

        builder.Property(a => a.Rank)
            .IsRequired()
            .HasConversion(
                rank => rank.Value,
                value => Domain.ValueObjects.Rank.Create(value))
            .HasMaxLength(10);

        builder.Property(a => a.IsTenured)
            .IsRequired();

        builder.Property(a => a.ContractEndDate)
            .IsRequired(false);

        // Relationships
        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(a => a.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Room>()
            .WithMany()
            .HasForeignKey(a => a.RoomId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne<Extension>()
            .WithMany()
            .HasForeignKey(a => a.ExtensionId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne<Chair>()
            .WithMany()
            .HasForeignKey(a => a.ChairId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Many-to-many relationships with explicit join entity
        builder.HasMany<Degree>()
            .WithMany()
            .UsingEntity<AcademicDegree>(
                "AcademicDegrees",
                l => l.HasOne<Degree>().WithMany().HasForeignKey(ad => ad.DegreeId),
                r => r.HasOne<Academic>().WithMany().HasForeignKey(ad => ad.AcademicId),
                j =>
                {
                    j.HasKey(ad => new { ad.AcademicId, ad.DegreeId });
                    j.Property(ad => ad.UniversityId).IsRequired();
                });

        builder.HasMany<Subject>()
            .WithMany()
            .UsingEntity(
                "AcademicSubjects",
                l => l.HasOne(typeof(Subject)).WithMany().HasForeignKey("SubjectId"),
                r => r.HasOne(typeof(Academic)).WithMany().HasForeignKey("AcademicId"),
                j => j.HasKey("AcademicId", "SubjectId"));

        // Ignore domain events (handled in base class)
        builder.Ignore(a => a.DomainEvents);

        // Ignore collection properties (relationships handled via join tables)
        builder.Ignore(a => a.SubjectIds);
        builder.Ignore(a => a.DegreeIds);

        // Concurrency token
        builder.Property<byte[]>("Version")
            .IsRowVersion();
    }
}
