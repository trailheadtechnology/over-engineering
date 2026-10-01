using HotelReservations.Domain;
using HotelReservations.Infrastructure.Sharding;

namespace HotelReservations.Data;

public sealed class InMemoryReservationRepository : IReservationRepository
{
    private readonly Dictionary<int, List<Reservation>> _shards = [];

    public InMemoryReservationRepository(IShardRouter router)
    {
        foreach (var reservation in Seed())
        {
            var shard = router.GetShard(reservation.ConfirmationCode);
            if (!_shards.TryGetValue(shard, out var list))
                _shards[shard] = list = [];
            list.Add(reservation);
        }
    }

    public Task<IReadOnlyList<Reservation>> GetShardAsync(int shard, CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Reservation>>(_shards.TryGetValue(shard, out var list) ? list : []);

    private static IEnumerable<Reservation> Seed()
    {
        yield return Create("HX7Q2M", "Ada Lovelace", "101", new DateOnly(2026, 10, 2), new DateOnly(2026, 10, 5));
        yield return Create("PL4K9A", "Grace Hopper", "204", new DateOnly(2026, 10, 3), new DateOnly(2026, 10, 4));
        yield return Create("ZT8R1C", "Alan Turing", "312", new DateOnly(2026, 10, 9), new DateOnly(2026, 10, 12));
    }

    private static Reservation Create(string code, string guest, string room, DateOnly checkIn, DateOnly checkOut) => new()
    {
        Id = Guid.NewGuid(),
        ConfirmationCode = code,
        Guest = new Guest { Id = Guid.NewGuid(), FullName = guest, Email = $"{guest.Split(' ')[0].ToLowerInvariant()}@example.com" },
        Room = new Room { Id = Guid.NewGuid(), Number = room, Type = "King" },
        CheckIn = checkIn,
        CheckOut = checkOut,
    };
}
