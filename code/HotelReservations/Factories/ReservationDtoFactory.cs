using HotelReservations.Contracts;
using HotelReservations.Domain;

namespace HotelReservations.Factories;

public sealed class ReservationDtoFactory
{
    public ReservationDto Create(Reservation reservation) => new(
        reservation.ConfirmationCode,
        reservation.Guest.FullName,
        reservation.Room.Number,
        reservation.CheckIn,
        reservation.CheckOut);
}
