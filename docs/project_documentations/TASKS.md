# TASKS.md — Milestone Checklists

*Every task, tick by tick, in the order it gets built. **Tick a box ✅ only after the evidence is recorded** (build green, tests green, or the owner clicked it in the browser). Nothing is "done" until proven — milestone claims are claims, not proof.*

*Milestones mirror [`00-product/roadmap.md`](00-product/roadmap.md) M0 → M13. **Working agreement:** one milestone at a time; the next milestone opens only when the current one's boxes are all ticked. **Quality gate (every milestone):** `dotnet build` zero warnings → all unit tests green → walkthrough script(s) passed and recorded → human clicked it.*

---

## M0 — Spike: host + solution skeleton

- [x] Create `TicTacToe.slnx` and the 4 projects:
  - [x] `src/TicTacToe.Domain` — **zero references** (verify by solution build)
  - [x] `src/TicTacToe.Application` — references **Domain only**
  - [x] `src/TicTacToe.Infrastructure` — references **Application + Domain**
  - [x] `src/TicTacToe.Web` (ASP.NET Core MVC, .NET 10) — references **Application** + the one Infrastructure DI call only
- [x] Dependency rule enforced by project references (Web → Application → Domain ← Infrastructure, never outward)
- [x] `test/TicTacToe.Application.UnitTests/` — the **only** test project (NUnit + Moq + Shouldly + AutoMock), plus `ArchitectureTests.cs` (R-A1…R-A5)
- [x] Folder tree per `01-architecture/clean-architecture-ddd.md` §3: `Features/{Area}/{Feature}/Command|Query`, `Contracts/`, `Mapping/`, `Settings/`, `Data/`, `Identity/`, `Services/`, `Controllers/`, `Hubs/`, `Middleware/`, `wwwroot/{css,js,lib}`, `test/TicTacToe.Application.UnitTests/` skeleton
- [x] `Directory.Packages.props` — pinned: Cortex.Mediator 3.1.2, Mapster 10.0.14, Serilog 10.0.0, EF Core/Identity 10.0.x (any other package = decision entry, [ADR-0012](01-architecture/adr/0012-pinned-stable-stack.md))
- [x] `Program.cs` composition root: `AddApplication()` + `AddInfrastructure()` chained; `ValidateScopes` in Development
- [x] Serilog configured (console + rolling file `logs/`, request logging); **never** log passwords/tokens/connection strings/full emails
- [x] `.github/workflows/dotnet.yml` → restore → build (warnings as errors) → `dotnet test` — green on ($) dotnet build zero warnings, CI first PR green
- [x] Identity cookie wired in `Program.cs` (`IdentityConstants.ApplicationScheme`); SQL Server **Express** connection via user-secrets only (never committed); CSS/Javascript folders exist under `wwwroot/`
- [x] `spikes/SignalRCheck/` SignalR/websockets check — **result recorded**, then throwaway deleted
- [x] EF migration plumbing: `AddInfrastructure(IConfiguration)` + initial migration applies green on Express via `dotnet ef database update`
- [x] Owner checklist: app opens on the host plan; walkthrough M0 evidence recorded

**Gate:** `dotnet build` zero warnings · CI green on first PR · pins in `Directory.Packages.props` · spike negotiates SignalR on the host plan.

**Evidence (M0 closed, owner-confirmed 2026-11-08):** CI run #1 `5b23936` green · spike `walkthroughs/W-M0-2026-11-08.md` (200 + connectionId, spike deleted) · EF `walkthroughs/W-M0-EF-2026-11-08.md` (`20261108090327_InitialIdentity` on Express 2025, backup `C:\Temp\TicTacToe-backup-20261108.bak`) · owner `walkthroughs/W-M0-OWNER-2026-11-08.md` (“opened OK”). Build Release 0 warnings 0 errors, tests 5/5.

## M1 — Engine core

- [x] `Domain/Engine/`: `Board`, `Player`, `Rules` (Validate/Apply/Evaluate), `GameResult`
- [x] E1 legality (bounds, occupancy, turn) — tests
- [x] E2 strict X-first alternation — tests
- [x] E3 all 8 win lines — `[TestCase]` per line, both symbols
- [x] E4 draw detection — test
- [x] E5 finished immutability — tests
- [x] E6 determinism — tests
- [x] **100% line coverage** on the engine (hard gate)
- [x] Architecture tests prove `Domain` references nothing

**Evidence (M1 closed 2026-11-08):** `ad8fbb1` — `dotnet build` Release 0 warnings 0 errors · `dotnet test` 49/49 (5 architecture + 44 engine) · coverlet `coverage.cobertura.xml` Domain line 1.0 + branch 1.0.

## M2 — AI (Easy → Medium → Hard)

- [x] Easy: seeded `IRandomSource`, legal move only
- [x] Medium: takes an immediate win → blocks an immediate loss → center → corner → any
- [x] Hard: exhaustive minimax (memoized), prefers fastest win
- [x] Hard **never loses** — exhaustive sweep proves it
- [x] Reply lands ≤ 500 ms total incl. artificial 300–500 ms "thinking" delay; board non-interactive meanwhile

**Evidence (M2 closed 2026-11-08):** Domain `IComputerPlayer` + `EasyAi`/`MediumAi`/`HardAi` + `IRandomSource` + `Difficulty` + `SystemRandomSource` + `ComputerPlayerFactory` (DI in Infrastructure only) — `dotnet build` Release 0 warnings 0 errors · `dotnet test` 83/83 (6 architecture + 44 engine + 33 AI/factory) · Hard sweeps (as X and as O) 0 losses in ~0.8s, `ChooseMove` <500ms (152ms measured).

