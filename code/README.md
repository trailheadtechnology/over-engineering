# Over-Engineering Demo

One small ticket, built two ways, plus two tools that catch the difference:

- **Roslyn analyzers** catch the *structural* smells, on every build.
- **An AI skill** ([`.claude/skills/over-engineering-review`](../.claude/skills/over-engineering-review/SKILL.md)) catches the
  *intent* smells, the ones you can only see by reading the ticket and the team's decisions.

## The ticket

[`HotelReservations/TICKET.md`](HotelReservations/TICKET.md): a 42-room inn wants its 6 front-desk clerks to look
up a reservation by confirmation code. One endpoint, exact match, 404 if not found.

| Project | Files | Lines | Analyzer warnings |
|---|---|---|---|
| `HotelReservations/`: how it got built | 28 | 459 | 16 |
| `HotelReservations.Simple/`: what the ticket needed | 1 | 15 | 0 |

Both return the same JSON.

## The analyzers

Diagnostic IDs match the over-engineering type numbers from the talk.

| ID | Type | Flags | Default |
|---|---|---|---|
| OE0002 | 2. OO Gymnastics | A generic type only ever used with one type argument (`Entity<TId>` that's always `Entity<Guid>`) | Info |
| OE0003 | 3. Over-Abstraction | An interface with exactly one implementation | Info |
| OE0006 | 6. Overuse of Design Patterns | A `*Factory` method that only passes its arguments to `new` | Info |
| OE0007 | 7. Lasagna Architecture | A method that only forwards its arguments to another method | Info |
| OE0009 | 9. Rolling Your Own | A loop that catches exceptions and calls `Task.Delay`/`Thread.Sleep` (a hand-rolled retry) | Info |

They're all judgment calls, so they ship as Info. Each sample project's `.editorconfig` raises them to warnings.
.NET also ships related rules you can turn on: CA1501 (excessive inheritance), CA1502 (excessive complexity),
and CA1506 (excessive class coupling).

Types 1, 4, 5, 8, and 10 don't get analyzers, because they depend on what was asked for and how big the
system is. That's the skill's job.

## Layout

- `OverEngineering.Analyzers/`: the analyzers (netstandard2.0)
- `OverEngineering.Analyzers.Tests/`: xUnit tests (`dotnet test`)
- `HotelReservations/`: the over-engineered version, with `TICKET.md` and `docs/adr/`
- `HotelReservations.Simple/`: the version the ticket asked for (`TreatWarningsAsErrors` is on)

## Running it

```bash
dotnet build HotelReservations            # 16 warnings
dotnet build HotelReservations.Simple     # 0 warnings
dotnet test OverEngineering.Analyzers.Tests
dotnet run --project HotelReservations    # GET /api/v1/reservations/HX7Q2M
dotnet run --project HotelReservations.Simple   # GET /reservations/HX7Q2M
```

In VS Code, open this `code` folder with the C# Dev Kit extension. `.vscode/settings.json` turns on
full-solution analysis, which OE0002 and OE0003 need because they look at the whole project.

## Installing the skill

The skill is a standard `SKILL.md` ([Agent Skills format](https://agentskills.io)), so it isn't tied to one tool.
It lives in this repo at `.claude/skills/over-engineering-review/`, so **Claude Code and GitHub Copilot in VS Code
pick it up automatically** when you open the repo.

To use it in your own projects, copy the `over-engineering-review` folder into your tool's skills folder:

| Tool | Project folder | Personal folder |
|---|---|---|
| GitHub Copilot (VS Code) | `.github/skills/`, `.agents/skills/`, or `.claude/skills/` | `~/.copilot/skills/` or `~/.agents/skills/` |
| Claude Code | `.claude/skills/` | `~/.claude/skills/` |
| Other agents | See your tool's docs for its skills folder | |

No skills support? Paste the contents of `SKILL.md` into any AI chat, followed by your code and ticket.

Then ask: *"Review this project for over-engineering."*

## Demo script

1. Open `HotelReservations/TICKET.md`: one endpoint, exact match, 6 users, exports out of scope.
2. Show the `HotelReservations` folder tree: 28 files for that ticket.
3. `dotnet build HotelReservations`: walk the 16 warnings by type number (2, 3, 6, 7, 9).
4. Open `OverEngineering.Analyzers/PassThroughMethodAnalyzer.cs`: under 60 lines for the whole rule.
5. Ask the room: what *can't* an analyzer know? (Was the fuzzy search requested? Do 6 clerks need 64 shards?)
6. Start your AI agent **inside `HotelReservations/`** and ask it to review the project for over-engineering.
   It reads the ticket and ADRs and finds the types the analyzers can't. Claude Code finds the repo's skill from
   any subfolder (check with `/skills`, or invoke it with `/over-engineering-review`). For other tools, if the skill
   doesn't load, tell the agent to follow `../../.claude/skills/over-engineering-review/SKILL.md`.
   `sample-review.md` is a saved run, if the live one wanders.
7. Open `HotelReservations.Simple/Program.cs`: 15 lines. `dotnet build HotelReservations.Simple`: 0 warnings.
8. Optional: ask the agent to apply its own findings and compare the diff.

## Answer key (spoilers)

Every type from the talk is planted in `HotelReservations`. **A** = an analyzer catches it; **S** = the skill.

| Type | Where | Caught by |
|---|---|---|
| 1. Gold-Plating | `SearchMode` Prefix/Fuzzy + `FuzzyMatcher`; the CSV export endpoint (the ticket lists both as out of scope); an `/api/v1/` route the ticket didn't ask for | S |
| 2. OO Gymnastics | `Entity<TId>` always `Entity<Guid>`; `IQuery<T>` and `IQueryHandler<,>` with one use each | A (OE0002) |
| 3. Over-Abstraction | 10 interfaces with one implementation each, e.g. `IReservationRepository` "so we can swap later" | A (OE0003) |
| 4. Over-Built Scalability | `ConsistentHashShardRouter`: 64 shards × 100 virtual nodes for 150 reservations a month. ADR 0001 says one database | S |
| 5. Premature Optimization | `ReservationCache` with cache-stampede locking, for 6 users and an in-memory list. ADR 0002 says measure first. Bonus bug: it caches misses, so a new booking can 404 for 5 minutes | S |
| 6. Overuse of Design Patterns | `ReservationQueryFactory`; a mediator with one handler | A (OE0006) + S |
| 7. Lasagna Architecture | Endpoint → Controller → Service → Manager → Mediator → Handler → Repository: 6 hops | A (OE0007) + S |
| 8. Shiny Object Syndrome | CQRS with a hand-rolled mediator for a single read | S |
| 9. Rolling Your Own | `RetryExecutor` retrying an in-memory read; also the cache and the mediator | A (OE0009) + S |
| 10. Misapplied Libraries/Frameworks | `ServiceRegistry` service locator next to ASP.NET Core DI; `IClock` instead of `TimeProvider`. ADR 0002 says use the platform | S |
