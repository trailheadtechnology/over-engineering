<!-- A saved run of the over-engineering-review skill against HotelReservations/, as a backup for the live demo.
     Two edits: the summary said 26 .cs files (there are 28) and 11 findings (it lists 12). -->

## Over-Engineering Review: HotelReservations (whole project; no diff against main)

**Requirement:** HR-142, "Look up a reservation by confirmation code." `GET /reservations/{code}`, exact match that ignores case, returns guest name, room, check-in and check-out, and returns 404 when nothing matches. Searching by guest name or partial code, exports and reporting are all **out of scope**. Source: `TICKET.md`.
**Scale:** 1 property (Maple Grove Inn, 42 rooms), about 6 front-desk clerks, about 150 reservations a month. Source: `TICKET.md:3-5`, `docs/adr/0001-single-database.md:7`.
**Decisions checked:** `docs/adr/0001-single-database.md` (one database, no sharding until more than 10 properties) and `docs/adr/0002-use-platform-defaults.md` (use built-in DI, `IMemoryCache`/`HybridCache`, `TimeProvider` and `Microsoft.Extensions.Resilience`, and add caching or retries only for a measured problem). The project has no AGENTS.md, CLAUDE.md or copilot-instructions file. I also ran `dotnet build` and got 16 OE analyzer warnings. I treated them as leads and confirmed each one in the code.

**Summary:** 12 findings across all 10 types. Following them removes most of the 28 `.cs` files and 8 layers. The request path currently has 9 hops (endpoint → controller → service → manager → mediator → handler → cache → retry → shard router/repository). Afterwards it has 2 (endpoint → repository).

