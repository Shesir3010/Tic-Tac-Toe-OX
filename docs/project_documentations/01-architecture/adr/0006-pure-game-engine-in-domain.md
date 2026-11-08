# ADR-0006: Pure Game Engine in the Domain

*The correctness core: a zero-dependency, functionally pure engine that owns every rule, replayed server-side to defeat forged results.*

**Status:** Accepted (2026-11-06) · **Referenced by:** `../../00-product/PRD.md` (FR-02…FR-06, FR-09), `../system-architecture.md` R-A2, ADR-0001 · unit tests → `../../05-quality/test-plan.md`

## Context

FR-02/03/04/05 demand server-authoritative rules: server-side alternation and legality checks on every submission path, immutable finished boards, an Easy/Medium/Hard AI contract, and validation that can be *replayed* (`SaveMatchCommand` re-runs a saved move list to detect forgery). The v1 shape must also survive a JS client, a hub, a JSON endpoint, a cleanup service, and future engines (V2 item 4) — with the rules written **once**.

## Decision

1. The engine lives in `Domain/Engine/` and is **pure C#: value objects (`Board`, `Player`, `GameResult`) + static rules (`Rules.ValidateMove/ApplyMove/Evaluate`)** — no I/O, no framework types, no NuGet packages (R-A2 in [`../system-architecture.md`](../system-architecture.md)).
2. Every mutation path (JSON endpoint, hub commands, cleanup forfeits) validates through the **same** engine functions; no controller, hub, or JS module re-implements a rule.
3. `SaveMatchCommand` verifies a claimed result by re-running the engine over the submitted move list ([FR-15](../../00-product/PRD.md), story US-06 unhappy path); a mismatch = `Game.ForgedResult`, no row written.
4. AI sits beside the engine in `Engine/Ai/` behind `IComputerPlayer { Player Mark; int ChooseMove(Board) }` with `IRandomSource` injected ([ADR-0006, R-A4](../system-architecture.md)): Easy uses the random source; Medium is deterministic win-take/block/center/corner; Hard is exhaustive minimax over reachable states — always at least a draw.
5. `Board` moves are **functional** (`Move(i, p)` returns a new board); no engine object mutates steadily mid-column.

## Invariants (each an independent engine test)

| ID | Law |
| --- | --- |
| E1 | A move is legal ⇔ index ∈ 0–8 ∧ cell empty ∧ game unfinished ∧ mover's symbol matched turn |
| E2 | X moves first; alternation is strict; no double-move by one symbol |
| E3 | Exactly 8 win lines: rows `[0-2] [3-5] [6-8]`, columns `[0,3,6] [1,4,7] [2,5,8]`, diagonals `[0,4,8] [2,4,6]` |
| E4 | Draw ⇔ board full ∧ no winning line |
| E5 | A finished game is immutable: `Move` throws / rejects after result |
| E6 | `Evaluate` is deterministic; same state ⇒ same result forever |

The engine must reach **100% line coverage** ([PRD §7](../../00-product/PRD.md)) — gate in [M1](../../00-product/roadmap.md).

## Consequences

**Positive:** a single truth for rules never drifting between page and server; the AI harness is per-unit-of-work (no async, no clocks); forged saves reject; a second game plugs behind the same contract when it arrives; the engine can be verified exhaustively.

**Negative:** tiny allocation churn (per-move object copies) — irrelevant at 9 cells and 17 moves; adding a new kind of rule requires work in *this* file, not elsewhere.

## Alternatives considered

| Alternative | Why rejected |
| --- | --- |
| Client-side authoritative rules | Directly violates FR-02/25 and opens trivial forgery; legacy from un-authoritative drafts |
| An abstract "board game" engine framework up front | YAGNI at 3×3 until V2 item 4 actually starts; the current shape refactors cleanly to `IGameEngine` later |
| Mutable engine object game state | Harder to reason about/history; functional style maps to move-lists already needed for [FR-15](../../00-product/PRD.md) |
| Rules inside Infrastructure (DB-triggered or service-layer shims) | Rules would no-longer be dependency-free or pure; world's simplest test loop lost |

## Links

- Engine contract surface: `../../02-backend/cqrs-use-cases.md` (PlayMove + validation) 
- AI details: `../system-architecture.md` §3 layout · `../../05-quality/test-plan.md` (sweep)

## Assumptions

- The engine's public API (`Rules`, `Board`) is v1-stable; a V2 multi-game abstraction is an explicit refactor with its own ADR — never a drift-in change.
- All randomness (Easy AI) is injected as `IRandomSource`, so tests seed determinism without touching production behavior.
