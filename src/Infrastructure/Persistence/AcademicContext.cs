using Microsoft.EntityFrameworkCore;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.ValueObjects;
using Zeus.People.Infrastructure.Persistence.Configurations;

namespace Zeus.People.Infrastructure.Persistence;

/// <summary>
/// Academic context for write operations following CQRS pattern
/// Handles command-side operations with full domain model
/// </summary>
public class AcademicContext : BaseDbContext
{
    public AcademicContext(DbContextOptions<AcademicContext> options) : base(options)
    {
    }

    // Academic Entities
    public DbSet<Academic> Academics { get; set; } = null!;
    public DbSet<Degree> Degrees { get; set; } = null!;
    public DbSet<Subject> Subjects { get; set; } = null!;
    public DbSet<University> Universities { get; set; } = null!;

    // Department Entities
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Chair> Chairs { get; set; } = null!;
    public DbSet<Committee> Committees { get; set; } = null!;

    // Room and Extension Entities
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<Extension> Extensions { get; set; } = null!;
    public DbSet<Building> Buildings { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply entity configurations
        modelBuilder.ApplyConfiguration(new AcademicConfiguration());
        modelBuilder.ApplyConfiguration(new DepartmentConfiguration());
        modelBuilder.ApplyConfiguration(new DegreeConfiguration());
        modelBuilder.ApplyConfiguration(new SubjectConfiguration());
        modelBuilder.ApplyConfiguration(new UniversityConfiguration());
        modelBuilder.ApplyConfiguration(new ChairConfiguration());
        modelBuilder.ApplyConfiguration(new CommitteeConfiguration());
        modelBuilder.ApplyConfiguration(new RoomConfiguration());
        modelBuilder.ApplyConfiguration(new ExtensionConfiguration());
        modelBuilder.ApplyConfiguration(new BuildingConfiguration());

        // Configure value objects
        ConfigureValueObjects(modelBuilder);

        // Configure indexes for performance
        ConfigureIndexes(modelBuilder);
    }

    private static void ConfigureValueObjects(ModelBuilder modelBuilder)
    {
        // Employee Number value object
        modelBuilder.Entity<Academic>()
            .Property(a => a.EmpNr)
            .HasConversion(
                empNr => empNr.Value,
                value => EmpNr.Create(value))
            .HasMaxLength(10);

        // Room Number value object
        modelBuilder.Entity<Room>()
            .Property(r => r.RoomNr)
            .HasConversion(
                roomNr => roomNr.Value,
                value => RoomNr.Create(value))
            .HasMaxLength(10);

        // Extension Number value object
        modelBuilder.Entity<Extension>()
            .Property(e => e.ExtNr)
            .HasConversion(
                extNr => extNr.Value,
                value => ExtNr.Create(value))
            .HasMaxLength(10);

        // Money Amount value object
        modelBuilder.Entity<Department>()
            .OwnsOne(d => d.ResearchBudget, money =>
            {
                money.Property(m => m.Value).HasColumnName("ResearchBudgetAmount").HasPrecision(18, 2);
            });

        modelBuilder.Entity<Department>()
            .OwnsOne(d => d.TeachingBudget, money =>
            {
                money.Property(m => m.Value).HasColumnName("TeachingBudgetAmount").HasPrecision(18, 2);
            });
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Academic indexes
        modelBuilder.Entity<Academic>()
            .HasIndex(a => a.EmpNr)
            .IsUnique();

        modelBuilder.Entity<Academic>()
            .HasIndex(a => a.DepartmentId);

        // Department indexes
        modelBuilder.Entity<Department>()
            .HasIndex(d => d.Name)
            .IsUnique();

        // Room indexes  
        modelBuilder.Entity<Room>()
            .HasIndex(r => new { r.RoomNr, r.BuildingId })
            .IsUnique();

        // Extension indexes
        modelBuilder.Entity<Extension>()
            .HasIndex(e => e.ExtNr)
            .IsUnique();

        // Degree indexes
        modelBuilder.Entity<Degree>()
            .HasIndex(d => d.Code)
            .IsUnique();

        // Subject indexes
        modelBuilder.Entity<Subject>()
            .HasIndex(s => s.Code)
            .IsUnique();

        // Building indexes
        modelBuilder.Entity<Building>()
            .HasIndex(b => b.BldgName)
            .IsUnique();
    }
}