| # | Type | Where | What's there | What was needed | Do this | Confidence |
|---|------|-------|--------------|-----------------|---------|------------|
| 1 | 8 Shiny Object Syndrome | `Cqrs/IMediator.cs:3`, `Cqrs/Mediator.cs:6-14`, `Cqrs/IQuery.cs:4-6`, `Cqrs/IQueryHandler.cs:3`, `Queries/FindReservationQuery.cs:6`, `Queries/FindReservationQueryHandler.cs:12-18`, `Program.cs:25-26` | CQRS with a reflection-based mediator (`MakeGenericType` + `MethodInfo.Invoke`) that serves exactly one query. The comment says it exists "so each side can scale on its own," but there is no write side. OE0002 and OE0003 fire on `IQuery`, `IQueryHandler` and `IMediator`. | One read: look up a code in a list. | **Remove.** Delete the `Cqrs/` folder, `FindReservationQuery` and the handler class. Move the matching line (exact, ignore case) into the repository or endpoint. That is 6 files and 2 layers (mediator, handler). | High |
| 2 | 7 Lasagna Architecture | `Controllers/ReservationController.cs:13-19`, `Services/ReservationService.cs:20-21`, `Managers/ReservationManager.cs:16-19`, `Endpoints/ReservationEndpoints.cs:13-14` | Endpoint → Controller → Service → Manager. The service only forwards (OE0007). The manager only forwards to the factory and mediator ("and (eventually) commands," line 14). The controller only does the null→404 mapping, which a one-line endpoint can do. | One minimal-API endpoint. | **Remove.** Delete the Controller, Service and Manager files and their interfaces. Do the 404 mapping in the `MapGet` lambda. That is 3 files and 3 layers. | High |
| 3 | 4 Over-Built Scalability | `Infrastructure/Sharding/ConsistentHashShardRouter.cs:6-38`, `Infrastructure/Sharding/IShardRouter.cs`, `Data/InMemoryReservationRepository.cs:8-22`, `Queries/FindReservationQueryHandler.cs:27-30,46-52` | A consistent-hash ring with SHA-256, 64 shards and 100 virtual nodes each (6,400 ring points, walked linearly on every lookup), holding 3 seeded reservations. | 150 reservations a month at one property. ADR 0001 says "No sharding, partitioning, or read replicas" until there are more than 10 properties. | **Remove.** This directly contradicts ADR 0001. Delete the `Sharding/` folder (2 files, 1 layer). Store the data as one list or dictionary, and replace `GetShardAsync` with `FindByCode`. | High |
| 4 | 1 Gold-Plating | `Queries/SearchMode.cs:3-8`, `Queries/FuzzyMatcher.cs:6-32`, `Queries/FindReservationQueryHandler.cs:32-41`, `Endpoints/ReservationEndpoints.cs:13-14` | `?mode=Prefix\|Fuzzy` with a hand-written Levenshtein matcher. | The ticket asks for an exact match, and "partial code" search is listed as **out of scope** (`TICKET.md:19`). | **Remove.** Delete `SearchMode`, `FuzzyMatcher` and the `mode` parameter (2 files). If clerks later need typo tolerance, add it then with a ticket behind it. | High |
| 5 | 1 Gold-Plating | `Endpoints/ReservationEndpoints.cs:16-18`, `Controllers/ReservationController.cs:21-22`, `Services/ReservationService.cs:23-32` | `GET /export?codes=` CSV export. It also hand-builds CSV without escaping. | "Exports and reporting" is listed as **out of scope** (`TICKET.md:20`). | **Remove** the endpoint and `ExportCsvAsync`. | High |
| 6 | 5 Premature Optimization | `Infrastructure/Caching/ReservationCache.cs:6-53`, `Queries/FindReservationQueryHandler.cs:21-23`, `Program.cs:21` | A read-through cache with a 5-minute TTL, per-key `SemaphoreSlim` "stampede" protection and a `SemaphoreSlim` that is never disposed. It caches `null` misses, and `Invalidate` is never called. A reservation booked right after a failed lookup therefore returns 404 for up to 5 minutes. | 6 clerks reading an in-memory list. There is no measurement, and ADR 0002 says to cache "only when a measured problem calls for them." | **Remove** (1 file, 1 layer). If a measured need ever shows up, use `HybridCache` or `IMemoryCache` as ADR 0002 says. | High |
| 7 | 9 Rolling Your Own | `Infrastructure/Resilience/RetryExecutor.cs:10-25` (OE0009), `Queries/FindReservationQueryHandler.cs:23` | A hand-written exponential-backoff retry that catches every `Exception`, wrapped around a lookup in an in-memory dictionary, which has no transient failures to retry. | Nothing. ADR 0002 names `Microsoft.Extensions.Resilience` and says to retry only when a measured problem calls for it. | **Remove** (1 file, 1 layer). Defer (YAGNI): when a real database exists and shows transient faults, add `Microsoft.Extensions.Resilience`. | High |
| 8 | 10 Misapplied Libraries/Frameworks | `Infrastructure/ServiceRegistry.cs:8-15`, `Infrastructure/IClock.cs:6-14`, `Program.cs:17`, `Infrastructure/Caching/ReservationCache.cs:17` | A static service locator running next to the built-in DI container, plus a custom `IClock`/`SystemClock` in place of `TimeProvider`. The only consumer of either is the cache. | ADR 0002 says to use built-in DI and `TimeProvider`. | **Remove** both files. They go away with finding 6. If a clock is needed later, inject `TimeProvider`. | High |
| 9 | 3 Over-Abstraction | `Data/IReservationRepository.cs:5-11`, plus single-implementation interfaces at `Controllers/ReservationController.cs:6`, `Services/ReservationService.cs:8`, `Managers/ReservationManager.cs:8`, `Factories/ReservationQueryFactory.cs:5` (OE0003 on all) | `IReservationRepository` exists "so we can swap the in-memory store for SQL Server, Cosmos DB, or anything else later." It has one implementation and no tests that use the seam. | One store. Swapping storage later is cheap. | **Remove** `IReservationRepository` (1 file) and use `InMemoryReservationRepository` directly. The other interfaces disappear along with findings 2 and 10. | High |
| 10 | 6 Overuse of Design Patterns | `Factories/ReservationQueryFactory.cs:13-15` (OE0006), `Factories/ReservationDtoFactory.cs:6-13` | One factory that only calls `new FindReservationQuery(...)`, behind its own interface and registered in DI ("one place to grow"). A second, DI-registered factory that maps 5 fields. | A constructor call and a 5-field mapping. | **Remove** `ReservationQueryFactory` (it goes with finding 1). **Inline** the DTO mapping into the lookup, or make it a static `ReservationDto.From(r)`. That is 2 files. | High |
| 11 | 2 OO Gymnastics | `Domain/Entity.cs:7-10` (OE0002); `Guest.cs:3`, `Room.cs:3`, `Reservation.cs:3` | A generic `Entity<TId>` base class "so we can move to int, long, or string keys later," used only as `Entity<Guid>`. No code ever reads `Id`. `Guest.Email` and `Room.Type` are never read either. | Code, guest name, room number and dates. | **Remove** the base class (1 file). Defer (YAGNI): add `Id` back when a database needs a key. | Medium |
| 12 | 1 Gold-Plating | `Endpoints/ReservationEndpoints.cs:10` | The route is `/api/v1/reservations/{code}`, which adds API versioning. | `GET /reservations/{code}` (`TICKET.md:12`). As written, the endpoint doesn't meet the acceptance criterion. | Map `/reservations/{code}` directly. | Medium |

### Simplest version that meets the requirement
```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<ReservationRepository>();
var app = builder.Build();

app.MapGet("/reservations/{code}", (string code, ReservationRepository repo) =>
    repo.Find(code) is { } r
        ? Results.Ok(new ReservationDto(r.ConfirmationCode, r.Guest.FullName, r.Room.Number, r.CheckIn, r.CheckOut))
        : Results.NotFound());
app.Run();

// Data/ReservationRepository.cs
public sealed class ReservationRepository
{
    private readonly Dictionary<string, Reservation> _byCode = new(StringComparer.OrdinalIgnoreCase) { /* seed */ };
    public Reservation? Find(string code) => _byCode.GetValueOrDefault(code);
}
```
What's left: `Program.cs`, `Data/ReservationRepository.cs`, `Contracts/ReservationDto.cs` and `Domain/{Reservation,Guest,Room}.cs` (or flatten those into one record).

### Kept on purpose
- **`ReservationDto`** keeps the API response separate from the domain model and returns exactly the fields the ticket lists. It's a reasonable boundary.
- **The in-memory store** is the simplest storage that works at this scale. ADR 0001 says to migrate to a real database when it's needed.
- **Case-insensitive matching** (`OrdinalIgnoreCase`) is required by `TICKET.md:13`.
- **The 404 on a miss** is required by `TICKET.md:15`.
- **`Guest` and `Room` as separate types** are harmless value holders that mirror the domain. Only their base class and unused fields are flagged.
- **`CancellationToken` parameters** are passed in by the framework at no cost. Once the async layers go, they can stay or go.
