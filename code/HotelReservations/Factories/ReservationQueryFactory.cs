using HotelReservations.Queries;

namespace HotelReservations.Factories;

public interface IReservationQueryFactory
{
    FindReservationQuery Create(string confirmationCode, SearchMode mode);
}

/// <summary>
/// Centralizes query construction so creation logic has one place to grow.
/// </summary>
public sealed class ReservationQueryFactory : IReservationQueryFactory
{
    public FindReservationQuery Create(string confirmationCode, SearchMode mode) => new(confirmationCode, mode);
}
