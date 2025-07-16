using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for Department entity
/// </summary>
public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("Departments");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id)
            .ValueGeneratedNever();

        builder.Property(d => d.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Budget owned entities
        builder.OwnsOne(d => d.ResearchBudget, budget =>
        {
            budget.Property(b => b.Value).HasColumnName("ResearchBudgetAmount").HasColumnType("decimal(18,2)");
        });

        builder.OwnsOne(d => d.TeachingBudget, budget =>
        {
            budget.Property(b => b.Value).HasColumnName("TeachingBudgetAmount").HasColumnType("decimal(18,2)");
        });

        builder.OwnsOne(d => d.HeadHomePhone, phone =>
        {
            phone.Property(p => p.Value).HasColumnName("HeadHomePhoneNumber").HasMaxLength(20);
        });

        // Navigation properties
        builder.Property(d => d.HeadProfessorId)
            .IsRequired(false);

        builder.Property(d => d.ChairId)
            .IsRequired(false);

        // Relationships
        builder.HasOne<Academic>()
            .WithMany()
            .HasForeignKey(d => d.HeadProfessorId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        builder.HasOne<Chair>()
            .WithMany()
            .HasForeignKey(d => d.ChairId)
            .OnDelete(DeleteBehavior.SetNull)
            .IsRequired(false);

        // Ignore domain events
        builder.Ignore(d => d.DomainEvents);

        // Ignore collection properties
        builder.Ignore(d => d.AcademicIds);

        // Concurrency token
        builder.Property<byte[]>("Version")
            .IsRowVersion();
    }
}
