using Microsoft.EntityFrameworkCore;
using Zeus.People.Application.Common;
using Zeus.People.Application.Interfaces;
using Zeus.People.Domain.Entities;
using Zeus.People.Infrastructure.Persistence;

namespace Zeus.People.Infrastructure.Persistence.Repositories;

/// <summary>
/// Room repository implementation for command operations
/// </summary>
public class RoomRepository : IRoomRepository
{
    private readonly AcademicContext _context;

    public RoomRepository(AcademicContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Result<Room?>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        try
        {
            var room = await _context.Rooms
                .Where(r => r.Id == id)
                .FirstOrDefaultAsync(cancellationToken);

            return Result<Room?>.Success(room);
        }
        catch (Exception ex)
        {
            return Result.Failure<Room?>(new Error("Room.RetrievalError", $"Error retrieving room by ID: {ex.Message}"));
        }
    }

    public async Task<Result<Room?>> GetByRoomNumberAsync(string roomNumber, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(roomNumber))
                return Result.Failure<Room?>(new Error("Room.InvalidInput", "Room number cannot be null or empty"));

            var room = await _context.Rooms
                .Where(r => r.RoomNr.Value == roomNumber)
                .FirstOrDefaultAsync(cancellationToken);

            return Result<Room?>.Success(room);
        }
        catch (Exception ex)
        {
            return Result.Failure<Room?>(new Error("Room.RetrievalError", $"Error retrieving room by room number: {ex.Message}"));
        }
    }

    public async Task<Result<bool>> IsRoomOccupiedAsync(Guid roomId, CancellationToken cancellationToken = default)
    {
        try
        {
            var room = await _context.Rooms
                .Where(r => r.Id == roomId)
                .FirstOrDefaultAsync(cancellationToken);

            if (room == null)
                return Result.Failure<bool>(new Error("Room.NotFound", "Room not found"));

            // Check if room is occupied - you may need to implement this logic based on your domain model
            // This is a placeholder implementation
            bool isOccupied = false; // Implement based on your business rules

            return Result<bool>.Success(isOccupied);
        }
        catch (Exception ex)
        {
            return Result.Failure<bool>(new Error("Room.OccupancyCheckError", $"Error checking room occupancy: {ex.Message}"));
        }
    }

    public async Task<Result<List<Room>>> GetAvailableRoomsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var availableRooms = await _context.Rooms
                .Where(r => true) // Add your availability logic here
                .ToListAsync(cancellationToken);

            return Result<List<Room>>.Success(availableRooms);
        }
        catch (Exception ex)
        {
            return Result.Failure<List<Room>>(new Error("Room.RetrievalError", $"Error retrieving available rooms: {ex.Message}"));
        }
    }
}
