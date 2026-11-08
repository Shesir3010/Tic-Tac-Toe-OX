# Test Plan

*The concrete inventory: what the unit suite covers per use case, the manual walkthrough scripts that replace integration/E2E suites, and the milestone gates that decide "done".*

Parent document: [PRD](../00-product/PRD.md) · Related: [Test Strategy](test-strategy.md) · [Security](security.md) · [CQRS Use Cases](../02-backend/cqrs-use-cases.md) · [Real-time & SignalR](../01-architecture/realtime-signalr.md) · Decision records: [ADR-0008](../01-architecture/adr/0008-nunit-moq-shouldly-testing.md) · [ADR-0015](../01-architecture/adr/0015-vertical-slices-and-unit-only-tests.md)

---

## 1. Unit inventory (the automated floor)

Every row below is a green-unit-test obligation of the milestone it belongs to (roadmap M0–M7). "Fake time" = injected `TimeProvider`; "Substitute" = Moq `Verify`.

| # | Use case / area | Tests (happy + unhappy) |
| --- | --- | --- |
| U-1 | Engine (E1–E6) | All 8 win lines `[TestCase]`; bounds/occupancy/turn/abuse guards each with its exact failure; E5 immutability; E6 determinism; AI legality + exhaustive Hard minimax at-least-draw |
| U-2 | `PlayMoveCommand` | Legality → `Game.*` codes; correct DTO (board/turn/result) |
| U-3 | `SaveMatchCommand` | Honest result saved once; tampered result → `Game.ForgedResult`, no row written |
| U-4 | `SendInvitationCommand` | All five guards (`Invite.Self/Offline/Busy/Cooldown/AlreadyPending`); one outgoing per player |
| U-5 | `RespondToInvitationCommand` | Accept creates the room exactly once (inviter = X); reject notifies + starts 30 s cooldown; terminal-state late accept fails cleanly |
| U-6 | `CancelInvitationCommand` + `GetOnlinePlayersQuery` | Only-inviter cancel; status derivation table (Available/Invited/In game) |
| U-7 | `MakeOnlineMoveCommand` | Turn/legality guards; winning move saves both rows **once** in one commit |
| U-8 | `LeaveRoomCommand` + `RequestRematchCommand` | Forfeit semantics; both-ready rematch rule; board reset |
| U-9 | `GetRoomQuery` + `GetHistoryQuery` + `GetStatsQuery` | Participant guard; history/stats projections from `Matches` |
| U-10 | `ChangePasswordCommand` + `DeleteAccountCommand` | Wrong-password code; delete-in-active-room forfeits first, then cascades |
| U-11 | `InMemoryRoomStore` | Two writers, one room → exactly one coherent winner (per-room lock); user→room index |
| U-12 | `InvitationManager` + `PresenceTracker` | TTL fires once (fake time); cooldown re-arms; disconnect-at-zero flips presence + cancels invitations |
| U-13 | `RoomCleanupService` | Forfeit sweep for `Playing` rooms past grace; orphaned rooms close without saves; stale invitations expire |
| U-14 | Architecture tests | R-A1…R-A5 (`Domain` package-free; `Application` never sees SignalR; `R-A2` engine purity) |
| U-15 | `OperationResult` catalog | Factories; literal message-per-code; HTTP mapping (§5.1 of `error-handling.md`) in one switch |

## 2. Concurrency walkthroughs (the manual half of the race contract)

Unit tests prove the *decisions*; these scripts prove the *real-server manifestation* (hub + two browsers) — [realtime-signalr.md](../01-architecture/realtime-signalr.md) §6 lists all 10 races:

| # | Walkthrough | Pass criteria |
| --- | --- | --- |
| W-1 | Double-invite simultaneously (A→B and B→A) | Exactly one invitation exists; the other fails with "already pending"; no deadlocks |
| W-2 | Accept vs 30 s expiry vs Cancel (three runs) | Exactly-once: room exists iff accept won; late accept fails cleanly |
| W-3 | Disconnect → reconnect inside 60 s | Grace cancels; board resyncs from `GetRoomQuery`; play continues |
| W-4 | Disconnect past 60 s, then cleanup pass | Forfeit finish; rows saved once; `RoomClosed` to the connected player |
| W-5 | Presence flip as an invite lands | No orphan invitation; the losing side gets the offline notice |
| W-6 | Double-click move + two tabs | Second attempt returns `Game.NotYourTurn`/`CellOccupied`; nothing broadcasts |
| W-7 | Delete account while in a game | Opponent sees the forfeit; deleting player's rows cascade; opponent's rows keep their history (set-null) |

## 3. Manual walkthrough scripts (the E2E replacements)

Each script is executed at every milestone gate and during the M7 sweep; **results are recorded (script id, date, pass/fail) — an unrecorded script counts as not passed** ([ADR-0008](../01-architecture/adr/0008-nunit-moq-shouldly-testing.md)):

| # | Script | Covers |
| --- | --- | --- |
| W-8 | Register → login → logout → lockout (5 wrong passwords) | FR-01…FR-05, lockout §3.1 security |
| W-9 | Online: invite → popup → accept → play → win → save → history shows both rows | FR-32…FR-36, FR-26 |
| W-10 | Invite→reject→cooldown→expiry→offline-cancel (full decline cycle) | FR-35 + `error-handling.md` |
| W-11 | Local game + computer (Easy/Medium/Hard) + `SaveMatch` forgery probe | FR-02…FR-17 |
| W-12 | Account: change password (wrong current), delete with active room | FR-19…FR-21 + W-7 |
| W-13 | Hub negotiate + cookie persistence across pages; unauthorized access redirects | §8 security checklist items 1, 3, 6, 7 |

## 4. Milestone gates (M0 → M7)

| Milestone | Required evidence |
| --- | --- |
| **M0** spike | WebSockets alive on the host (`spikes/SignalRCheck` result recorded); stack pins committed |
| **M1** engine | U-1 + U-14 green; engine 100% line coverage; architecture tests present |
| **M2** offline | U-2, U-3, U-9 green; W-11 passed |
| **M3** online | U-4…U-8, U-11…U-13 green; W-1…W-7 passed |
| **M4** auth/polish | U-10 green; W-8, W-12, W-13 passed; security checklist §8 green |
| **M5** hardening | All rows of §8 checklist re-verified after hardening changes |
| **M6** pre-release | Full unit suite green; W-8…W-13 re-run after final config review |
| **M7** verification sweep | Every script re-run against the final build; roadmap M7 exit criteria checked item by item |

## Assumptions

- The unit inventory is closed for v1: a new use case adds its row here *and* its tests in the same change.
- The concurrency walkthroughs (W-1…W-7) and the walkthrough scripts (W-8…W-13) are versioned with this file; a failed script blocks its milestone gate — there is no "run it later" state.
- Real-server behaviour that cannot be unit-tested (cookie redirects, hub negotiate, error-copy rendering) is *only* covered by the scripts; an uncovered behaviour is a doc gap to record, not a test to skip silently.
