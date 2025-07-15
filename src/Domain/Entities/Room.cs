using Zeus.People.Domain.Entities;
using Zeus.People.Domain.Exceptions;
using Zeus.People.Domain.ValueObjects;

namespace Zeus.People.Domain.Entities;

/// <summary>
/// Room entity - represents physical rooms with unique identifiers
/// Business rule: The combination of a Room has roomNr and Room is in Building is unique
/// </summary>
public class Room : AggregateRoot
{
    private readonly List<Guid> _academicIds = new();

    // Private constructor for EF
    private Room() { }

    private Room(RoomNr roomNr, Guid buildingId) : base()
    {
        RoomNr = roomNr ?? throw new ArgumentNullException(nameof(roomNr));
        BuildingId = buildingId != Guid.Empty ? buildingId : throw new ArgumentException("Building ID cannot be empty", nameof(buildingId));
    }

    public RoomNr RoomNr { get; private set; } = null!;
    public Guid BuildingId { get; private set; }

    // Collections
    public IReadOnlyList<Guid> AcademicIds => _academicIds.AsReadOnly();

    /// <summary>
    /// Creates a new room
    /// </summary>
    public static Room Create(RoomNr roomNr, Guid buildingId)
    {
        return new Room(roomNr, buildingId);
    }

    /// <summary>
    /// Assigns an academic to this room
    /// </summary>
    public void AssignAcademic(Guid academicId)
    {
        if (academicId == Guid.Empty) throw new ArgumentException("Academic ID cannot be empty", nameof(academicId));

        if (!_academicIds.Contains(academicId))
        {
            _academicIds.Add(academicId);
        }
    }

    /// <summary>
    /// Removes an academic from this room
    /// </summary>
    public void RemoveAcademic(Guid academicId)
    {
        _academicIds.Remove(academicId);
    }

    /// <summary>
    /// Business rule validation: Room is occupied by one or more Academic
    /// </summary>
    public void ValidateHasOccupants()
    {
        if (_academicIds.Count == 0)
        {
            throw new BusinessRuleViolationException("Room is occupied by one or more Academic");
        }
    }

    /// <summary>
    /// Creates a unique key for room number and building combination
    /// </summary>
    public string GetUniqueKey() => $"{BuildingId}_{RoomNr}";
}