## M3 — Local pass-and-play

- [x] `Game/Web` page renders server `RoomState`-equivalent board (stateless: browser sends board + move)
- [x] `PlayMoveCommand` + `POST /api/game/move` (server validates via engine)
- [x] Winning-line highlight + status text (game state, not just code)
- [x] Reset (keeps scoreboard) + New match (zeroes scoreboard)
- [x] Session scoreboard (X / Draws / O) tracked incl. guests
- [x] Handler tests green (unit)
- [x] ☑ Owner clicked a full 2-player game

**Evidence (M3 closed, owner-confirmed 2026-11-08):** `345b2c4` — build Release 0 warnings 0 errors · `dotnet test` 91/91 (8 handler U-2) · live `GET /` 200, `POST /api/game/move` valid 200 (turn flips to O) / occupied 409 / no-token 400, `walkthroughs/W-M3-2026-11-08.md`.

## M4 — Computer play

- [x] Mode/difficulty selector (Easy / Medium / Hard) + factory (`IComputerPlayerFactory`)
- [x] Computer reply applied inside the same server call (stateless board round-trip)
- [x] Board disabled during thinking + thinking status shown
- [x] Stateless statelessness proven: no server state is kept between requests
- [x] ☑ Owner played all 3 difficulties

**Evidence (M4 closed, owner-confirmed 2026-11-08):** `ec0f838` — build Release 0 warnings 0 errors · `dotnet test` 95/95 (12 handler incl. Medium center / Easy seeded / human win no reply) · live Easy X@0→O@6, Medium X@0→O@4, Hard X@0→O@4 (300ms delay, 344ms cmd), `walkthroughs/W-M4-2026-11-08.md`.

## M5 — Save & forgery proof

- [x] `Matches` table + migrations (Mode, Difficulty, Symbol, Result, Moves, UTC)
- [x] `SaveMatchCommand` — **server replays** the move list, verifies legality + result
- [x] Forged/tampered result → `Game.ForgedResult`, row **not** written
- [x] Honest save writes exactly one row (insert-once)
- [x] W-11 passed (forgery probe)

**Evidence (M5 closed 2026-11-08):** `Matches` migration `20261108120613_AddMatches` on Express 2025 (UserId Cascade, Opponent NoAction for SQL 1785; M12 nulls explicitly) · build Release 0 warnings 0 errors · `dotnet test` 102/102 (7 new SaveMatch incl. honest 1-row / tampered / illegal / unfinished / guest / difficulty / draw) · W-M5 walkthrough.

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

## M7.5 — Theme, sound & settings (polish)

> **Senior designer standard (FR-13, FR-21, FR-22, US-14):** clean, modern, calm — dark-first, one accent (indigo), shape before color. Dark-first tokens only in `site.css` (`--ttt-*`), no hex in views, no Tailwind. Every interaction answers in <150ms. Touch targets ≥44px, `prefers-reduced-motion` collapses motion, board announced via `aria-live`.

- [ ] **Tokens:** `--ttt-bg/surface/border/text/primary/mark-x/mark-o/success/danger/status`, fonts (Inter + JetBrains Mono self-hosted), radius `card/control/pill`, `shadow-card/overlay/glow`, motion `fast 150ms/normal 200ms/slow 300ms/deliberate 500ms` — Bootstrap 5.3 `data-bs-theme` swap, never a per-component concern
- [ ] **Color rules:** marks only for game semantics, dot+label never color alone, text contrast ≥4.5:1, focus ring ≥3:1 (indigo-400), white reserved for modal headings
- [ ] **Theme (FR-13):** Light/Dark/Device (default Device), applied **before first paint** (inline head script, no flash), persisted in `localStorage`, switchable from navbar + Settings
- [ ] **`/Settings` public page (FR-22):** Theme (Light/Dark/Device) × Sound (On/Off, default On) + “Reset to defaults”, browser-only (`localStorage`, no server round-trip), works for guests
- [ ] **Sound (FR-21):** synthesized Web-Audio tones only (place/win/lose/draw/ping/toggle, no files), always-visible mute toggle, persisted (`ttt:sound`), only after first interaction, null-guarded (missing asset → silence, never broken page)
- [ ] **Components (taskwise):** Button (press `scale(0.97)` 150ms), Input (label above, red error below, eye toggle), `ttt-card` (24px padding, shadow-card), Avatar (initials, aria-hidden), Badge/StatusDot (Available/Invited/In game + Connected/Reconnecting), Toast (bottom-right, auto-dismiss 4s), Spinner (always with label), Skeleton (1.5s shimmer), `ConnectionBadge` (pulse 2s)
- [ ] **Board polish:** mark draw SVG stroke-dash 300ms (`ease-out`), winning line draw 500ms + glow pulse, hover ghost preview 150ms (no touch), invalid shake 300ms, turn pulse 2s, board card 560px gap 8px cells ≥64px ghost hover
- [ ] **Modals/confetti:** result + invitation popup scale 0.96→1 + fade spring 200ms (Esc=Reject for invite, focus on Reject first), confetti 900ms win burst (respects reduced-motion → CSS glow)
- [ ] **Responsive/a11y:** 360px no h-scroll, layout `≥1024` two-column Home / `768-1023` single / `<768` compact; grid `role=grid/cell` labels “Row N, column M”, keyboard arrows + Enter/Space, `aria-live=polite` (turns/moves/status, countdown every 10s), modal focus trap
- [ ] **NFR gate:** Lighthouse a11y ≥ 90 / perf ≥ 85, keyboard-complete game, zero console errors, game.js ≤ ~15KB per module
- [ ] ☑ Owner toggled theme + sound and Settings reflects it

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
