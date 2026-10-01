namespace HotelReservations.Cqrs;

public interface IMediator
{
    Task<TResult> SendAsync<TResult>(IQuery<TResult> query, CancellationToken ct);
}
