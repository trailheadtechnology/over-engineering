using HotelReservations.Contracts;
using HotelReservations.Cqrs;

namespace HotelReservations.Queries;

public sealed record FindReservationQuery(string ConfirmationCode, SearchMode Mode) : IQuery<ReservationDto?>;
