using HotelReservations.Contracts;
using HotelReservations.Cqrs;
using HotelReservations.Factories;
using HotelReservations.Queries;

namespace HotelReservations.Managers;

public interface IReservationManager
{
    Task<ReservationDto?> FindAsync(string confirmationCode, SearchMode mode, CancellationToken ct);
}

/// <summary>
/// Orchestrates reservation workflows across queries and (eventually) commands.
/// </summary>
public sealed class ReservationManager(IMediator mediator, IReservationQueryFactory queries) : IReservationManager
{
    public Task<ReservationDto?> FindAsync(string confirmationCode, SearchMode mode, CancellationToken ct) =>
        mediator.SendAsync(queries.Create(confirmationCode, mode), ct);
}
