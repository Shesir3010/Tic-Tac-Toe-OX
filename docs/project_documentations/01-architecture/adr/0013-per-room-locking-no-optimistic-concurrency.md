# ADR-0013: Per-Room Locking — No Optimistic Concurrency on Game State

*Game and room state serialize through per-entity locks in memory; SQL rows are written once or never updated concurrently — so DB-level optimistic concurrency (rowversion/xmin) is deliberately not used for state correctness.*

**Status:** Accepted (2026-11-07, consolidating the lock design from [ADR-0005](0005-in-memory-room-store.md)) · **Inverted relative to:** Senior suite ADR-0006 (they chose `xmin` optimistic concurrency) · **Referenced by:** [`../realtime-signalr.md`](../realtime-signalr.md) §5–§6 · `../../02-backend/cqrs-use-cases.md` (future unit)

## Context

Two sources of concurrent access exist:

1. **Game-state correctness** — simultaneous invites/moves/forfeits against the *same* room must resolve to exactly one winner ([`../realtime-signalr.md`](../realtime-signalr.md) §6 race table).
2. **SQL persistence** — `Matches` rows are **insert-once** per finished game (FR-26's two rows, one transaction) and never updated afterwards; totals are computed, never stored ([ADR-0002](0002-sql-server-with-ef-core.md)).

The seniors' project used PostgreSQL row `xmin` optimistic concurrency: read a row, mutate on a later transaction, fail-and-retry on version conflict. Their writes were frequent (rooms as DB rows).

## Decision

1. **Conflict resolution lives in the mutexes, not in retry loops.** Every mutation of a room, presence record, or invitation runs inside its **per-entity lock** (`InMemoryRoomStore` per-room locks; `PresenceTracker` per-user; `InvitationManager` per-invitation-pair) — the winner is decided by *ordering of lock acquisition*, always producing a coherent state ([ADR-0014](0014-in-memory-state-trio.md)).
2. **No optimistic concurrency tokens on game/room state.** There are no columns to compute a version from — this state lives in memory by design ([ADR-0005](0005-in-memory-room-store.md)); DB versioning them would mean moving room rows into SQL, which is the option [ADR-0005](0005-in-memory-room-store.md) rejected.
3. **No retry loops in handlers.** Handlers never "mutate optimistic and catch conflict"; the mutation function itself either runs clean under the lock or fails validation (`OperationResult`, [ADR-0010](0010-result-pattern-expected-failures.md)). This eliminates the retry-exposed-to-UI problem the seniors' shape must handle in every client.
4. **For `Matches`:** rows are insert-only within one transaction per game-end; the only multi-row scenario is the online finish writing **both** players' rows in a single `SaveChangesAsync` ([FR-26](../../00-product/PRD.md)). No two writes ever target the same row concurrently → nothing to version-conflict against.
5. **Account deletion vs an active room:** the delete flow first runs the leave-forfeit path for the deleting player's active room (inside the room lock, so the opponent's row is written with a result), then performs the cascade delete — sequential, no interleave ([ADR-0004](0004-identity-with-cookie-auth.md) FR-19 semantics).

## Consequences

**Positive:** lock-based serializability is simpler to reason about and to test (fake-time unit tests, no retry coverage needed in handlers); zero DB round-trips for state contention; the race table's ten rows each map to a deterministic ordering outcome.

**Negative:** accepts the single-instance constraint *again*, this time from the concurrency side — scaling out would need a distributed lock or Redis-backed state; if a second instance ever ran, two per-room locks would be independent and unsafe — coupling between [ADR-0005](0005-in-memory-room-store.md) and this ADR must be respected in deploys.

## Alternatives considered

| Alternative | Why rejected (for v1) |
| --- | --- |
| Optimistic concurrency (`rowversion`/`xmin`) on DB rows for rooms | Requires rooms as DB rows (rejected in [ADR-0005](0005-in-memory-room-store.md)); pushes retry UX to every client |
| Pessimistic DB locks (`SELECT … FOR UPDATE`) | Holds transaction open per mutation; pins hot rows; pays DB round-trip latency per move (p95 < 500 ms budget) |
| Optimistic in-memory CAS (e.g. atomic version counters) | More complex than a mutex for 9-cell state; weaker guarantees for multi-step transitions (finish + save) |
| A single global lock | Serializes *all* rooms behind one mutex — an unnecessary hotspot ([ADR-0005](0005-in-memory-room-store.md) rejected it) |

## Links

- The lock ordering + broadcast timing: [`../realtime-signalr.md`](../realtime-signalr.md) §5
- The state containers: [ADR-0014](0014-in-memory-state-trio.md) · [ADR-0005](0005-in-memory-room-store.md)
- Failure vocabulary used when a mutation *is* invalid: [ADR-0010](0010-result-pattern-expected-failures.md)

## Assumptions

- "No retries in handlers" is a standing review rule — a `while (conflict) retry` loop appearing anywhere in `Application` is flagged at review as a violation of this ADR.
- If V2 introduces durable rooms or matchmaking, this ADR is revisited first — the lock design and the persistence choice change *together*, not separately.
