namespace HotelReservations.Domain;

public sealed class Reservation : Entity<Guid>
{
    public required string ConfirmationCode { get; init; }
    public required Guest Guest { get; init; }
    public required Room Room { get; init; }
    public required DateOnly CheckIn { get; init; }
    public required DateOnly CheckOut { get; init; }
}
