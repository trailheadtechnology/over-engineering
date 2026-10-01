namespace HotelReservations.Cqrs;

/// <summary>
/// Routes each query to its handler, so callers never depend on handlers directly.
/// </summary>
public sealed class Mediator(IServiceProvider services) : IMediator
{
    public Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken ct)
    {
        var handlerType = typeof(IQueryHandler<,>).MakeGenericType(query.GetType(), typeof(TResult));
        var handler = services.GetRequiredService(handlerType);
        var handle = handlerType.GetMethod(nameof(IQueryHandler<IQuery<TResult>, TResult>.HandleAsync))!;
        return (Task<TResult>)handle.Invoke(handler, [query, ct])!;
    }
}
