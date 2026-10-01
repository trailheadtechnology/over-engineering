using HotelReservations.Domain;

namespace HotelReservations.Data;

/// <summary>
/// Storage abstraction so we can swap the in-memory store for SQL Server, Cosmos DB, or anything else later.
/// </summary>
public interface IReservationRepository
{
    Task<IReadOnlyList<Reservation>> GetShardAsync(int shard, CancellationToken ct);
}
