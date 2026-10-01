# HR-142: Look up a reservation by confirmation code

**Requested by:** Front desk, Maple Grove Inn (one property, 42 rooms)
**Users:** about 6 front-desk clerks
**Volume:** about 150 reservations a month

As a front-desk clerk, I want to type a guest's confirmation code and see their reservation,
so I can check them in faster.

## Acceptance criteria

- `GET /reservations/{code}`
- Exact match on the confirmation code (codes are case-insensitive)
- Returns guest name, room number, check-in date, and check-out date
- Returns 404 when no reservation matches

## Out of scope

- Searching by guest name or partial code
- Exports and reporting
