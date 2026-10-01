---
name: over-engineering-review
description: Review code for over-engineering, meaning complexity the requirements don't justify. Checks code against the ticket and the team's architecture decisions (ADRs), maps each finding to one of 10 over-engineering types, and recommends what to delete or defer. Use when asked to review a change, PR, branch, or project for over-engineering, unnecessary complexity, gold-plating, YAGNI, or "is this too much?"; or after an AI agent generates a large change.
---

# Over-Engineering Review

Find code that is more complex than what was actually asked for, and say what to delete.

Static analyzers can catch *structural* smells (an interface with one implementation, a method that only
forwards). They can't tell whether a feature was requested or whether a cache was needed. Your job is the
judgment part: compare the code to the **intent** (the requirement and the team's decisions).

This is a read-only review. Don't change code unless the user asks you to.

## 1. Find out what was actually needed (do this first)

Look for, in this order:

1. **The requirement.** A ticket, issue, or PR description the user gave you. Otherwise look for files such
   as `TICKET.md`, `REQUIREMENTS.md`, or a PR or issue description in the repo. Note the acceptance criteria and
   anything marked **out of scope**.
2. **Scale facts.** Users, data volume, request rates, number of tenants or sites. Over-engineering is
   relative to scale, so write these down.
3. **Decisions.** Architecture Decision Records (look in `docs/adr/`, `doc/adr/`, `adr/`, `docs/decisions/`) and
   agent instruction files (`AGENTS.md`, `CLAUDE.md`, `.github/copilot-instructions.md`). A decision can make a
   complex thing justified, or make it a violation.

If you can't find a requirement, ask the user for it in one short question. If they don't have one, continue
and label every finding "not checked against requirements."

## 2. Decide the scope

- If the user named files or folders, review those.
- Otherwise, if there are uncommitted or branch changes in git, review the diff against the default branch.
- Otherwise, review the project in the current folder.

## 3. Use structural signals if they exist

If the project has static analysis for structural smells (for example, analyzer warnings with IDs like
`OE0003`), build it and collect the warnings. Treat them as leads. Confirm each one in the code, and don't just
repeat the warning. Spend your effort on what analyzers can't see.

## 4. Check each type

For each type, look for the signals, then answer the question. Only report what you can back with evidence.

| # | Type | Signals | Question |
|---|------|---------|----------|
| 1 | Gold-Plating | Endpoints, options, modes, flags, exports not in the requirement | Where in the requirement is this? Is it listed as out of scope? |
| 2 | OO Gymnastics | Generics with one type argument, base classes with one subclass, deep inheritance | Does anything use the flexibility? |
| 3 | Over-Abstraction | Interfaces with one implementation, wrappers around libraries "so we can swap later" | Is there a second implementation, or a test that needs the seam? Swapping a library later is cheap (especially with AI help); isolate it, don't wrap it. |
| 4 | Over-Built Scalability | Sharding, partitioning, queues, microservices, distributed caches | Is the design more than 1-2 orders of magnitude beyond the stated scale? |
| 5 | Premature Optimization | Caching, pooling, parallelism, custom hashing or data structures | Is there a measurement that shows the problem? |
| 6 | Overuse of Design Patterns | Factories that only call `new`, mediators or strategies with one handler, builders for small objects | What would break if you called the constructor or method directly? |
| 7 | Lasagna Architecture | Layers that only forward calls, Controller to Service to Manager to Repository chains | Count the hops from entry point to data. Which layers add behavior? |
| 8 | Shiny Object Syndrome | CQRS, event sourcing, a mediator, a new framework or paradigm for a simple need | Would a plain approach meet the requirement? |
| 9 | Rolling Your Own | Hand-written retry, caching, DI, auth, logging, serialization, scheduling | Does the platform or a well-known library already do this? |
| 10 | Misapplied Libraries/Frameworks | Working around the framework: a service locator next to built-in DI, a custom clock instead of `TimeProvider`, sync-over-async | Is the code fighting the tools it already has? |

## 5. Rules for findings

- **Evidence or it doesn't count.** Every finding cites `file:line` and either the requirement it exceeds or
  the decision it contradicts. Without evidence, leave it out.
- **Respect decisions.** If an ADR or the requirement justifies something, don't flag it. List it under
  "Kept on purpose."
- **Simplify by subtracting.** Prefer delete over inline over simplify. Never suggest a new abstraction as the
  fix.
- **Separate "remove" from "defer."** Some things may be needed someday. Say "defer (YAGNI): add it when it's
  needed," and remember that adding later has never been cheaper.
- **Don't flag** tests, code the framework requires, interfaces with real second implementations, or
  boundaries the requirement calls for.
- **Be calibrated.** Give each finding High, Medium, or Low confidence.
- **Count the cost.** Estimate what the change removes: files, types, layers, and dependencies.

## 6. Report

Use this format:

```
## Over-Engineering Review: <scope>

**Requirement:** <one line, with source>
**Scale:** <users / volume / sites, with source>
**Decisions checked:** <ADRs and instruction files you read>

**Summary:** <N> findings across <K> types. Following them removes about <files> files and <layers> layers.

| # | Type | Where | What's there | What was needed | Do this | Confidence |
|---|------|-------|--------------|-----------------|---------|------------|

### Simplest version that meets the requirement
<A short sketch, at most 20 lines, of what the code could be.>

### Kept on purpose
<Anything that looked complex but is justified, and why.>
```

Order findings by how much complexity they remove, biggest first.
