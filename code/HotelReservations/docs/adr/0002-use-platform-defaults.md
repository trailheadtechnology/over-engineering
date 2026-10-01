# ADR 0002: Use what ASP.NET Core already gives us

**Status:** Accepted

## Context

ASP.NET Core ships with dependency injection, configuration, logging, caching (`IMemoryCache`,
`HybridCache`), a testable clock (`TimeProvider`), and resilience (`Microsoft.Extensions.Resilience`).
Every custom replacement is code we have to test, document, and maintain.

## Decision

Use the built-in features. Don't write our own version of something the platform or a
well-known library already does. Add caching or retries only when a measured problem calls for them.

## Consequences

- New developers already know how everything is wired.
- When we need something the platform doesn't do, we write an ADR first.
