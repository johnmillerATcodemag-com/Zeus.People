using Zeus.People.Application.Interfaces;

namespace Zeus.People.Infrastructure.Persistence;

/// <summary>
/// No-operation implementation of IUnitOfWork for Cosmos-only deployments
/// </summary>
public class NoOpUnitOfWork : IUnitOfWork
{
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // No-op - returns 0 to indicate no changes were saved
        await Task.CompletedTask;
        return 0;
    }

    public void Dispose()
    {
        // No-op
    }

    public async ValueTask DisposeAsync()
    {
        // No-op
        await ValueTask.CompletedTask;
    }
}
