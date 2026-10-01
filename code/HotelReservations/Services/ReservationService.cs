using System.Text;
using HotelReservations.Contracts;
using HotelReservations.Managers;
using HotelReservations.Queries;

namespace HotelReservations.Services;

public interface IReservationService
{
    Task<ReservationDto?> FindAsync(string confirmationCode, SearchMode mode, CancellationToken ct);

    Task<string> ExportCsvAsync(IEnumerable<string> confirmationCodes, CancellationToken ct);
}

/// <summary>
/// Business logic layer for reservations.
/// </summary>
public sealed class ReservationService(IReservationManager manager) : IReservationService
{
    public Task<ReservationDto?> FindAsync(string confirmationCode, SearchMode mode, CancellationToken ct) =>
        manager.FindAsync(confirmationCode, mode, ct);

    public async Task<string> ExportCsvAsync(IEnumerable<string> confirmationCodes, CancellationToken ct)
    {
        var csv = new StringBuilder("ConfirmationCode,Guest,Room,CheckIn,CheckOut\n");
        foreach (var code in confirmationCodes)
        {
            if (await manager.FindAsync(code, SearchMode.Exact, ct) is { } r)
                csv.AppendLine($"{r.ConfirmationCode},{r.GuestName},{r.RoomNumber},{r.CheckIn:yyyy-MM-dd},{r.CheckOut:yyyy-MM-dd}");
        }
        return csv.ToString();
    }
}
