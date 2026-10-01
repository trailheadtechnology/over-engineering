// HR-142: look up a reservation by confirmation code. That's the whole ticket.

var app = WebApplication.CreateBuilder(args).Build();

Reservation[] reservations =
[
    new("HX7Q2M", "Ada Lovelace", "101", new(2026, 10, 2), new(2026, 10, 5)),
    new("PL4K9A", "Grace Hopper", "204", new(2026, 10, 3), new(2026, 10, 4)),
    new("ZT8R1C", "Alan Turing", "312", new(2026, 10, 9), new(2026, 10, 12)),
];

app.MapGet("/reservations/{code}", (string code) =>
    reservations.FirstOrDefault(r => r.ConfirmationCode.Equals(code, StringComparison.OrdinalIgnoreCase))
        is { } reservation
            ? Results.Ok(reservation)
            : Results.NotFound());

app.Run();

record Reservation(string ConfirmationCode, string GuestName, string RoomNumber, DateOnly CheckIn, DateOnly CheckOut);
