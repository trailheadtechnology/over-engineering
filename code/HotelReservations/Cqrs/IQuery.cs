namespace HotelReservations.Cqrs;

/// <summary>
/// Marker for read-side requests. Reads and writes are separated (CQRS) so each side can scale on its own.
/// </summary>
public interface IQuery<TResult>;
