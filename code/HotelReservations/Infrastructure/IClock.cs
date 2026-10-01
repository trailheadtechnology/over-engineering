namespace HotelReservations.Infrastructure;

/// <summary>
/// Abstracts the system clock so time-dependent code is testable.
/// </summary>
public interface IClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
