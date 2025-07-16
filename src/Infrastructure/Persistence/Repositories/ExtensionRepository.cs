using Microsoft.EntityFrameworkCore;
using Zeus.People.Application.Common;
using Zeus.People.Application.Interfaces;
using Zeus.People.Domain.Entities;
using Zeus.People.Infrastructure.Persistence;

namespace Zeus.People.Infrastructure.Persistence.Repositories;

/// <summary>
/// Extension repository implementation for command operations
/// </summary>
public class ExtensionRepository : IExtensionRepository
{
    private readonly AcademicContext _context;

    public ExtensionRepository(AcademicContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Result<Extension?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var extension = await _context.Extensions
                .Where(e => e.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return Result<Extension?>.Success(extension);
        }
        catch (Exception ex)
        {
            return Result.Failure<Extension?>(new Error("Extension.RetrievalError", $"Error retrieving extension by ID: {ex.Message}"));
        }
    }

    public async Task<Result<Extension?>> GetByExtensionNumberAsync(string extensionNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(extensionNumber))
                return Result.Failure<Extension?>(new Error("Extension.InvalidInput", "Extension number cannot be null or empty"));

            var extension = await _context.Extensions
                .Where(e => e.ExtNr.Value == extensionNumber)
                .FirstOrDefaultAsync(cancellationToken);

            return Result<Extension?>.Success(extension);
        }
        catch (Exception ex)
        {
            return Result.Failure<Extension?>(new Error("Extension.RetrievalError", $"Error retrieving extension by extension number: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> IsExtensionInUseAsync(Guid extensionId, CancellationToken cancellationToken = default)
    {
        try
        {
            var extension = await _context.Extensions
                .Where(e => e.Id == extensionId)
                .FirstOrDefaultAsync(cancellationToken);

            if (extension == null)
                return Result.Failure<bool>(new Error("Extension.NotFound", "Extension not found"));

            // Check if extension is in use - implement based on your domain model
            // This is a placeholder implementation
            bool isInUse = false; // Implement based on your business rules

            return Result<bool>.Success(isInUse);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>(new Error("Extension.UsageCheckError", $"Error checking extension usage: {ex.Message}"));
        }
    }

    public async Task<Result<List<Extension>>> GetAvailableExtensionsAsync(string? accessLevelFilter = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var query = _context.Extensions.AsQueryable();

            // Note: AccessLevel filtering would require joining with Academic entity
            // since AccessLevel is derived from the assigned Academic's rank
            // For now, return all available extensions and filter in application layer

            // Add availability logic here
            var availableExtensions = await query
                .Where(e => !e.AcademicId.HasValue) // Available if not assigned to any academic
                .ToListAsync(cancellationToken);

            return Result<List<Extension>>.Success(availableExtensions);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<Extension>>(new Error("Extension.RetrievalError", $"Error retrieving available extensions: {ex.Message}"));
        }
    }
}
