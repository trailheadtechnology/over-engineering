namespace HotelReservations.Contracts;

public sealed record ReservationDto(
    string ConfirmationCode,
    string GuestName,
    string RoomNumber,
    DateOnly CheckIn,
    DateOnly CheckOut);
