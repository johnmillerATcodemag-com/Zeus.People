using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Exceptions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// Building entity - represents physical buildings with unique identifiers
/// </summary>
public class Building : AggregateRoot
{
    private readonly List<Guid> _roomIds = new();

    // Private constructor for EF
    private Building() { }

    private Building(BldgNr bldgNr, BldgName bldgName) : base()
    {
        BldgNr = bldgNr ?? throw new ArgumentNullException(nameof(bldgNr));
        BldgName = bldgName ?? throw new ArgumentNullException(nameof(bldgName));
    }

    public BldgNr BldgNr { get; private set; } = null!;
    public BldgName BldgName { get; private set; } = null!;

    // Collections
    public IReadOnlyList<Guid> RoomIds => _roomIds.AsReadOnly();

    /// <summary>
    /// Creates a new building
    /// </summary>
    public static Building Create(BldgNr bldgNr, BldgName bldgName)
    {
        return new Building(bldgNr, bldgName);
    }

    /// <summary>
    /// Adds a room to the building
    /// </summary>
    public void AddRoom(Guid roomId)
    {
        if (roomId == Guid.Empty) throw new ArgumentException("Room ID cannot be empty", nameof(roomId));

        if (!_roomIds.Contains(roomId))
        {
            _roomIds.Add(roomId);
        }
    }

    /// <summary>
    /// Removes a room from the building
    /// </summary>
    public void RemoveRoom(Guid roomId)
    {
        _roomIds.Remove(roomId);
    }

    /// <summary>
    /// Business rule validation: Building must have at least one room
    /// </summary>
    public void ValidateHasRooms()
    {
        if (_roomIds.Count == 0)
        {
            throw new BusinessRuleViolationException("Building has one or more Room");
        }
    }
}
