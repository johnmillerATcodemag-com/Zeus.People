using System.Linq.Expressions;
using Zeus.People.Application.Interfaces;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Infrastructure.Persistence.Repositories;

/// <summary>
/// No-operation implementation of IAcademicRepository for Cosmos-only deployments
/// </summary>
public class NoOpAcademicRepository : IAcademicRepository
{
    public Task<Academic?> GetByIdAsync(AcademicId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Academic?>(null);
    }

    public Task<Academic?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<Academic?>(null);
    }

    public Task<IEnumerable<Academic>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<Academic>());
    }

    public Task<IEnumerable<Academic>> FindAsync(Expression<Func<Academic, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Enumerable.Empty<Academic>());
    }

    public Task<bool> ExistsAsync(AcademicId id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task AddAsync(Academic academic, CancellationToken cancellationToken = default)
    {
        // No-op - write operations are not supported in Cosmos-only mode
        return Task.CompletedTask;
    }

    public Task UpdateAsync(Academic academic, CancellationToken cancellationToken = default)
    {
        // No-op - write operations are not supported in Cosmos-only mode
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Academic academic, CancellationToken cancellationToken = default)
    {
        // No-op - write operations are not supported in Cosmos-only mode
        return Task.CompletedTask;
    }

    public Task<int> CountAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(0);
    }

    public Task<bool> AnyAsync(Expression<Func<Academic, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }
}
