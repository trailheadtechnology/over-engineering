namespace HotelReservations.Domain;

public sealed class Guest : Entity<Guid>
{
    public required string FullName { get; init; }
    public required string Email { get; init; }
}
