# Backend (ASP.NET Core MVC)

*The backend: Cortex commands/queries, errors, room/invitation lifecycles, and configuration — everything the Application + Infrastructure layers need, in one place.*

Parent document: [PRD](../00-product/PRD.md) · Architecture: [System architecture](../01-architecture/system-architecture.md) · [Clean Architecture & DDD](../01-architecture/clean-architecture-ddd.md) · [CQRS & Mediator](../01-architecture/cqrs-and-mediator.md)

---

## Files in this folder

| File | About |
| --- | --- |
| [cqrs-use-cases.md](cqrs-use-cases.md) | Every command/query: inputs, guards, returns, errors |
| [error-handling.md](error-handling.md) | The exact error messages + status table + exception net |
| [room-invitation-lifecycle.md](room-invitation-lifecycle.md) | Room/invitation life story: states, timers, cleanup, locks |
| [settings-and-configuration.md](settings-and-configuration.md) | `appsettings` keys, env overrides, cookie + secrets rules |

## The hub contract lives in `01-architecture/`

The real-time contract (13 events, invocations, race table) is [01-architecture/realtime-signalr.md](../01-architecture/realtime-signalr.md) — kept there as the single source of truth, deliberately not duplicated here.

## Architecture & decisions (01-architecture)

| Topic | Doc |
| --- | --- |
| Layers, projects, folder layout | [01-architecture/clean-architecture-ddd.md](../01-architecture/clean-architecture-ddd.md) |
| Hub-as-gateway, rooms in memory, engine purity | [ADR-0003](../01-architecture/adr/0003-signalr-hub-as-command-gateway.md) · [ADR-0005](../01-architecture/adr/0005-in-memory-room-store.md) · [ADR-0006](../01-architecture/adr/0006-pure-game-engine-in-domain.md) |
| Invitations, presence, locks, results, slices | [ADR-0009](../01-architecture/adr/0009-invitations-replace-room-codes.md) · [ADR-0013](../01-architecture/adr/0013-per-room-locking-no-optimistic-concurrency.md) · [ADR-0014](../01-architecture/adr/0014-in-memory-state-trio.md) · [ADR-0010](../01-architecture/adr/0010-result-pattern-expected-failures.md) · [ADR-0015](../01-architecture/adr/0015-vertical-slices-and-unit-only-tests.md) |

## Assumptions

- This folder is the backend contract detail; the *why* behind each decision lives in `01-architecture/adr/`.
- Data layer detail (EF Core mapping, migrations, schema) is the next unit (`03-database/`) — tables in this folder never define schema.
