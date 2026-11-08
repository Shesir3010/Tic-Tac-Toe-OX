# ADR-0014: The In-Memory State Trio (Rooms · Presence · Invitations)

*All live online-play state lives in three Singleton containers in `Infrastructure/Services`, each with a per-entity lock and fake-time-testable timing — the technical home of presence and invitations alongside the room store.*

**Status:** Accepted (2026-11-07) · **Extends:** [ADR-0005](0005-in-memory-room-store.md) (rooms), records the containers behind [ADR-0009](0009-invitations-replace-room-codes.md) (invitations) · Mirrors Senior suite ADR-0008 (their in-memory presence tracker) · **Referenced by:** [`../realtime-signalr.md`](../realtime-signalr.md) · `../../02-backend/room-invitation-lifecycle.md` (future unit)

## Context

Online play needs three kinds of "right now" state: who is in what room, who is connected to the site, and which invitations are pending/waiting/cooldown. All three share the same properties: tiny, fast-changing, single-instance ([ADR-0005](0005-in-memory-room-store.md)), and never worth a database round-trip.

## Decision

### The three containers (all `Infrastructure/Services/`, all Singleton, all interface-backed)

| Container | Holds | Key rules |
| --- | --- | --- |
| `InMemoryRoomStore : IRoomStore` | Active rooms (`Room` models from `Domain/Online/`), user→room index | Created only on invitation acceptance · per-room lock · cleanup-owned closing ([ADR-0005](0005-in-memory-room-store.md), [ADR-0009](0009-invitations-replace-room-codes.md)) |
| `PresenceTracker : IPresenceTracker` | `userId → connectionCount` ('online' = ≥ 1) | Incremented on hub connect, decremented on disconnect; reaching 0 flips the player offline *immediately* (no grace) and triggers pending-invitation cancellation ([FR-35](../../00-product/PRD.md)) |
| `InvitationManager : IInvitationManager` | Pending/terminal invitations, per-player outgoing slot, per-pair cooldown timestamps | One outgoing per inviter · guards re-evaluated under the manager lock at creation · ~30 s TTL via `TimeProvider` · accept = invitation-terminal + room creation in **one locked flow** ([ADR-0009](0009-invitations-replace-room-codes.md)) |

### Shared policies (identical across the trio)

1. **Per-entity locks**, never a global mutex — rooms, users, and invitation pairs are independent contention domains ([ADR-0013](0013-per-room-locking-no-optimistic-concurrency.md)).
2. **All clocks injected** (`TimeProvider`): grace countdowns, invitation TTL, cooldown durations read the injected clock so unit tests travel through time deterministically.
3. **All durations read from the `RoomSettings` options** (grace seconds `60`, invitation TTL `30`, cooldown seconds `30`, cleanup interval `5`): changing a timing is a config edit, not a code change.
4. **`RoomCleanupService` (the Hosted service) owns all timeout sweeps** — every ~5 s: forfeit Playing rooms past their grace deadline (both rows saved once, per the forfeit rule), close orphaned rooms (both players gone past grace, **no result saved**), expire stale invitations past TTL. Per-iteration try/catch so one bad entity never stalls the loop.
5. **Domain models stay pure** — the `Room`/`Invitation` types in `Domain/Online/` carry no locks, no I/O, no SignalR: the containers own all lifecycles; the domain declares the state machines.
6. **Restart semantics:** restart wipes presence + invitations + active rooms; finished `Match`es persist in SQL ([FR-15](../../00-product/PRD.md)). Players returning after a restart find no board or open invite — the lobby is the only recovery surface.

## Consequences

**Positive:** each container is unit-testable in isolation with mocked dependencies + fake clock (per [ADR-0008](0008-nunit-moq-shouldly-testing.md)'s one-suite model); per-entity locks give predictable latency (~µs hold times, no cross-room contention); timings live in one config singleton; presence/invitation memory footprint is bytes per connected user.

**Negative:** all three containers must respect the single-instance deployment the same way (they are the reason improved instances cannot share state); cross-container operations (e.g. *accept invite* which uses the InvitationManager + creates into the RoomStore) run under both related locks in a documented order — **invitation lock acquired first, room lock second** — so deadlock-free ordering is reviewable (no other cross-container sequences exist in v1).

## Alternatives considered

| Alternative | Why rejected (for v1) |
| --- | --- |
| Presence/invitations in SQL | A write per connect/disconnect/invite for state that matters for ~30 seconds; adds no correctness the containers lack ([ADR-0005](0005-in-memory-room-store.md) trade-off logic) |
| Redis-backed trio | Second infra dependency, removes the free-plan-simplicity the project leans on; future seam if presence must survive restarts or scale out |
| One combined "OnlineStateService" | Rejected: three different per-entity lock granularities in one class blurs the review story; three focused containers with typed contracts stay unit-testable ([ADR-0013](0013-per-room-locking-no-optimistic-concurrency.md)) |
| Lazy expiry only (no cleanup service) | Expired invitations/graces would rely on "next time someone looks" — the forfeit path for `_DisconnectedUntilUtc` deadlines must run even when nobody connected is watching |

## Links

- Product decision behind invitations: [ADR-0009](0009-invitations-replace-room-codes.md)
- Hub contract consuming the trio: [`../realtime-signalr.md`](../realtime-signalr.md)
- Lock discipline: [ADR-0013](0013-per-room-locking-no-optimistic-concurrency.md)
- Lifecycle diagrams (future unit): `../../02-backend/room-invitation-lifecycle.md`

## Assumptions

- Presence is counted by *hub connections*, and multiple tabs per user are supported (connectionCount > 1 keeps the player online even after closing one tab) — the natural reading of "logged in with the site open" ([ADR-0009](0009-invitations-replace-room-codes.md)).
- The lock-acquisition order "invitation before room" applies only to the accept flow; no other cross-container lock pair is allowed without amending this ADR.
- The three containers never reference each other directly; commands orchestrate cross-container flows through `IMediator` handlers, keeping dispatch reviewable in one place.
