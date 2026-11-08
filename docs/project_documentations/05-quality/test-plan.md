# Test Plan

*The concrete inventory: what the unit suite covers per use case, the manual walkthrough scripts that replace integration/E2E suites, and the milestone gates that decide "done".*

Parent document: [PRD](../00-product/PRD.md) · Related: [Test Strategy](test-strategy.md) · [Security](security.md) · [CQRS Use Cases](../02-backend/cqrs-use-cases.md) · [Real-time & SignalR](../01-architecture/realtime-signalr.md) · Decision records: [ADR-0008](../01-architecture/adr/0008-nunit-moq-shouldly-testing.md) · [ADR-0015](../01-architecture/adr/0015-vertical-slices-and-unit-only-tests.md)

---

## 1. Unit inventory (the automated floor)

Every row below is a green-unit-test obligation of the milestone it belongs to (roadmap M0–M13). "Fake time" = injected `TimeProvider`; "Substitute" = Moq `Verify`.

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

Each script below is **self-contained**: the steps, expected lobby banners/toasts, and server codes are inline. Run exactly as written in two browsers (A = inviter, B = invitee) — do not open other docs mid-run. Failure at any inline check = script fails, milestone is blocked. Results are recorded in `docs/project_documentations/walkthroughs/` (script id, date, browser, pass/fail) — an unrecorded script counts as not passed ([ADR-0008](../01-architecture/adr/0008-nunit-moq-shouldly-testing.md)).

### W-1 — Double-invite race (real-time §6 race 7)

1. A and B both logged in, see each other as Available. Simultaneously (count down 3-2-1) A invites B and B invites A.
2. **Expected:** exactly one invitation exists (either A→B or B→A wins). The losing invite gets a toast containing `already pending` (`Invite.AlreadyPending`); no deadlock, no second popup, no room created yet.
3. **Pass:** only the winner's target sees one popup; loser never sees a popup for its own losing invite.

### W-2 — Accept vs expiry vs Cancel (exactly-once)

1. Run A→B three times: (a) B accepts quickly, (b) B waits 31 s doing nothing, (c) A clicks Cancel while popup is open.
2. **Expected (a):** B clicks Accept → popup closes, B sees green `room-status` “Accepted! Room created. You are O…”, A sees green banner “Room {id}… ready — you are X…”, Waiting card hides on both, Invite buttons reset. Exactly one room exists.
3. **Expected (b):** countdown drains “Expires in 30s → … → 0s”, client auto-rejects; B sees “Invitation expired.” toast, A sees Waiting hide; no room created. A late Accept click after 0 s fails cleanly (no room).
4. **Expected (c):** A clicks Cancel → both see red `invite-status` “Invitation cancelled.”, popup closes on B, Waiting hides on A, no room created.
5. **Pass:** room exists **iff** Accept won; all three runs end with Waiting hidden on both sides.

### W-3 — Disconnect → reconnect inside 60 s (grace)

1. In a playing room (M11), disconnect B (close tab), then reopen + reconnect within 60 s.
2. **Expected:** A sees “B is reconnecting…” + countdown ring; on reconnect the banner hides, board resyncs from `GetRoomQuery`, play continues with no loss.

### W-4 — Disconnect past 60 s + cleanup (forfeit)

1. Disconnect B and do nothing for >60 s + 6 s (cleanup tick is 5 s).
2. **Expected:** A sees forfeit finish, `RoomClosed` “Opponent forfeited”, rows saved once, board frozen.

### W-5 — Presence flip as invite lands (race 10)

1. B closes tab at the exact moment A invites B (try 3 times).
2. **Expected:** no orphan invitation. If invite registered first, B shows popup then disconnect cancels it — A sees red “went offline. Invitation cancelled.” If disconnect won first, A gets toast `Player is offline.` (`Invite.Offline`) immediately. Either way Waiting is hidden.

### W-6 — Double-click move + two tabs

1. Double-click a cell, or open two tabs for same player and move in both.
2. **Expected:** second attempt returns `Game.NotYourTurn` / `Game.CellOccupied` (shake 300 ms + exact error toast); nothing broadcasts; board never double-applies.

### W-7 — Delete account while in game

1. Delete A mid-game (password re-entry). **Expected:** B sees forfeit; A’s rows cascade-delete; B’s rows keep history with opponent shown as “Deleted player”.

### W-8 — Register → login → logout → lockout

1. Register new user → success toast, presence connects. Logout via header `Logout (Name)` → redirect `/`. Login wrong password ×5 → 5th shows “Account locked. Try again in 1 minute.” with countdown, 6th still locked, after 60 s login works. **Pass:** unknown user and wrong password both show the same single generic error (never which field is wrong).

### W-9 — Online: invite → popup → accept → play → win → save → history (M11)

