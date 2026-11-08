# TASKS.md — Milestone Checklists

*Every task, tick by tick, in the order it gets built. **Tick a box ✅ only after the evidence is recorded** (build green, tests green, or the owner clicked it in the browser). Nothing is "done" until proven — milestone claims are claims, not proof.*

*Milestones mirror [`00-product/roadmap.md`](00-product/roadmap.md) M0 → M13. **Working agreement:** one milestone at a time; the next milestone opens only when the current one's boxes are all ticked. **Quality gate (every milestone):** `dotnet build` zero warnings → all unit tests green → walkthrough script(s) passed and recorded → human clicked it.*

---

## M0 — Spike: host + solution skeleton

- [ ] Create `TicTacToe.slnx` and the 4 projects:
  - [ ] `src/TicTacToe.Domain` — **zero references** (verify by solution build)
  - [ ] `src/TicTacToe.Application` — references **Domain only**
  - [ ] `src/TicTacToe.Infrastructure` — references **Application + Domain**
  - [ ] `src/TicTacToe.Web` (ASP.NET Core MVC, .NET 10) — references **Application** + the one Infrastructure DI call only
- [ ] Dependency rule enforced by project references (Web → Application → Domain ← Infrastructure, never outward)
- [ ] `test/TicTacToe.Application.UnitTests/` — the **only** test project (NUnit + Moq + Shouldly + AutoMock), plus `ArchitectureTests.cs` (R-A1…R-A5)
- [ ] Folder tree per `01-architecture/clean-architecture-ddd.md` §3: `Features/{Area}/{Feature}/Command|Query`, `Contracts/`, `Mapping/`, `Settings/`, `Data/`, `Identity/`, `Services/`, `Controllers/`, `Hubs/`, `Middleware/`, `wwwroot/{css,js,lib}`, `test/TicTacToe.Application.UnitTests/` skeleton
- [ ] `Directory.Packages.props` — pinned: Cortex.Mediator 3.1.2, Mapster 10.0.14, Serilog 10.0.0, EF Core/Identity 10.0.x (any other package = decision entry, [ADR-0012](01-architecture/adr/0012-pinned-stable-stack.md))
- [ ] `Program.cs` composition root: `AddApplication()` + `AddInfrastructure()` chained; `ValidateScopes` in Development
- [ ] Serilog configured (console + rolling file `logs/`, request logging); **never** log passwords/tokens/connection strings/full emails
- [ ] `.github/workflows/dotnet.yml` → restore → build (warnings as errors) → `dotnet test` — green on ($) dotnet build zero warnings, CI first PR green
- [ ] Identity cookie wired in `Program.cs` (`IdentityConstants.ApplicationScheme`); SQL Server **Express** connection via user-secrets only (never committed); CSS/Javascript folders exist under `wwwroot/`
- [ ] `spikes/SignalRCheck/` SignalR/websockets check — **result recorded**, then throwaway deleted
- [ ] EF migration plumbing: `AddInfrastructure(IConfiguration)` + initial migration applies green on Express via `dotnet ef database update`
- [ ] Owner checklist: app opens on the host plan; walkthrough M0 evidence recorded

**Gate:** `dotnet build` zero warnings · CI green on first PR · pins in `Directory.Packages.props` · spike negotiates SignalR on the host plan.

## M1 — Engine core

- [ ] `Domain/Engine/`: `Board`, `Player`, `Rules` (Validate/Apply/Evaluate), `GameResult`
- [ ] E1 legality (bounds, occupancy, turn) — tests
- [ ] E2 strict X-first alternation — tests
- [ ] E3 all 8 win lines — `[TestCase]` per line, both symbols
- [ ] E4 draw detection — test
- [ ] E5 finished immutability — tests
- [ ] E6 determinism — tests
- [ ] **100% line coverage** on the engine (hard gate)
- [ ] Architecture tests prove `Domain` references nothing

## M2 — AI (Easy → Medium → Hard)

- [ ] Easy: seeded `IRandomSource`, legal move only
- [ ] Medium: takes an immediate win → blocks an immediate loss → center → corner → any
- [ ] Hard: exhaustive minimax (memoized), prefers fastest win
- [ ] Hard **never loses** — exhaustive sweep proves it
- [ ] Reply lands ≤ 500 ms total incl. artificial 300–500 ms "thinking" delay; board non-interactive meanwhile

## M3 — Local pass-and-play

- [ ] `Game/Web` page renders server `RoomState`-equivalent board (stateless: browser sends board + move)
- [ ] `PlayMoveCommand` + `POST /api/game/move` (server validates via engine)
- [ ] Winning-line highlight + status text (game state, not just code)
- [ ] Reset (keeps scoreboard) + New match (zeroes scoreboard)
- [ ] Session scoreboard (X / Draws / O) tracked incl. guests
- [ ] Handler tests green (unit)
- [ ] ☑ Owner clicked a full 2-player game

## M4 — Computer play

