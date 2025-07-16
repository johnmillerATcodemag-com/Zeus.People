using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Zeus.People.Infrastructure.EventStore;

namespace Zeus.People.Infrastructure.HealthChecks;

/// <summary>
/// Health check for the Event Store database connection
/// </summary>
public class EventStoreHealthCheck : IHealthCheck
{
    private readonly EventStoreContext _context;

    public EventStoreHealthCheck(EventStoreContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to execute a simple query to check event store connectivity
            await _context.Database.ExecuteSqlRawAsync("SELECT 1", cancellationToken);

            return HealthCheckResult.Healthy("Event Store connection is healthy");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Event Store connection failed",
                ex);
        }
    }
}