1. A→B, B accepts → both land on `/Rooms/{id}`, A = X. Play to a win, verify both players’ `/History` each show one new row (winner Win, loser Loss, same moves, one timestamp). Forged result (edit payload result) → `Game.ForgedResult`, no row written.

### W-10 — Decline cycle: reject → cooldown → expiry → offline-cancel

1. B rejects A’s invite. **Expected:** A Waiting hides, red banner “{B} declined… after 30s cooldown.” + toast; B popup closes, red banner “You declined…”.
2. A immediately re-invites B. **Expected:** toast `You can try inviting again in a moment.` (`Invite.Cooldown`). Wait 31 s, re-invite succeeds.
3. Fresh invite, do nothing 31 s. **Expected:** B popup closes with “Invitation expired.” toast; A Waiting hides (until M11 RoomCleanupService, timeout is client auto-reject → server marks Rejected, inviter copy says “declined”; the 30 s bar is the honest timer).
4. Fresh invite, B closes tab. **Expected:** A red banner “went offline. Invitation cancelled.”, Waiting hides, re-invite to B now fails as `Invite.Offline`.

### W-11 — Local + computer + forgery probe

1. Play full local game, then Easy/Medium/Hard vs computer. **Expected:** legal moves only, computer replies ≤500 ms with thinking spinner, stateless (no server state between calls). Submit a forged `SaveMatch` (altered result) → `Game.ForgedResult`, row not written. As guest, same games store score only in `localStorage` (nothing hits DB).

### W-12 — Change password + delete with active room

1. Change password with wrong current → `Auth.WrongPassword` under the field. Correct current → success + sign-out. Delete with active room → forfeit-first, then cascade; opponent’s history shows “Deleted player”.

### W-13 — Hub negotiate + cookie persistence

1. Log in, visit `/`, `/History`, `/Settings` — one presence entry stays (count does not double). Log out → hub negotiate on `/hubs/game` returns 401, all user pages redirect to Login. **Expected:** online count reflects connections, not pages; `Logout (Name)` is visible in header while signed in.

### 3.1 Edge catalog (categorization only — scripts above already contain all steps)

| Theme | Scenarios (map to script) | Invariant |
| --- | --- | --- |
| Guards | S1 Second invite = AlreadyPending (W-1); S2 Cooldown 30 s (W-10); S10 Third user = Busy | Server re-validates under lock; toast shows literal `Invite.*` message |
| Races | S8 Simultaneous A↔B (W-1); S9 Inviter offline before accept (W-5); double-click = single (W-2) | Exactly-once; monotonic terminal states; stuck Waiting = bug |
| Lifecycle | S4 31 s expiry auto-reject (W-2/W-10); S5 Cancel closes popup (W-2); S6 Disconnect mid-pending (W-5/W-10); S12 Refresh mid-pending = offline cancel | Waiting hides on **all** terminal states on both sides |
| Surfaces | S7 Popup on `/History`/`/Settings` (popup in `_Layout`); S11 `Esc` = Reject; S13 Guest = no hub, no Invite buttons | Hub is `[Authorize]`; guests see notice only, never a popup |

## 4. Milestone gates (M0 → M13)

| Milestone | Required evidence |
| --- | --- |
| **M0** spike | WebSockets alive on the host (`spikes/SignalRCheck` result recorded); stack pins committed |
| **M1** engine | U-1 + U-14 green; engine 100% line coverage; architecture tests present |
| **M2** AI | U-1 (AI legality + exhaustive Hard minimax sweep) green |
| **M3** local | U-2 green; W-11 passed |
| **M4** computer | U-2 green; W-11 passed |
| **M5** save & forgery | U-3 green; W-11 passed |
| **M6** history & stats | U-9 green; W-11 passed |
| **M7** accounts | W-8 passed |
| **M8** guests | W-11 (guest) passed |
| **M9** presence | U-12 (presence part) green; live list observed in two browsers |
| **M10** invitations | U-4, U-5, U-6, U-12 green; W-1, W-2, W-5, W-10 passed |
| **M11** rooms | U-7, U-8, U-11, U-12, U-13 green; W-3, W-4, W-6, W-7, W-9 passed |
| **M12** account extras | U-10 green; W-12 passed |
| **M13** verification sweep | Every script (W-1…W-13) re-run against the final build; roadmap M13 exit criteria checked item by item |

## Assumptions

- The unit inventory is closed for v1: a new use case adds its row here *and* its tests in the same change.
- The concurrency walkthroughs (W-1…W-7) and the walkthrough scripts (W-8…W-13) are versioned with this file; results are recorded in `docs/project_documentations/walkthroughs/` (script id, date, browser, pass/fail) — a failed script blocks its milestone gate; there is no "run it later" state.
- Real-server behaviour that cannot be unit-tested (cookie redirects, hub negotiate, error-copy rendering) is *only* covered by the scripts; an uncovered behaviour is a doc gap to record, not a test to skip silently.