- [ ] Mode/difficulty selector (Easy / Medium / Hard) + factory (`IComputerPlayerFactory`)
- [ ] Computer reply applied inside the same server call (stateless board round-trip)
- [ ] Board disabled during thinking + thinking status shown
- [ ] Stateless statelessness proven: no server state is kept between requests
- [ ] ☑ Owner played all 3 difficulties

## M5 — Save & forgery proof

- [ ] `Matches` table + migrations (Mode, Difficulty, Symbol, Result, Moves, UTC)
- [ ] `SaveMatchCommand` — **server replays** the move list, verifies legality + result
- [ ] Forged/tampered result → `Game.ForgedResult`, row **not** written
- [ ] Honest save writes exactly one row (insert-once)
- [ ] W-11 passed (forgery probe)

## M6 — History & stats pages

- [ ] `GetHistoryQuery` (paged, newest first) + `GetStatsQuery` (W/L/D computed, never a stored counter)
- [ ] History page with opponent names (or "**Deleted player**")
- [ ] Stats chips (Played / Won / Lost / Win %)
- [ ] U-9 green

## M7 — Accounts

- [ ] Register (unique username + email, inline duplicate errors)
- [ ] Login (username-or-email; **one generic error** for unknown user or wrong password)
- [ ] Logout (`[Authorize]` on all user surfaces incl. the hub)
- [ ] Lockout after 5 failures → `Auth.LockedOut`
- [ ] W-8 passed

## M8 — Guests

- [ ] Guest plays local/computer (browser only)
- [ ] Nothing a guest does touches the database
- [ ] Guest notice shown in UI (session score only)

## M9 — Presence

- [ ] `GameHub` as command gateway (`[Authorize]`, cookie) — authenticates, dispatches, broadcasts, **zero rules**
- [ ] `PresenceTracker` singleton (userId → connectionCount)
- [ ] Online list updates live (no refresh) with Available/Invited/In game statuses
- [ ] Disconnect flips presence (userId→count → 0)
- [ ] ☑ Owner observed the live list in two browsers

## M10 — Invitations

- [ ] `SendInvitationCommand` / `CancelInvitationCommand` / `RespondToInvitationCommand` + TTL ~30 s + cooldown ~30 s
- [ ] Guard tests green (self / offline / In game / Invited / cooldown → `Invite.*` codes)
- [ ] Invitation popup on every page (Accept / Reject + 30 s bar)
- [ ] Atomic accept: exactly-once room creation, inviter = X
- [ ] `RespondToInvitation` cancel-on-offline behavior
- [ ] Unit-tested with fake time (TTL + cooldown)
- [ ] W-1, W-2, W-5, W-10 passed

## M11 — Rooms

- [ ] `MakeOnlineMoveCommand` / `LeaveRoomCommand` / `RequestRematchCommand` / `GetRoomQuery`
- [ ] Per-room locks with the 10-race contract unit-tested (store level, mocked seams, fake time)
- [ ] Both-rows-one-transaction test (mocked UoW)
- [ ] Leave-forfeit test (mid-game leave = loss)
- [ ] 60 s grace with fake `TimeProvider` (unit) + countdown on both clients
- [ ] `RoomCleanupService` (5 s tick: forfeit / orphaned close without saves / invitation expiry)
- [ ] W-3, W-4, W-6, W-7, W-9 passed

## M12 — Account extras

- [ ] Change password (current password verified → `Auth.WrongPassword`)
- [ ] Delete account (password re-entry → in one transaction: forfeit-first if in an active room, then cascade delete + opponent "Deleted player")
- [ ] History shows "Deleted player" for the deleted opponent
- [ ] W-12 passed

## M13 — Final verification sweep

- [ ] **FR sweep:** FR-01…FR-36 (excl. superseded FR-23/FR-24) traced to test/script; pass/fail recorded ([test-plan](05-quality/test-plan.md), [test-plan §1 FR registry](00-product/PRD.md))
- [ ] **US sweep:** every happy + unhappy Gherkin block walked against the live app (US-01…US-17)
- [ ] **Error-copy sweep:** every failure code shows exactly its literal message ([error-handling](02-backend/error-handling.md))
- [ ] **NFR spot-checks:** move round-trip p95 < 500 ms · page paint < 2 s · Lighthouse a11y ≥ 90 / perf ≥ 85 · zero console errors · zero build warnings
- [ ] **Deploy rehearsal:** ≤ 30 min manual deploy on the host (owner); produced migration script; prod secrets checklist; `logs/` writability fallback
- [ ] Docs match shipped behavior (docs change first on any deviation, per roadmap)

## Notes

- **Not in this list (skipped for now, per owner):** `06-devops/` deploy runbook unit — part of M13 deploy artifacts only.
- Working agreements (AGENTS) apply throughout: commit format, PR checklist, no secrets, no deploying (owner does), golden rules.
- Later: root `README.md` (index + sourcing rule) when the owner asks.
