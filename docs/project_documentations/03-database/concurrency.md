# Concurrency & Write Rules

*How concurrent writers stay safe — per-room locks in memory, insert-once rows in SQL, and no optimistic concurrency anywhere.*

Parent document: [PRD](../00-product/PRD.md) · Related: [Schema & ERD](schema-and-erd.md) · [ADR-0013](../01-architecture/adr/0013-per-room-locking-no-optimistic-concurrency.md) · [ADR-0005](../01-architecture/adr/0005-in-memory-room-store.md) · Decision record: [Migrations & Indexes](migrations-and-indexes.md)

---

## 1. The stance (deliberately inverted vs the seniors)

The seniors solved concurrency with optimistic DB tokens (`xmin`). **We don't use them** ([ADR-0013](../01-architecture/adr/0013-per-room-locking-no-optimistic-concurrency.md)) — because our live state never lives in the database:

| Kind of state | Concurrency mechanism |
| --- | --- |
| Rooms, presence, invitations | **Per-room / per-invitation / per-user locks in memory** ([ADR-0014](../01-architecture/adr/0014-in-memory-state-trio.md)) — serialization, not retries |
| `Matches` rows | **Insert-once** — a row is inserted at game end and never updated, so nothing can conflict |

## 2. Write rules for `Matches`

| Rule | Meaning |
| --- | --- |
| **Insert-once, never updated** | A `Matches` row is created at finish (both players' rows in one transaction) and never modified afterwards; history and stats are reads |
| **One commit per game end** | The online finish writes both rows in a single `SaveChangesAsync` — the game is saved exactly once, regardless of which path finished it |
| **No retry loops in handlers** | A handler never "mutates optimistic and catches conflict"; the mutation runs under the room lock or fails validation as `OperationResult` |
| **No concurrency tokens on game state** | There are no columns to version — live state has no row to version ([ADR-0005](../01-architecture/adr/0005-in-memory-room-store.md)) |

## 3. Deletes (the two FK rules, applied safely)

| Scenario | What happens |
| --- | --- |
| Player deletes their account | Their `Matches` rows **cascade-delete** (`UserId` FK); their `OpponentUserId` references on **other players'** rows become **NULL** (set-null) — opponent history is never erased |
| Delete while in an active room | The delete flow **forfeits first** (inside the room lock, opponent's row written as a loss), then deletes rows — sequential, never interleaved |
| Orphaned room where both players left | Closed by cleanup **without saving any result** (FR-30) — no rows are written at all |

## 4. Why this is safe on one instance

A single deploy means one lock domain and one writer per table — the two preconditions (per-entity locks + insert-once rows) fully cover the ten race scenarios ([realtime-signalr](../01-architecture/realtime-signalr.md) §6). A second instance would break the lock domain; scaling out requires Redis-backed state first ([ADR-0013](../01-architecture/adr/0013-per-room-locking-no-optimistic-concurrency.md)).

## Assumptions

- "No retries in handlers" is a standing review rule — a `while (conflict) retry` loop in `Application` is flagged as a violation of [ADR-0013](../01-architecture/adr/0013-per-room-locking-no-optimistic-concurrency.md).
- EF `ConcurrencyStamp` on Identity tables is unused by app code (Identity handles it internally) — app rows (`Matches`) carry no concurrency token by design.
- If V2 introduces durable rooms or matchmaking, this file is revisited first — lock design and persistence change *together*.
