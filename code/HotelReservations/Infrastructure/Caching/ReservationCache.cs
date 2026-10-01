using System.Collections.Concurrent;
using HotelReservations.Contracts;

namespace HotelReservations.Infrastructure.Caching;

/// <summary>
/// Read-through cache with a time-to-live and per-key locking, so a burst of lookups for
/// the same code only hits storage once (no cache stampede).
/// </summary>
public sealed class ReservationCache
{
    private static readonly TimeSpan TimeToLive = TimeSpan.FromMinutes(5);

    private readonly ConcurrentDictionary<string, (ReservationDto? Value, DateTimeOffset ExpiresAt)> _entries =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new(StringComparer.OrdinalIgnoreCase);
    private readonly IClock _clock = ServiceRegistry.Resolve<IClock>();

    public async Task<ReservationDto?> GetOrAddAsync(string key, Func<Task<ReservationDto?>> load)
    {
        if (TryGetFresh(key, out var cached))
            return cached;

        var gate = _locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            if (TryGetFresh(key, out cached))
                return cached;

            var value = await load();
            _entries[key] = (value, _clock.UtcNow + TimeToLive);
            return value;
        }
        finally
        {
            gate.Release();
        }
    }

    public void Invalidate(string key) => _entries.TryRemove(key, out _);

    private bool TryGetFresh(string key, out ReservationDto? value)
    {
        if (_entries.TryGetValue(key, out var entry) && entry.ExpiresAt > _clock.UtcNow)
        {
            value = entry.Value;
            return true;
        }

        value = null;
        return false;
    }
}
