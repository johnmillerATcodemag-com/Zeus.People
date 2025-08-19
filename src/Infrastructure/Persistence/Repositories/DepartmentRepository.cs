using Microsoft.EntityFrameworkCore;
using Zeus.People.Application.Common;
using Zeus.People.Application.Interfaces;
using Zeus.People.Domain.Entities;

namespace Zeus.People.Infrastructure.Persistence.Repositories;

/// <summary>
/// Department repository implementation for command operations
/// </summary>
public class DepartmentRepository : IDepartmentRepository
{
    private readonly AcademicContext _context;

    public DepartmentRepository(AcademicContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Result<Guid>> AddAsync(Department department, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Departments.Add(department);
            return Task.FromResult(Result<Guid>.Success(department.Id));
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure<Guid>(new Error("Department.AddFailed", ex.Message)));
        }
    }

    public Task<Result> UpdateAsync(Department department, CancellationToken cancellationToken = default)
    {
        try
        {
            _context.Departments.Update(department);
            return Task.FromResult(Result.Success());
        }
        catch (Exception ex)
        {
            return Task.FromResult(Result.Failure(new Error("Department.UpdateFailed", ex.Message)));
        }
    }

    public async Task<Result> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var department = await _context.Departments.FindAsync(new object[] { id }, cancellationToken);
            if (department == null)
            {
                return Result.Failure(new Error("Department.NotFound", $"Department with ID {id} not found"));
            }

            _context.Departments.Remove(department);
            return Result.Success();
        }
        catch (Exception ex)
        {
            return Result.Failure(new Error("Department.DeleteFailed", ex.Message));
        }
    }

    public async Task<Result<Department?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Id == id, cancellationToken);

            return Result<Department?>.Success(department);
        }
        catch (Exception ex)
        {
            return Result.Failure<Department?>(new Error("Department.GetByIdFailed", ex.Message));
        }
    }

    public async Task<Result<Department?>> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var department = await _context.Departments
                .FirstOrDefaultAsync(d => d.Name == name, cancellationToken);

            return Result<Department?>.Success(department);
        }
        catch (Exception ex)
        {
            return Result.Failure<Department?>(new Error("Department.GetByNameFailed", ex.Message));
        }
    }

    public async Task<Result<bool>> ExistsByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await _context.Departments
                .AnyAsync(d => d.Name == name, cancellationToken);

            return Result<bool>.Success(exists);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>(new Error("Department.ExistsByNameFailed", ex.Message));
        }
    }

    public async Task<Result<List<Department>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var departments = await _context.Departments
                .ToListAsync(cancellationToken);

            return Result<List<Department>>.Success(departments);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<Department>>(new Error("Department.GetAllFailed", ex.Message));
        }
    }

    public async Task<Result<List<Department>>> GetWithBudgetAsync(decimal minBudget, CancellationToken cancellationToken = default)
    {
        try
        {
            var departments = await _context.Departments
                .Where(d => (d.ResearchBudget != null && d.ResearchBudget.Value >= minBudget) ||
                           (d.TeachingBudget != null && d.TeachingBudget.Value >= minBudget))
                .ToListAsync(cancellationToken);

            return Result<List<Department>>.Success(departments);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<Department>>(new Error("Department.GetWithBudgetFailed", ex.Message));
        }
    }

    public async Task<Result<List<Department>>> GetWithoutHeadsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var departments = await _context.Departments
                .Where(d => d.HeadProfessorId == null)
                .ToListAsync(cancellationToken);

            return Result<List<Department>>.Success(departments);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<Department>>(new Error("Department.GetWithoutHeadsFailed", ex.Message));
        }
    }
}
