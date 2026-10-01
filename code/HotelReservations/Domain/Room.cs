namespace HotelReservations.Domain;

public sealed class Room : Entity<Guid>
{
    public required string Number { get; init; }
    public required string Type { get; init; }
}
