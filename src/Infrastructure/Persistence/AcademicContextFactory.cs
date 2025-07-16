using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Zeus.People.Infrastructure.Persistence;

/// <summary>
/// Factory for creating AcademicContext instances at design time for Entity Framework migrations
/// </summary>
public class AcademicContextFactory : IDesignTimeDbContextFactory<AcademicContext>
{
    public AcademicContext CreateDbContext(string[] args)
    {
        // Use a simple connection string for migrations
        var connectionString = "Server=(localdb)\\mssqllocaldb;Database=Zeus.People;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true;";

        // Configure DbContext options
        var optionsBuilder = new DbContextOptionsBuilder<AcademicContext>();
        optionsBuilder.UseSqlServer(connectionString, options =>
        {
            options.MigrationsAssembly(typeof(AcademicContext).Assembly.FullName);
            options.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);
        });

        optionsBuilder.EnableSensitiveDataLogging();
        optionsBuilder.EnableDetailedErrors();

        return new AcademicContext(optionsBuilder.Options);
    }
}
