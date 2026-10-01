namespace HotelReservations.Domain;

/// <summary>
/// Base class for every entity. The key type is generic so we can move to int, long,
/// or string keys later without touching the domain model.
/// </summary>
public abstract class Entity<TId> where TId : notnull, IEquatable<TId>
{
    public required TId Id { get; init; }
}
