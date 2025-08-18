using Zeus.People.Application.Common;
using Zeus.People.Application.DTOs;
using Zeus.People.Application.Queries.Academic;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Application.Interfaces;

/// <summary>
/// Repository interface for Academic aggregate
/// </summary>
public interface IAcademicRepository
{
    // Command operations
    Task<Result<Guid>> AddAsync(Academic academic, CancellationToken cancellationToken = default);
    Task<Result> UpdateAsync(Academic academic, CancellationToken cancellationToken = default);
    Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    // Query operations
    Task<Result<Academic?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<Academic?>> GetByEmpNrAsync(string empNr, CancellationToken cancellationToken = default);
    Task<Result<bool>> ExistsByEmpNrAsync(string empNr, CancellationToken cancellationToken = default);
    Task<Result<List<Academic>>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default);
    Task<Result<List<Academic>>> GetByRankAsync(string rank, CancellationToken cancellationToken = default);
    Task<Result<List<Academic>>> GetTenuredAsync(CancellationToken cancellationToken = default);
    Task<Result<List<Academic>>> GetWithExpiringContractsAsync(DateTime beforeDate, CancellationToken cancellationToken = default);
}

/// <summary>
/// Read-only repository interface for Academic queries
/// </summary>
public interface IAcademicReadRepository
{
    Task<Result<AcademicDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Result<AcademicDto>> GetByEmpNrAsync(string empNr, CancellationToken cancellationToken = default);
    Task<Result<PagedResult<AcademicSummaryDto>>> GetAllAsync(
        int pageNumber,
        int pageSize,
        string? nameFilter = null,
        string? rankFilter = null,
        bool? isTenuredFilter = null,
        CancellationToken cancellationToken = default);
    Task<Result<PagedResult<AcademicSummaryDto>>> GetByDepartmentAsync(
        Guid departmentId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Result<PagedResult<AcademicSummaryDto>>> GetByRankAsync(
        string rank,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Result<PagedResult<AcademicSummaryDto>>> GetTenuredAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Result<PagedResult<AcademicSummaryDto>>> GetWithExpiringContractsAsync(
        DateTime? beforeDate,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<Result<List<AcademicCountByDepartmentDto>>> GetCountByDepartmentAsync(CancellationToken cancellationToken = default);
}
