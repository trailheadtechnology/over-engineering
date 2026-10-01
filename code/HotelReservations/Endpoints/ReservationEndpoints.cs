using HotelReservations.Controllers;
using HotelReservations.Queries;

namespace HotelReservations.Endpoints;

public static class ReservationEndpoints
{
    public static IEndpointRouteBuilder MapReservationEndpoints(this IEndpointRouteBuilder app)
    {
        var reservations = app.MapGroup("/api/v1/reservations");

        reservations.MapGet("/{confirmationCode}",
            (string confirmationCode, SearchMode? mode, IReservationController controller, CancellationToken ct) =>
                controller.LookupAsync(confirmationCode, mode ?? SearchMode.Exact, ct));

        reservations.MapGet("/export",
            (string codes, IReservationController controller, CancellationToken ct) =>
                controller.ExportAsync(codes, ct));

        return app;
    }
}
