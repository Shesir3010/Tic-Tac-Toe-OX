# ADR-0005: In-Memory Room Store with Per-Room Locking

*Why active rooms live only in process memory, and how per-room lock objects deliver the serialized-transaction behavior the discipline demands.*

**Status:** Accepted (2026-11-06) · **Amended 2026-11-07:** [ADR-0009](0009-invitations-replace-room-codes.md) removed room codes and created rooms at invitation acceptance — no Waiting rooms, no idle expiry; the in-memory + per-room-lock design below is unchanged. · **Referenced by:** `../realtime-signalr.md` · `../../02-backend/room-invitation-lifecycle.md` · `../../00-product/PRD.md` (FR-25…FR-30)

## Context

Online play requires: unique short codes among *active* rooms, one-active-room-per-player, a join race resolved to exactly one winner ([FR-24](../../00-product/PRD.md)), near-instant move applications (p95 < 500 ms — [PRD NFRs](../../00-product/PRD.md) §6), and ~10-minute idle expiry with ~60-second disconnect grace. Rooms are a transient concept in v1: the host plan is free-tier (256 MB RAM) with **one instance** and no message bus; SQL Server retention is only meaningful for *finished* matches ([FR-15/26](../../00-product/PRD.md)).

## Decision

1. **Active rooms live only in memory** inside the Singleton `InMemoryRoomStore : IRoomStore` — `CreateAsync/JoinAsync/GetAsync/ApplyAsync/TryCloseExpiredAsync` (.NET framework dict built from `ConcurrentDictionary`).
2. **Per-room lock objects** serialize every mutation. Requests read a room, then **Apply** it under that room's private lock — validation, mutation, commit ordering inside; broadcasting after release ([../realtime-signalr.md](../realtime-signalr.md) §5). The lock granularity = one room, so 100 rooms never contend.
3. **Cleanup loop:** hosted `RoomCleanupService` ticks every 5 s to (a) ~~close Waiting rooms idle > 10 min~~ *(removed by [ADR-0009](0009-invitations-replace-room-codes.md) — no Waiting rooms exist)*, (b) forfeit Playing rooms past their deadline, (c) close orphaned rooms (both players gone past grace) **without saving a result**.
4. ~~**Codes:** 5 characters from `23456789ABCDEFGHJKMNPQRSTUVWXYZ`; generated chunks re-randomize on collision with an active code; a Released-closed code becomes reusable.~~ *(Removed 2026-11-07 by [ADR-0009](0009-invitations-replace-room-codes.md) — rooms receive internal opaque ids for URLs, never user-facing.)*
5. **User → room index** in the store preserves the one-active-game rule ([FR-30 amended](../../00-product/PRD.md)); it now mirrors what the presence layer reports as "In game"; the cleanup service is still the only closing path.

## Consequences

**Positive:** per-room locking is semantically strong (simple reasoning; each row of the race table maps to an inner-lock flow), fastest possible room mutations with zero I/O, no persistence effort for rooms at all.

**Negative:** **a restart ends every in-progress game** — acceptable because finished matches are SQL-persisted ([FR-15/26](../../00-product/PRD.md)), the release sees graceful reconnects but not multi-minute outages; room state can't survive a swap to two instances — this choice enforces single-instance forever in v1.

**Neutral:** ~200 B per room (board + a few ids and timestamps) ⇒ ≥ 100 rooms ≈ 20 KB — negligible; scaling out is easier *after* rooms move behind the `IRoomStore` interface (e.g. a durable Redis-backed implementation) without changing any commands.

## Alternatives considered

| Alternative | Why rejected (for v1) |
| --- | --- |
| Rooms as DB rows | Adds write I/O to every move; mutation caching still needed to hit p95; the free DB has no real room-state need — rows can't express a 60-s grace countdown cleanly |
| Redis-backed store | Second infra dependency with no v1 utility; the deployment/runtime cost outweighs the durability gain; clean swap later if evidence demands |
| DB rows + in-memory cache hybrid | The worst of both: two truth sources to keep consistent, plus the trade-off still nowhere insight |
| Global lock (one `SemaphoreSlim`) | Simple, but joins/wins across rooms form a serialization hotspot — rejected for latency headroom |
| Optimistic concurrency (retry on conflict) | Races then depend on retry/reconciliation; per-room serialization is simpler to reason about and testable with fake clocks ([../realtime-signalr.md](../realtime-signalr.md) §6 test 4) — expanded in [ADR-0013](0013-per-room-locking-no-optimistic-concurrency.md) |

## Links

- Lock ordering and broadcast timing: [`../realtime-signalr.md`](../realtime-signalr.md) §5
- Why locks, not optimistic concurrency: [ADR-0013](0013-per-room-locking-no-optimistic-concurrency.md)
- State-container discipline (rooms · presence · invitations): [ADR-0014](0014-in-memory-state-trio.md)
- Room state machine & transition rules R1–R5: [`../../00-product/PRD.md`](../../00-product/PRD.md) §4.3
- Lifecycle detail: [`../../02-backend/room-invitation-lifecycle.md`](../../02-backend/room-invitation-lifecycle.md)

## Assumptions

- "In-process" implies SACD (single-app deployment accepted): load-balancing to more than one instance is out of v1 scope *by construction* — the docs treat that constraint, not merely the implementation.
- Room model purity (no I/O in `Online/Room.cs`) is a hard invariant; the store owns all side effects, so that testing expiry uses a fake `TimeProvider` cleanly.
