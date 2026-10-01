using HotelReservations.Queries;
using HotelReservations.Services;

namespace HotelReservations.Controllers;

public interface IReservationController
{
    Task<IResult> LookupAsync(string confirmationCode, SearchMode mode, CancellationToken ct);

    Task<IResult> ExportAsync(string confirmationCodes, CancellationToken ct);
}

public sealed class ReservationController(IReservationService service) : IReservationController
{
    public async Task<IResult> LookupAsync(string confirmationCode, SearchMode mode, CancellationToken ct)
    {
        var reservation = await service.FindAsync(confirmationCode, mode, ct);
        return reservation is null ? Results.NotFound() : Results.Ok(reservation);
    }

    public async Task<IResult> ExportAsync(string confirmationCodes, CancellationToken ct) =>
        Results.Text(await service.ExportCsvAsync(confirmationCodes.Split(','), ct), "text/csv");
}
