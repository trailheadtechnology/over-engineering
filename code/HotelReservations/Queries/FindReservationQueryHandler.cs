using HotelReservations.Contracts;
using HotelReservations.Cqrs;
using HotelReservations.Data;
using HotelReservations.Domain;
using HotelReservations.Factories;
using HotelReservations.Infrastructure.Caching;
using HotelReservations.Infrastructure.Resilience;
using HotelReservations.Infrastructure.Sharding;

namespace HotelReservations.Queries;

public sealed class FindReservationQueryHandler(
    IReservationRepository repository,
    IShardRouter router,
    ReservationCache cache,
    RetryExecutor retry,
    ReservationDtoFactory dtoFactory)
    : IQueryHandler<FindReservationQuery, ReservationDto?>
{
    public Task<ReservationDto?> HandleAsync(FindReservationQuery query, CancellationToken ct) =>
        cache.GetOrAddAsync(
            $"{query.Mode}:{query.ConfirmationCode}",
            () => retry.ExecuteAsync(() => FindAsync(query, ct), ct));

    private async Task<ReservationDto?> FindAsync(FindReservationQuery query, CancellationToken ct)
    {
        // Exact matches go straight to one shard; prefix and fuzzy searches have to scan them all
        var candidates = query.Mode == SearchMode.Exact
            ? await repository.GetShardAsync(router.GetShard(query.ConfirmationCode), ct)
            : await GetAllShardsAsync(ct);

        var match = query.Mode switch
        {
            SearchMode.Exact => candidates.FirstOrDefault(r =>
                r.ConfirmationCode.Equals(query.ConfirmationCode, StringComparison.OrdinalIgnoreCase)),
            SearchMode.Prefix => candidates.FirstOrDefault(r =>
                r.ConfirmationCode.StartsWith(query.ConfirmationCode, StringComparison.OrdinalIgnoreCase)),
            _ => candidates
                .Where(r => FuzzyMatcher.Distance(r.ConfirmationCode, query.ConfirmationCode) <= FuzzyMatcher.MaxDistance)
                .MinBy(r => FuzzyMatcher.Distance(r.ConfirmationCode, query.ConfirmationCode)),
        };

        return match is null ? null : dtoFactory.Create(match);
    }

    private async Task<IReadOnlyList<Reservation>> GetAllShardsAsync(CancellationToken ct)
    {
        var all = new List<Reservation>();
        for (var shard = 0; shard < router.ShardCount; shard++)
            all.AddRange(await repository.GetShardAsync(shard, ct));
        return all;
    }
}
