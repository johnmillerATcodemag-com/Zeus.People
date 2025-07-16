using Microsoft.EntityFrameworkCore;
using Zeus.People.Application.Common;
using Zeus.People.Application.Interfaces;
using Zeus.People.Domain.Entities;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Infrastructure.Persistence.Repositories;

/// <summary>
/// Academic repository implementation for command operations
/// </summary>
public class AcademicRepository : IAcademicRepository
{
    private readonly AcademicContext _context;

    public AcademicRepository(AcademicContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Result<Guid>> AddAsync(Academic academic, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Academics.Add(academic);
            return Result<Guid>.Success(academic.Id);
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>(new Error("Academic.AddFailed", ex.Message));
        }
    }

    public async Task<Result> UpdateAsync(Academic academic, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Academics.Update(academic);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>(new Error("Academic.Error", new Error("Academic.UpdateFailed", ex.Message)));
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var academic = await _context.Academics.FindAsync(new object[] { id }, cancellationToken);
            if (academic == null)
            {
                return Result.Failure<Guid>(new Error("Academic.Error", new Error("Academic.NotFound", $"Academic with ID {id} not found")));
            }

            _context.Academics.Remove(academic);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure<Guid>(new Error("Academic.Error", new Error("Academic.DeleteFailed", ex.Message)));
        }
    }

    public async Task<Result<Academic?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var academic = await _context.Academics
                .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

            return Result<Academic?>.Success(academic);
        }
        catch (Exception ex)
        {
            return Result.Failure<Academic?>(new Error("Academic.GetByIdFailed", ex.Message));
        }
    }

    public async Task<Result<Academic?>> GetByEmpNrAsync(string empNr, CancellationToken cancellationToken = default)
    {
        try
        {
            var empNrValue = EmpNr.Create(empNr);
            var academic = await _context.Academics
                .FirstOrDefaultAsync(a => a.EmpNr == empNrValue, cancellationToken);

            return Result<Academic?>.Success(academic);
        }
        catch (Exception ex)
        {
            return Result.Failure<Academic?>(new Error("Academic.GetByEmpNrFailed", ex.Message));
        }
    }

    public async Task<Result<bool>> ExistsByEmpNrAsync(string empNr, CancellationToken cancellationToken = default)
    {
        try
        {
            var empNrValue = EmpNr.Create(empNr);
            var exists = await _context.Academics
                .AnyAsync(a => a.EmpNr == empNrValue, cancellationToken);

            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>(new Error("Academic.ExistsByEmpNrFailed", ex.Message));
        }
    }

    public async Task<Result<List<Academic>>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken cancellationToken = default)
    {
        try
        {
            var academics = await _context.Academics
                .Where(a => a.DepartmentId == departmentId)
                .ToListAsync(cancellationToken);

            return Result<List<Academic>>.Success(academics);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<Academic>>(new Error("Academic.GetByDepartmentFailed", ex.Message));
        }
    }

    public async Task<Result<List<Academic>>> GetByRankAsync(string rank, CancellationToken cancellationToken = default)
    {
        try
        {
            var rankValue = Rank.Create(rank);
            var academics = await _context.Academics
                .Where(a => a.Rank == rankValue)
                .ToListAsync(cancellationToken);

            return Result<List<Academic>>.Success(academics);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<Academic>>(new Error("Academic.GetByRankFailed", ex.Message));
        }
    }

    public async Task<Result<List<Academic>>> GetTenuredAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var academics = await _context.Academics
                .Where(a => a.IsTenured)
                .ToListAsync(cancellationToken);

            return Result<List<Academic>>.Success(academics);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<Academic>>(new Error("Academic.GetTenuredFailed", ex.Message));
        }
    }

    public async Task<Result<List<Academic>>> GetWithExpiringContractsAsync(DateTime beforeDate, CancellationToken cancellationToken = default)
    {
        try
        {
            var academics = await _context.Academics
                .Where(a => a.ContractEndDate.HasValue && a.ContractEndDate <= beforeDate)
                .ToListAsync(cancellationToken);

            return Result<List<Academic>>.Success(academics);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<Academic>>(new Error("Academic.GetWithExpiringContractsFailed", ex.Message));
        }
    }
}
