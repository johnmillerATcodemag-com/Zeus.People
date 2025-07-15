namespace Zeus.People.Domain.Entities;

/// <summary>
/// Base interface for all entities
/// </summary>
public interface IEntity
{
    /// <summary>
    /// Unique identifier for the entity
    /// </summary>
    Guid Id { get; }
}

/// <summary>
/// Base interface for entities with typed identifiers
/// </summary>
/// <typeparam name="TId">Type of the identifier</typeparam>
public interface IEntity<out TId> : IEntity
{
    /// <summary>
    /// Typed unique identifier for the entity
    /// </summary>
    new TId Id { get; }
}
