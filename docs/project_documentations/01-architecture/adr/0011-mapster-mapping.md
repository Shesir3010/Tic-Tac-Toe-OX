# ADR-0011: Mapping with Mapster

*Domain types and presentational DTOs stay separate, and every translation between them is an explicit, reviewed Mapster profile — no magic, no hand-mapped drift.*

**Status:** Accepted (2026-11-06 from D-16, recorded standalone 2026-11-07) · **Referenced by:** [`../system-architecture.md`](../system-architecture.md) §5.3 · [`../adr/0007-cortex-mediator-mapster-serilog.md`](0007-cortex-mediator-mapster-serilog.md) · mirrors Senior suite ADR-0010 · `../../05-quality/test-plan.md` (config-compile test)

## Context

Every request/response boundary turns types: `Match` entities → `MatchHistoryItem` DTOs for the History page; `Room` + engine state → `RoomState` for hub payloads; command inputs (form models) → command objects. Options were AutoMapper (convention magic), Mapster (explicit profiles, performant), or hand-written mapping in controllers.

## Decision

1. **Mapster 10 + Mapster.DependencyInjection** is the only mapper. All translation lives in **`IRegister` profiles** under `Application/Mapping/` (one profile per feature area: `GameMapping`, `RoomsMapping`, `MatchesMapping`, `AccountMapping`).
2. **Never** map in Domain; **never** hand-write field-by-field copies in controllers, the hub, or views — a boundary type change shows up as a profile change in exactly one place.
3. Profiles are wired via `AddInfrastructure()`'s Mapster registration (all DI lives in Infrastructure; consumers inject `IMapper`).
4. **One unit test compiles the entire configuration** (`TypeAdapterConfig.Compile()`) on every CI run — a renamed entity property or DTO member fails at test time, not at 11 pm in the browser ([ADR-0008](0008-nunit-moq-shouldly-testing.md) suite).
5. Where a mapping needs runtime services (e.g. the current user id for a stats projection), Mapster.DependencyInjection supplies constructor dependencies — no service-locator inside expressions.

## Consequences

**Positive:** explicit, reviewable mapping — the diff for "add a column to History" touches one profile; compile-checkable against renames; faster than AutoMapper reflection-chains; removes hand-mapping busywork from controllers.

**Negative:** a small surface of "learn this tool" exists; Mapster's implicit flattening must be kept *explicit* (we configure mappings rather than relying on auto-conventions), so profiles are slightly longer — accepted, aligning with the boring-and-clear principle of [ADR-0007](0007-cortex-mediator-mapster-serilog.md).

## Alternatives considered

| Alternative | Why rejected |
| --- | --- |
| AutoMapper | Self-configuring conventions hide DTO drift until runtime; already the same reasoning as [ADR-0007](0007-cortex-mediator-mapster-serilog.md) |
| Hand-written `MapTo()` extension methods per type | Duplicates structure in code, requires per-file manual updates, invites missed properties — the exact drift the compile test prevents |
| Map straight in Razor views / hub payloads | Spreads rules from one feature across the entire Web layer; untestable |
| Manual records + factory statics on DTOs | Only *looks* simpler per-type; no central review point, no compile-checked coverage |

## Links

- Where the profiles live: [`../system-architecture.md`](../system-architecture.md) §3 solution tree (`Application/Mapping/`)
- Dispatch pipeline these serve: [`../adr/0007-cortex-mediator-mapster-serilog.md`](0007-cortex-mediator-mapster-serilog.md)

## Assumptions

- The compile unit test is the *only* guard line for mapping coverage — if a DTO is used somewhere but has no profile entry, the database-architect review (05-quality unit) flags it manually; Mapster can't prove exhaustive coverage alone.
- Mapster remains MIT and compatible with .NET 10 for the v1/V2 lifecycle ([ADR-0012](0012-pinned-stable-stack.md) governs upgrades).
