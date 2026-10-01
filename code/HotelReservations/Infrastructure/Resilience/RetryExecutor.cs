namespace HotelReservations.Infrastructure.Resilience;

/// <summary>
/// Retries transient failures with exponential backoff.
/// </summary>
public sealed class RetryExecutor
{
    private const int MaxAttempts = 3;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken ct)
    {
        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await action();
            }
            catch (Exception) when (attempt < MaxAttempts)
            {
                await Task.Delay(TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempt)), ct);
            }
        }

        throw new InvalidOperationException("Retry loop exited without a result.");
    }
}
