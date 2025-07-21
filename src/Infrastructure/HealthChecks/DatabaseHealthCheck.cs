using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Zeus.People.Infrastructure.Persistence;

namespace Zeus.People.Infrastructure.HealthChecks;

/// <summary>
/// Health check for the Academic database connection
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly AcademicContext _context;

    public DatabaseHealthCheck(AcademicContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to check if we can connect to the database
            var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

            if (canConnect)
            {
                // If we can connect, try a simple query
                await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);
                return HealthCheckResult.Healthy("Database connection is healthy");
            }
            else
            {
                return HealthCheckResult.Unhealthy("Database connection failed");
            }
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection failed",
                ex);
        }
    }
}
