# ADR 0001: One database, no sharding

**Status:** Accepted

## Context

We build software for one property: Maple Grove Inn, 42 rooms, about 150 reservations a month.
Even 100x growth fits comfortably in a single database on modest hardware.

## Decision

Store all reservations in one database. No sharding, partitioning, or read replicas.
We'll revisit if we take on more than 10 properties.

## Consequences

- Queries stay simple, and every reservation is one lookup away.
- If we ever outgrow one database, we'll migrate then, with real load data to guide the design.
