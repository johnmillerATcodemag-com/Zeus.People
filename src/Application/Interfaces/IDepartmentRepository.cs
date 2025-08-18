using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries.Academic;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Application.Interfaces;

/// <summary>
/// Repository interface for Department aggregate
/// </summary>
public interface IDepartmentRepository
{
    // Command operations
    Task<Result<Guid>> AddAsync(Department department, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Department department, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Query operations
    Task<Result<Department?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Department?>> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<bool>> ExistsByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<List<Department>>> GetAllAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only repository interface for Department queries
/// </summary>
public interface IDepartmentReadRepository
{
    Task<Result<DepartmentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<DepartmentDto>> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<DepartmentSummaryDto>>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? nameFilter = null,
        CancellationToken cancellationToken = default);
    Task<Result<DepartmentStaffCountDto>> GetStaffCountAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Result<List<DepartmentStaffCountDto>>> GetAllStaffCountsAsync(CancellationToken cancellationToken = default);
    Task<Result<List<DepartmentSummaryDto>>> GetWithBudgetAsync(
        decimal? minResearchBudget = null,
        decimal? minTeachingBudget = null,
        CancellationToken cancellationToken = default);
    Task<Result<List<DepartmentSummaryDto>>> GetWithoutHeadsAsync(CancellationToken cancellationToken = default);
}
