using System.Linq.Expressions;
using Zeus.People.Application.Common;
using Zeus.People.Application.Interfaces;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Repositories;

/// <summary>
/// No-operation implementation of IDepartmentRepository for Cosmos-only deployments
/// </summary>
public class NoOpDepartmentRepository : IDepartmentRepository
{
    public Task<Result<Guid>> AddAsync(Department department, CancellationToken cancellationToken = default)
    {
        // Return success with empty Guid to indicate operation was accepted but not persisted
        return Task.FromResult(Result.Success(Guid.Empty));
    }

    public Task<Result> UpdateAsync(Department department, CancellationToken cancellationToken = default)
    {
        // Return success to indicate operation was accepted but not persisted
        return Task.FromResult(Result.Success());
    }

    public Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        // Return success to indicate operation was accepted but not persisted
        return Task.FromResult(Result.Success());
    }

    public Task<Result<Department?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<Department?>(null));
    }

    public Task<Result<Department?>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success<Department?>(null));
    }

    public Task<Result<bool>> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success(false));
    }

    public Task<Result<List<Department>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Result.Success(new List<Department>()));
    }
}
