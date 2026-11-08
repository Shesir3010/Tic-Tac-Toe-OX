# Tic Tac Toe: PRD & Technical Documentation

**Version:** 1.0 (Master document) | **Status:** Canonical specification — code is built and verified against this file | **Owner:** MD. Mahid Hasan Shesir
**Stack:** .NET 10 · ASP.NET Core MVC (Razor) · Bootstrap 5 · vanilla JS · SignalR · SQL Server (EF Core) · ASP.NET Core Identity (cookies) · Cortex.Mediator · Mapster · Serilog · NUnit + Moq + Shouldly
**Delivery:** M0–M7 · Host: MonsterASP.NET (owner deploys manually) · **Started:** 2026-11-06 (M0–M6 shipped under the original docs; v1.0 supersedes them)

> **ARCHIVED SEED (2026-11-07).** This file is superseded by the living docs in `00-product` – `06-devops`, which restructure and extend it. **Room-code sections here (FR-23/24, §3 online flow, §14 routes) are superseded by the invitation system** — see [`PRD.md`](PRD.md) §9 and [`../01-architecture/adr/0009-invitations-replace-room-codes.md`](../01-architecture/adr/0009-invitations-replace-room-codes.md). Kept unedited as the historical seed.

> **Origin note:** consolidates the original flat docs (`PRD/FEATURES/DESIGN/ARCHITECTURE/TESTING/DECISIONS/TASKS.md`) into one canonical document, upgraded so every requirement, contract and behaviour is testable. Restructured into the living section docs (00-product → 06-devops) without rewrites; then this file is frozen as the archive seed.

## 1. Document Overview

| Section | Contents |
| --- | --- |
| 2 | Product: vision, goals/non-goals, personas, scope, decisions carried from review |
| 3–4 | Core flows; key acceptance criteria |
| 5–7 | Functional requirements (FR-01…FR-31); non-functional requirements; success metrics |
| 8 | User stories & acceptance criteria (US-01…US-14) |
| 9–11 | UX/UI specification (pages, board, tokens, sound, accessibility) |
| 12–13 | Architecture: stack, layers, solution structure, cross-cutting design |
| 14–15 | Domain model (engine, AI, Room, Match); application design (CQRS, error codes) |
| 16–19 | Infrastructure design; transport contracts (HTTP + SignalR); database schema; frontend spec |
| 20–22 | Configuration & secrets; security; testing strategy |
| 23–24 | DevOps, CI & deployment readiness; roadmap (M0–M7) and risks |
| 25–26 | Future scope (V2); assumptions |

**Reading order:** product owner/reviewer → §2–§11, §23–§24 · backend → §12–§19, §21–§22 · frontend → §10–§12, §16, §18 · QA → §5, §8, §19–§22.

## 2. Product Requirements

### 2.1 Vision
A polished, friendly Tic Tac Toe anyone can open and play instantly: two players on one device, a solo player vs the computer, or two friends online in a room with a code — clean rules, fair server refereeing, saved histories for logged-in players, a professionally designed interface.

### 2.2 Problem & Goals
Tic Tac Toe is universally known — the ideal first product for proving a repeatable AI-driven build (PRD → tasks → code → tests → review, human review at milestones). **Goals:** (1) polished, bug-free, deployment-ready; (2) online multiplayer as a core feature, built *completely*; (3) registered players' results persist across sessions; (4) validate the AI workflow; (5) small, readable, well-tested codebase.

### 2.3 Non-Goals (v1)
Chat, ratings/ELO, friends, spectators, tournaments, timers/clocks · quick match, leaderboards, monetization, admin dashboard · social login, email confirmation (free host has no email) · board sizes ≠ 3×3 · settings beyond Theme & Sound · branding/logo (post-release) · private/password rooms, >2 players.

### 2.4 Personas

| Persona | Need |
| --- | --- |
| **Sara** — casual player | Open and play immediately (local/vs computer), no signup friction |
| **Rafi** — returning player | Register once; totals and history survive closed tabs; online rooms via code |
| **Nadia** — opponent on another device | Join fast, moves appear instantly, fair outcome even if her connection drops |
| **Owner** (MD. Mahid) | Clean, tested, documented app; manual one-step deployment |

### 2.5 Scope Summary
**In scope:** 3×3 board, X first · modes: local PvP, vs Computer (Easy/Medium/Hard), online (registered) · win/draw detection + winning-line highlight · Reset + New match · Identity accounts (no email confirmation, immediate login) · Account page (totals, history link, change password, delete account w/ password re-entry) · matches persisted in SQL Server with totals computed from them · guests: local/computer only, session score · online: room by code, SignalR real-time, server-authoritative, results for both players, rematch, leave anytime (mid-game = loss), ~60 s reconnect grace, ~10 min idle expiry, one active room per player · light/dark theme + sounds (browser-stored) · Settings page: Theme & Sound only · responsive, WCAG 2.1 AA, keyboard + mouse · deployment-ready.
**Out of scope:** §2.3.

### 2.6 Decisions Carried from Review

| # | Decision | Status |
| --- | --- | --- |
| D-01 | Web app (no mobile/desktop/CLI) | Accepted |
| D-02 | Stack per header (Razor MVC — earlier React/TS proposal superseded); SignalR added for online | Accepted |
| D-03 | Registered scores/history in SQL Server; guests session-only | Accepted |
| D-04 | Host **MonsterASP.NET** (replaced SmarterASP.NET); owner deploys manually at M7 | Accepted; host verified 2026-11-06 (§23) |
| D-05 | Name stays "Tic Tac Toe"; branding later | Accepted |
| D-06 | **Server-authoritative rules**: pure C# engine in Domain; JS renders; local saves verified by server-side replay; online fully server-controlled | Accepted |
| D-07 | Clean Architecture (Domain, Application, Infrastructure, Web); Repo pattern, CQRS, UoW; engine stays pure | Accepted |
| D-08 | NUnit + Moq + Shouldly (+ Autofac.Extras.Moq for handler tests); Playwright E2E; SignalR client; fake `TimeProvider` | Accepted |
| D-09 | Layout mirrors the owner's MaleFashion project; engine inside `Domain/Engine/`; architecture test enforces purity; Mapster (D-16) for mapping | Accepted |
| D-10 | No email confirmation in v1 (no host email) | Accepted |
| D-11 | Account deletion kept simple: password re-entry; deletes account + history | Accepted |
| D-12 | Online multiplayer is core: rooms by short code, SignalR, active rooms in memory, finished matches in SQL Server; leave-mid-game = loss; 60 s reconnect; ~10 min expiry; one room/player; rematch | Accepted |
| D-13 | Local DB: SQL Server Express + SSMS; user-secrets; migrations only; hosted DB never touched by agents | Accepted |
| D-14 | Theme/sound browser-only (`localStorage`); Settings = Theme + Sound; no "default difficulty"/"play as" (FR-12 stays Could) | Accepted |
| D-15 | Phase order: M0 setup · M1 engine · M2 local · M3 accounts · M4 online · M5 AI · M6 theme/sound/settings/guests/a11y · M7 verify + deploy prep | Accepted |
| D-16 | **Cortex.Mediator** (not MediatR) + **Mapster** + **Serilog**; SOLID; ctor DI; try/catch at boundaries; minimal comments; only Cortex logging behavior enabled | Accepted |
| D-17 | Pinned packages via `Directory.Packages.props`: Cortex.Mediator 3.1.2 (MIT), Mapster 10.0.14 + Mapster.DependencyInjection 10.0.14 (MIT), Serilog.AspNetCore 10.0.0 (Apache-2.0), EF Core/Identity 10.0.14 (MIT), Logging.Abstractions 10.0.14, Pkcs 10.0.14 (audit fix) | Accepted |
| D-18 | Hosting check: host supports .NET 10, SQL Server 2025 + SSMS, SignalR/WebSockets/MVC, HTTPS (Let's Encrypt), HTTP/3+TLS 1.3; free plan 256 MB RAM, 1 GB DB, no email | Accepted |
| D-19 | Autofac.Extras.Moq approved test-only (AutoMock conventions) | Accepted |

## 3. Core Flows

**Local play:** Game page → mode (+difficulty) → turns → result → [Reset / New match] → (logged-in user: match saved via `POST /api/matches`, totals update).
**Accounts:** Register → logged in → Account (totals, change password, delete account) → History → logout.
**Online:** Lobby → Create room (code shown, X waits) → Rafi enters code → *Playing for both* → hub moves (server validates each) → end (win/draw/forfeit/timeout) → both match rows saved once → [Play again ×2 → rematch] or [Leave → lobby].
**Disconnect:** socket drops mid-game → `PlayerDisconnected(60s)` to opponent → reconnect cancels timer (Room restored via `GetRoomQuery`) → expiry: forfeit for the connected player.
**State machines:** **Game:** `[empty] → Playing → Finished-Win | Finished-Draw → (Reset) [empty]`; New match also resets the scoreboard. **Room:** `Waiting → Playing → Finished → (both press rematch) Playing | Closed`; `Waiting → Closed` (idle ~10 min / host leaves / joined?); finished board immutable. Rules: R1 mutations only under the room's lock, each transition exactly once · R2 finished = terminal for the board ( rematch or Closed only) · R3 exactly one match-save per game per player · R4 one active room per player · R5 a Closed code is unrejoinable ("Room expired."). **Leave-save rule:** Waiting → close, no rows · Playing → opponent wins (forfeit), both rows saved, closed · Finished → close, no new rows.

## 4. Acceptance Criteria (Key Stories)

- **US-10 join race:** Given a Waiting room, when two different users click Join simultaneously, exactly one becomes O and enters the game; the other receives a clear "Room is full." and stays in no room.
- **US-11 move round-trip:** Given it is Sara's (X's) turn and she clicks an empty cell, both boards show the X immediately (round-trip budget per §5 NFR) and both turn indicators flip together. Given Rafi clicks when it is not his turn: his board is unchanged, nothing is broadcast, the server returns `Game.NotYourTurn`.
- **US-12 disconnect grace:** Given Rafi's connection drops mid-game, Sara sees "Opponent disconnected, 60 s left to return" with a countdown; reconnect within the window cancels it and play continues; expiry awards Sara the win and both matches are saved (exactly once per player).
- **US-08 delete account:** Given a logged-in user re-enters their password and confirms, account + all match history are deleted in one transaction, they are signed out, login now fails; the opponent's history shows "Deleted player". Wrong password → nothing deleted, inline error.

Full Given/When/Then criteria for all 14 stories: `user-stories.md` (from §8 summary table below).

## 5. Functional Requirements

| ID | Requirement | Priority |
| --- | --- | --- |
| FR-01 | An empty 3×3 board is displayed at the start of every game and page load | Must |
| FR-02 | Strict X-first alternation; occupied cells never change; illegal/non-alternating moves are rejected **server-side** | Must |
| FR-03 | The server detects a win on any of the 8 lines (3 rows, 3 columns, 2 diagonals) and ends the game | Must |
| FR-04 | The server detects a draw when all 9 cells are filled with no winner | Must |
| FR-05 | After a game ends every further move — any player, any client path — is rejected; finished games are immutable | Must |
| FR-06 | Winning cells are highlighted: distinct background + line overlay | Must |
| FR-07 | "Reset" restarts a local game instantly without page reload, keeping the scoreboard | Must |
| FR-08 | Mode selection: local PvP / vs Computer; link to the online Lobby | Must |
| FR-09 | vs Computer difficulty: Easy (random), Medium (takes wins, blocks losses), Hard (unbeatable minimax) | Must |
| FR-10 | Computer reply computed by the server engine, applied within 500 ms incl. a 300–500 ms "thinking" delay (board non-interactive) | Should |
| FR-11 | Session score (X/D/O) tracked for all players incl. guests; reset by "New match" | Must |
| FR-12 | Choosing who plays X in vs Computer — **not planned** (Could) | Could |
| FR-13 | Light/dark theme + "use device setting", navbar & Settings switchable, applied before first paint (no white flash), stored in the browser | Must |
| FR-14 | Identity registration/login/logout; no email confirmation; duplicates get inline errors; wrong credentials get one generic error | Must |
| FR-15 | Every completed match of a logged-in user saved with mode, difficulty?, symbol, result (owner's view), move list, UTC timestamp | Must |
| FR-16 | Persistent totals (W/L/D) computed from saved matches | Must |
| FR-17 | History page: user's matches newest first, paged, opponent name shown ("Deleted player" if the opponent deleted their account) | Must |
| FR-18 | Guests play local/computer; their results never hit the database | Must |
| FR-19 | Password re-entry + single confirm deletes the account and all match history in one transaction, signs out; login afterwards fails | Must |
| FR-20 | Account page: totals, History link, change password (current password required), delete account | Must |
| FR-21 | Sound effects (place, win, lose, draw, opponent move) via Web Audio (no files), default **on**, always-visible mute; immediate apply, browser-stored | Should |
| FR-22 | Settings: Theme (Light/Dark/Device, default Device) × Sound (On/Off, default On) + "Reset to defaults"; `localStorage`; works for guests; no server code | Must |
| FR-23 | Registered user creates a room and gets a short unguessable code (5 chars, deconfusable alphabet); creator sits as X and waits | Must |
| FR-24 | Registered user joins by code, becomes O, game starts immediately; distinct errors: "Room not found." / "Room is full." / "Room expired." | Must |
| FR-25 | Online moves sent and shown in real time (SignalR); server validates turn and legality and broadcasts results to both | Must |
| FR-26 | When an online game ends, one match row per player (mode Online, opponent's name) saved in **one transaction** | Must |
| FR-27 | Rematch: either player presses "Play again"; when **both** have, the board resets in the same room (X starts) | Must |
| FR-28 | Leave anytime; leaving mid-game awards the opponent the win and saves both rows; leaving a Waiting room just closes it | Must |
| FR-29 | Disconnect: ~60 s to reconnect (reconnect cancels the timer; countdown shown to both); expiry = win for the connected player | Must |
| FR-30 | Unused rooms expire after ~10 minutes; one **active** room per player (re-create → returned to the existing room) | Must |
| FR-31 | Online play is registered-users-only; guests are redirected to login with a clear message | Must |

## 6. Non-Functional Requirements

| Category | Requirement |
| --- | --- |
| Performance | Pages interactive < 2 s on broadband; online move round-trip p95 < 500 ms; computer reply ≤ 500 ms total; each JS module ≤ 15 KB; no SPA framework |
| Scalability | Single node: ≥ 100 concurrent rooms, ≥ 200 concurrent authenticated connections on the free plan; **one app instance only** (rooms in memory) |
| Reliability | Server-authoritative; two near-simultaneous moves → exactly one accepted per turn (per-room lock); crafted client data never corrupts a game or result |
| Security | Identity defaults (hashing, lockout), HTTPS only, anti-forgery on all POSTs, `[Authorize]` on user-only pages + hub, server validation of every move/result, unguessable codes, EF parameterized access only |
| Privacy | Store only account data (username, normalized email, hash, matches); no third-party tracking; account deletion (FR-19); theme/sound stay in the browser |
| Accessibility | WCAG 2.1 AA: contrast ≥ 4.5:1; full keyboard operation (arrows move focus, Enter/Space places); `aria-live=polite` for turns/results/online status; shape+text, never color/sound alone |
| Compatibility | Latest 2 versions of Chrome, Firefox, Safari, Edge + current mobile browsers |
| Quality | Engine 100% line coverage; zero console errors on the happy path; zero build warnings; all suites green at milestone exit |
| Maintainability | SOLID; CQRS; ctor DI with an interface per service; minimal comments; structured Serilog logging — never passwords/tokens/emails; friendly errors, no technical details |
| Hosting | Host supports .NET 10, SQL Server + SSMS, SignalR/WebSockets (Verified §23); documented manual deploy ≤ 30 min for the owner |

## 7. Success Metrics

1. Every **Must** FR passes an automated test.
2. Hard AI: 0 losses across an exhaustive minimax sweep of all reachable states.
3. Two-browser online session: create → join → play to win → both users see a saved match with the opponent's name.
4. Simultaneous-join and simultaneous-move tests prove exactly-one-winner semantics.
5. Lighthouse Accessibility ≥ 90, Performance ≥ 85 on the game page.
6. Zero-warning build; fully green `dotnet test` locally + CI.
7. Human time limited to milestone reviews.

## 8. User Stories & Acceptance Criteria (Summary)

Conventions: **Sara** = X/host, **Rafi** = O/joiner; board indices 0–8 row-major; "immediately" = no page reload; errors render as inline messages or typed `OperationResult` failures (§15.3) — never blank screens or stack traces. Full Gherkin lives in `user-stories.md`.

| US | Story | FRs | Key scenarios (full ACs in `user-stories.md`) |
| --- | --- | --- | --- |
| US-01 | Play locally on one device | 01,02,11 | Turns alternate; occupied cell does nothing; frozen after win |
| US-02 | Win/draw detection + highlight | 03,04,06 | 8-line win; draw; neither score changes without save/session update |
| US-03 | Play the computer | 08–10 | Hard never loses (exhaustive); Medium takes wins & blocks; Easy random |
| US-04 | Reset / New match | 07,11 | Reset keeps scoreboard; New match zeroes it |
| US-05 | Register, login, logout | 14 | Register signs in; duplicates inline; generic credential error; lockout message |
| US-06 | Saved stats & history | 15–17 | Local match saved; **forged save rejected**; totals survive days; opponent name stored |
| US-07 | Guest play | 11,18,31 | Guest plays local/computer w/ session score; lobby/room redirect to login |
| US-08 | Delete my account | 19 | Password-confirmed wipe in one transaction; wrong password deletes nothing; "Deleted player" for opponents |
| US-09 | Create a room | 23,30 | Code shown w/ Copy; re-create returns to existing room; idle ~10 min expiry |
| US-10 | Join a room | 24,30,31 | **Join race → exactly one O**; not found / full / expired messages; one-room limit |
| US-11 | Play online | 25,26,05 | Instant moves both boards; not-your-turn does nothing; once-per-player saves; reload rejoins via `GetRoomQuery` |
| US-12 | Leave or disconnect | 28,29 | Leave mid-game = loss; 60 s grace w/ countdown; reconnect cancels; expiry forfeit; waiting-room leave saves nothing |
| US-13 | Rematch | 27 | Both press → board reset (X first); waiting text; leaving cancels rematch |
| US-14 | Theme & sound | 13,21,22 | Immediate apply + persistence; sounds only after interaction; mute persists; no white flash |

## 9. UX/UI Specification

> Normative for the frontend. "The UI looks broken" is defined as: any deviation from this section. Components: file names/tokens below; extra component detail in `ux-ui-design-system.md`, page wireframes in `pages-and-flows.md` (Units 2+).

**Principles:** simple, clear, fast, accessible · Bootstrap 5 as a base customized to a premium look · consistent hierarchy/spacing on every page · shape before color · feedback for every action (< 150 ms) · color/sound are never the only signals · `prefers-reduced-motion` honored.

| Page | Route | Access | Key elements & states |
| --- | --- | --- | --- |
| Game (home) | `/` | all | Mode + difficulty selector; "Play online" link; status line (`aria-live`): "X's turn/O's turn/X wins!/It's a draw/Computer is thinking…"; board (below); scoreboard X\|Draws\|O (session; +Account link for users); Reset, New match; dismissible guest notice |
| Register/Login | Identity UI | all | Inline per-field validation; duplicate/credential/lockout messages per §15.3; no email step |
| Lobby | `/Rooms` | registered | Create room → large code + Copy + "Waiting for opponent…" + Cancel; Join-with-code box → "Room not found."/"Room is full."/"Room expired."; active-room banner with return button |
| Room | `/Rooms/{code}` | participant | "You (X)"/"Opponent (O)"; room code + Copy; status (`aria-live`): waiting / your turn / opponent's turn / "Opponent disconnected, {n} s left" / reconnecting / you win / you lose / draw / "Opponent left the room"; same board component, cells disabled off-turn; **Leave room** (confirm "Leaving now counts as a loss." mid-game), **Play again** (waiting text until both) |
| Account | `/Account` | registered | W/L/D totals; History · Change password · Delete account links; password form w/ inline errors |
| History | `/History` | registered | Table: date, mode (+difficulty), symbol, opponent (or "Deleted player"), result badge; newest first; paged; empty state |
| Delete account | `/Account/Delete` | registered | Destructive warning; password field; single Confirm button; anti-forgery POST |
| Settings | `/Settings` | all | Theme × Sound table per FR-22; Reset to defaults; Account link for users; errors inline |

**Navbar:** "Tic Tac Toe" (home) · Play · Online — logged-in: History, Account, user name, Logout; guests: Login/Register — right: Settings, theme toggle, mute (shortcuts share the same stored values as Settings).

**Board contract:** 3×3 of `<button>`s; square, ≤ 400 px, fits 360 px viewports; labels "Row N, column M, empty"; hover/focus ring on open cells; occupied/frozen cells not interactive; winning cells get highlight + overlay (≤ 300 ms); mark scale-in ≤ 150 ms; reduced-motion disables both.

**Visual tokens** (Bootstrap 5.3 `data-bs-theme`, custom CSS vars): `--ttt-x` `#2563eb`/`#60a5fa` · `--ttt-o` `#dc2626`/`#f87171` · `--ttt-cell-bg` `#f3f4f6`/`#1e1e1e` · 2 px focus ring ≥ 3:1. Inline head script applies the saved theme **before first paint** — no white flash. Theme: light/dark/device.

**Sound (FR-21):** synthesized Web-Audio tones (no files): place, win, lose, draw, opponent move (online); only after user interaction; always-visible mute; `ttt:sound` default on; never the only signal.

**Feedback for every action:** hover/focus/press states; computer thinking delay 300–500 ms with status text; online board disables while submitting or awaiting the opponent; failed saves/network errors → non-blocking友好 notice, state stays visible.

**Accessibility release gates:** keyboard-operable board + ARROW/Enter/Space · `aria-live` on all state changes · contrast/focus ≥ AA · labeled forms with associated errors · touch targets ≥ 44 px · Lighthouse a11y ≥ 90. (Detailed checklist in `05-quality` doc, Unit 2 set.)

## 10. Architecture

### 10.1 Stack (fixed — changes need a decision entry)

| Concern | Choice |
| --- | --- |
| Runtime/framework | .NET 10 · ASP.NET Core MVC (Razor Views) |
| Front end | HTML, CSS, vanilla JS (ES modules) · Bootstrap 5.3 |
| Real-time | ASP.NET Core SignalR (online play only) |
| Database | SQL Server via EF Core 10 migrations · local: SQL Server Express + SSMS |
| Auth | ASP.NET Core Identity · cookie auth (same cookie authorizes the hub) |
| CQRS | **Cortex.Mediator** 3.1.2 (MIT, not MediatR) · only its logging behavior enabled |
| Mapping | Mapster 10.0.14 + Mapster.DependencyInjection (MIT) |
| Logging | Serilog (Serilog.AspNetCore 10.0.0): console + rolling file |
| Tests | NUnit + Moq + Shouldly + Autofac.Extras.Moq · WebApplicationFactory · SignalR .NET client · Playwright for .NET |
| CI / host | GitHub Actions · MonsterASP.NET (owner deploys manually; agents never deploy) |

Versions pinned via `Directory.Packages.props` (Central Package Management). Any new package = decision entry.

### 10.2 Layers & dependency rule

```
Web → Application → Domain ← Infrastructure
```

| Project | Responsibility | May reference |
| --- | --- | --- |
| `TicTacToe.Domain` | Core, **zero dependencies**: entities, DTOs, enums, contracts, the pure engine (`Engine/`), Room model (`Online/`) | **nothing** |
| `TicTacToe.Application` | CQRS commands/queries + handlers, Mapster profiles, contracts (`IRepository<T>`, `IMatchRepository`, `IUnitOfWork`, `IRoomStore`, `ICurrentUser`, `IAccountService`, `IComputerPlayerFactory`), settings, exceptions, `OperationResult<T>` | Domain |
| `TicTacToe.Infrastructure` | `ApplicationDbContext`, repositories, `UnitOfWork`, Identity, in-memory room store, `RoomCleanupService`, `ComputerPlayerFactory`, migrations, DI extensions | Application, Domain |
| `TicTacToe.Web` | Controllers, `GameHub`, Razor views, `wwwroot`, `Program.cs`, `GlobalExceptionHandler` | Application; Infrastructure (DI registration **only**) |

**Hard rules:** R-A1 dependency arrows only point inward (architecture test enforces) · R-A2 engine is pure C# — no DB, ASP.NET Core, SignalR, NuGet or I/O in `Domain/Engine/`; rules are never duplicated in controllers/hub/JS · R-A3 SignalR types never appear in Domain/Application · R-A4 nondeterminism comes from injected interfaces (`IRandomSource`, `TimeProvider`), never statics.

```
TicTacToe/
├─ TicTacToe.slnx · Directory.Packages.props · .github/workflows/
├─ spikes/SignalRCheck/                       # throwaway (deleted after M0)
├─ src/
│  ├─ TicTacToe.Domain/      Engine/{Board,Player,GameResult,Rules}.cs · Engine/Ai/{IComputerPlayer,EasyAi,MediumAi,HardAi}.cs
│  │                          Entities/Match.cs · Online/{Room,RoomStatus,RoomPlayer}.cs · DTOs/{Game,Rooms,Matches}/ · Contracts/{IRandomSource}.cs · Enums/
│  ├─ TicTacToe.Application/  Features/{Game,Rooms,Matches,Account}/{Command,Query}/ · Contracts/{Repositories,Services}/
│  │                          Mapping/ (Mapster IRegister) · Settings/RoomSettings.cs · Exceptions/
│  ├─ TicTacToe.Infrastructure/  Data/{ApplicationDbContext,UnitOfWork,Repository/MatchRepository,Migrations/}
│  │                          Identity/{ApplicationUser,CurrentUser,AccountService}.cs
│  │                          Services/{InMemoryRoomStore,RoomCleanupService,ComputerPlayerFactory}.cs
│  └─ TicTacToe.Web/          Program.cs · appsettings*.json · Hubs/GameHub.cs · Middleware/GlobalExceptionHandler.cs
│                             Controllers/{Home,Rooms,Account,History,Settings}Controller.cs · Models/ · Views/… · wwwroot/{css,js,lib}
└─ tests/                     TicTacToe.UnitTests/ (mirrors Engine + Features) · TicTacToe.IntegrationTests/ · TicTacToe.E2ETests/
```

**File placement:** one use case folder per §15 use case — `Features/{Feature}/Command/XxxCommand{,Handler}.cs`, `Features/{Feature}/Query/GetXxxQuery{,Handler}.cs`. Each layer registers its services in `AddApplication()` / `AddInfrastructure(IConfiguration)`; `Program.cs` composes.

### 10.3 Cross-cutting design

- **DI:** interface + implementation for every service; **constructor injection only**; no statics/service locators. Lifetimes: Scoped = repositories, UoW, account/current-user; **Singleton** = `IRoomStore`, `TimeProvider`, `RoomSettings`; Hosted = `RoomCleanupService`. Domain is container-agnostic (engine gets `IRandomSource` via ctor from layers above).
- **Logging (Serilog):** configured once in `Program.cs` (+ request logging); `ILogger<T>` everywhere else; **Domain/engine never log**; structured templates (`"Room {RoomCode} closed"`); levels: Info = business events, Warning = expected failures, Error = exceptions; **never** passwords/tokens/connection strings/full emails (log userId); Cortex logging behavior records each command/query with timing.
- **Error handling:** expected failures = `OperationResult<T>` values (§15.3); unexpected = `try/catch` with specific types at boundaries (DB commits, Identity calls, each hub method, cleanup loop, browser fetch/SignalR), log with context, return friendly failure; never swallow, never `throw ex;`; `GlobalExceptionHandler` is the safety net (friendly page/problem-details).
- **Mapping (Mapster):** `IRegister` profiles in `Application/Mapping`; used via `IMapper`; never in Domain or hand-written in controllers; unit test compiles the config.
- **Repository & UoW:** generic `Repository<T>` + `MatchRepository` (`GetByUserAsync`, `GetStatsAsync`, `DeleteByUserAsync`); `IUnitOfWork.SaveChangesAsync()` = the only commit; **handlers own commits** (online finish = both rows, one commit); repositories stay thin.
- **SOLID:** SRP = one use case/module · OCP = new difficulty (`IComputerPlayer`) or behavior (pipeline) added, not edited · LSP = every `IComputerPlayer` legal on any non-full board; any `IRoomStore` keeps thread-safety/uniqueness · ISP = small focused interfaces · DIP = Application owns abstractions, outer layers implement.
- **Conventions:** nullable enabled; async for I/O; small classes; names explain (comments only for non-obvious reasons); SSR: Razor renders server state, JS only renders + calls server.

## 11. Domain Model

### 11.1 Engine (`Domain/Engine/`) — pure, zero deps

| Type | Shape/rules |
| --- | --- |
| `Player` | enum `X`/`O` |
| `Board` | 9-slot `Player?` array, value semantics, `Empty()`, `Move(index, player)` → new board or throw |
| `GameResult` | `Outcome` (XWin/OWin/Draw), `WinningLine` (`int[3]?`) — produced once |
| `Rules` | `ValidateMove(board, i, expectedTurn)`, `ApplyMove`, `Evaluate(board)` → result or still-playing |

**Invariant laws (engine tests, each independently failing):** E1 legal move ⇔ index 0–8 ∧ cell empty ∧ game unfinished ∧ mover's turn · E2 X first, strict alternation · E3 8 win lines `[0-2][3-5][6-8] [0,3,6][1,4,7][2,5,8] [0,4,8][2,4,6]` · E4 draw ⇔ full board ∧ no win · E5 finished is immutable · E6 `Evaluate` deterministic.

### 11.2 AI (`Engine/Ai/`)

```csharp
public interface IComputerPlayer { Player Mark { get; } int ChooseMove(Board board); }
public interface IRandomSource { int Next(int maxInclusive); }
```

Easy = uniform random open cell (seeded `IRandomSource`) · Medium = take immediate win → block immediate loss → center → corner → any · Hard = exhaustive minimax (memoized), prefers fastest win, **never loses**. New difficulty = new class + factory entry (OCP); `ComputerPlayerFactory` (Infrastructure) resolves difficulty → player.

### 11.3 Room (`Domain/Online/Room`) — pure model, no I/O

`Code` (5 chars, alphabet `23456789ABCDEFGHJKMNPQRSTUVWXYZ` — no 0/O, 1/I/L; unique among active rooms) · `PlayerXId`/`PlayerOId?` (host=X, joiner=O) · `Board` (X opens) · `Turn` · `Status` (`Waiting/Playing/Finished/Closed`) · `Result?` (outcome, reason, winning line — set once) · `CreatedAtUtc`, `LastActivityUtc`, `DisconnectedUntilUtc?` (grace deadline, Playing only) · `RematchRequestedByX/Y` flags (both → reset, clear, X first). Invariants: §3 R1–R5 + unique code + one-active-room/user + participants-only mutations + seat capacity 1/1.

### 11.4 Match (`Domain/Entities/Match`) — one row per player per finished game

`Id` (int PK) · `UserId` FK cascade · `OpponentUserId` FK nullable **set-null** · `Mode` (Local/Computer/Online) · `Difficulty?` · `PlayerSymbol` (row owner's) · `Result` (Win/Loss/Draw — owner's view) · `Moves` ("4,0,8,2", ≤ 17 chars, replayable) · `PlayedAtUtc` (from `TimeProvider`). Online finish = 2 rows, one transaction; totals computed from rows (no separate stats table v1).

### 11.5 DTOs (`Domain/DTOs/`)
Game: `MoveRequest{Mode,Difficulty?,Board,MoveIndex}`, `MoveResponse{Board,Turn?,Result?,ComputerMoveIndex?}` · Rooms: `RoomState{Code,Status,Board,Turn,Players,Result?,SecondsToReconnect?}` + event payloads (§14.3) · Matches: `MatchSummary`, `UserStats{Wins,Losses,Draws}`, `MatchHistoryItem{PlayedAtUtc,Mode,Difficulty,Symbol,Opponent,Result,Moves}`.

## 12. Application Design (CQRS — Cortex.Mediator)

One use case = one `ICommand<T>`/`IQuery<T>` + one handler; controllers/hub dispatch via `IMediator` only; commands mutate + commit once via `IUnitOfWork`; queries never persist; only the logging behavior on (extra behaviors = decision entries).

### 12.1 Use-case catalog

| Use case | Type | Input → Output | Rules / failure codes |
| --- | --- | --- | --- |
| `PlayMoveQuery` | Query | mode, difficulty?, board, moveIndex → board/turn/result?/computer reply | Engine-validated, stateless; no persistence · `Game.NotYourTurn`, `Game.CellOccupied`, `Game.AlreadyOver`, `Game.InvalidCell` |
| `SaveMatchCommand` | Command | move list + claimed result → success | **Server replays** the list; forged → rejected · `Game.ForgedResult` |
| `CreateRoomCommand` | Command | user → `RoomState` | Unique code, host=X, Waiting · `Room.AlreadyActive` → UI redirects to existing |
| `JoinRoomCommand` | Command | code → `RoomState` | Joiner=O, → Playing; race-safe (per-room lock) · `Room.NotFound/Full/Expired/AlreadyActive` |
| `MakeOnlineMoveCommand` | Command | roomCode, moveIndex → `RoomState` | Participant+turn+legality via engine, room lock, on end = both rows one commit; broadcast follows · `Room.NotParticipant`, Game.* |
| `LeaveRoomCommand` | Command | roomCode → success | Waiting→close(no rows) · Playing→opponent forfeit win, both rows, closed · Finished→close · `Room.NotParticipant` |
| `RequestRematchCommand` | Command | roomCode → `RoomState` | Both required → board reset, X first · `Room.NotParticipant`, `Room.Expired` |
| `GetRoomQuery` | Query | roomCode → `RoomState` | Reload-rejoin source of truth · `Room.NotFound/NotParticipant` |
| `GetUserStatsQuery` | Query | user → `UserStats` | W/L/D from Matches |
| `GetMatchHistoryQuery` | Query | page → paged items | Opponent names or "Deleted player" |
| `DeleteAccountCommand` | Command | current password → success | Verify password; delete matches + account in one tx; sign out · `Auth.WrongPassword` |
| `ChangePasswordCommand` | Command | current+new → success | via `IAccountService`; Identity policy |

### 12.2 `OperationResult<T>` & error catalog (the single message vocabulary)

`IsSuccess/Value/Error{Code,Message}`; factories `.Success(v)`/`.Failure(code,msg)`. Codes `Area.Reason`; message copies below are the literal user-facing strings used by pages, hub clients and tests.

| Code | User message | HTTP (JSON endpoints) |
| --- | --- | --- |
| `Game.NotYourTurn` | "It's not your turn." | 409 |
| `Game.CellOccupied` | "That cell is already taken." | 409 |
| `Game.AlreadyOver` | "The game is already over." | 409 |
| `Game.InvalidCell` | "Pick an empty cell." | 400 |
| `Game.ForgedResult` | "The match could not be verified." | 422 |
| `Room.NotFound` | "Room not found." | 404 |
| `Room.Full` | "Room is full." | 409 |
| `Room.Expired` | "Room expired." | 409 |
| `Room.AlreadyActive` | "You already have a game in progress." (+redirect) | 409 |
| `Room.NotParticipant` | "You are not in this room." | 403 |
| `Auth.InvalidCredentials` | "Wrong username/email or password." | 401 |
| `Auth.LockedOut` | "Too many attempts — try again in a few minutes." | 423 |
| `Auth.DuplicateEmail` / `DuplicateUsername` | "An account with this email already exists." / "That username is already taken." | 409 |
| `Auth.WrongPassword` | "Wrong password." | 400 |
| `Validation.Error` | field-specific inline messages | 400 |

Page POSTs re-render forms with inline fields instead of JSON statuses.

## 13. Infrastructure Design

- **`IRoomStore` / `InMemoryRoomStore` (Singleton):** `CreateAsync/JoinAsync/GetAsync(code)/ApplyAsync(code, mutation)/TryCloseExpiredAsync`. Thread-safety: **per-room lock objects** — every mutation runs inside its room's lock (the mechanism behind §3 R1 and the join-race guarantee). Codes retried until unique among active rooms; user→room index enforces one-active-room. Capacity: ~200 B/room ⇒ ≥ 100 rooms ≪ free-plan RAM. **Accepted trade-off:** a restart ends in-progress games (durable rooms only if evidence demands).
- **`RoomCleanupService` (Hosted):** every ~5 s: (1) close Waiting rooms idle > 10 min (no rows); (2) for Playing rooms with a deadline passed → forfeit finish for the connected player, save both rows, close, broadcast. Uses injected `TimeProvider`; per-iteration try/catch so one failure never stops the loop.
- **Identity:** `ApplicationUser : IdentityUser<Guid>` + `DisplayName` (unique, shown in rooms/history); cookie auth (hub `[Authorize]`); lockout 5/5 min; `AccountService : IAccountService` (register, login by username **or** email, logout, change password, delete), `CurrentUser : ICurrentUser`.
- **`ComputerPlayerFactory`:** Difficulty → `IComputerPlayer` (+ `SystemRandomSource : IRandomSource`); registering a new difficulty = one entry.

## 14. Transport Contracts

### 14.1 HTTP surface

| Route | Purpose | Auth |
| --- | --- | --- |
| `GET /` | Game page (local + computer) | — |
| `POST /api/game/move` | `PlayMoveQuery` → `MoveResponse` | optional |
| `POST /api/matches` | `SaveMatchCommand` (server-verifies replay) | required |
| `GET /Rooms`, `GET /Rooms/{code}` | Lobby / Room pages | required |
| `/Account`, `/History`, `/Settings`, Identity UI, `GET/POST /Account/Delete` | pages + account ops | required (Settings optional) |
| `GET /health` | liveness ping for quick checks | — |

All POSTs carry anti-forgery tokens. JSON: camelCase, ISO-8601 UTC dates (`…Z`), PascalCase enums; success = 200 + DTO, failure = 4xx + `{"error":{"code","message"}}` per §12.2. Page POSTs re-render with inline errors.

### 14.2 SignalR (`/hubs/game`, `[Authorize]`)

**Client → hub** (invocations return `OperationResult`-shaped results): `JoinRoom(code)` → full `RoomState` (participant-validated before group add) · `MakeMove(code,index)` → `RoomState` or failure · `Leave(code)` · `RequestRematch(code)` → `RoomState`.

**Hub → clients** (group `room-{code}`, server-derived group names only; payload camelCase):

| Event | Payload |
| --- | --- |
| `PlayerJoined` / `RematchStarted` | `{ room: RoomState }` |
| `MoveApplied` | `{ room: RoomState }` (board, turn, result on end) |
| `PlayerLeft` | `{ room: RoomState \| null }` (null = closed) |
| `PlayerDisconnected` | `{ code, secondsLeft }` |
| `PlayerReconnected` | `{ code }` |
| `RoomClosed` | `{ code, reason: "expired" \| "left" }` |

Event names are exactly these seven — no client-side improvisation.

### 14.3 Race-safety contract (each scenario is a test)

| Scenario | Guarantee | Mechanism |
| --- | --- | --- |
| Two users Join simultaneously | Exactly one becomes O; other → `Room.Full` | Per-room lock serializes mutations; loser re-reads fresh state |
| Double-click / two tabs, same player | Second → `Game.NotYourTurn` or `Game.CellOccupied` | Same lock; turn+board re-validated |
| Leave vs Join | First writer wins; loser gets a coherent result | Both under the room lock |
| Reconnect vs 60 s expiry | If reconnect processed first → timer cancels; if expiry first → rematch impossible, room finished | Same lock + monotonic transitions (R1/R2) |
| Move-win vs cleanup-forfeit | Finished exactly once; one save path | Finish guarded under the lock |

## 15. Database Schema (SQL Server + EF Core 10)

**`AspNetUsers` (+ standard Identity tables, `ApplicationUser`):** add `DisplayName` `nvarchar(50)`, unique, indexed.
**`Matches`:** `Id` `int` PK identity · `UserId` `nvarchar(450)` FK **cascade** · `OpponentUserId` FK nullable **set-null** · `Mode` `tinyint` (0/1/2) · `Difficulty` `tinyint?` · `PlayerSymbol` `char(1)` check in (X,O) · `Result` `tinyint` (W/L/D owner-view) · `Moves` `nvarchar(17)` · `PlayedAtUtc` `datetime2` UTC. Indexes: `(UserId, PlayedAtUtc DESC)`, `(OpponentUserId)`.

Rules: schema changes **only** via EF Core migrations (SSMS = inspection); dev DB `Server=localhost\SQLEXPRESS;Database=TicTacToe;…` in **user-secrets**; hosted DB never touched by agents (migrations strategy decided at M7 and recorded); no stored procedures, no raw SQL in app code.

## 16. Frontend Specification (Razor + vanilla JS)

Razor renders server state; **JS only renders and calls the server** (JSON endpoints / hub) — rules live once, server-side. ES modules, no globals, no SPA framework; Bootstrap 5.3 + small custom CSS layer.

| Module | Responsibilities |
| --- | --- |
| `game.js` | Board rendering from `MoveResponse`s (POST `/api/game/move`); mode/difficulty UI, status, scoreboard, Reset/New match; error-code → message; guest notice |
| `room.js` | Hub connection (after login); JoinRoom/MakeMove/Leave/RequestRematch; §14.2 events → **replace room state wholesale from `RoomState`** (no local merging); reload → `GetRoomQuery` + group rejoin; disconnect countdown; failure → inline messages |
| `theme.js` (+ inline head script) | light/dark/device; pre-paint apply; toggles persist (`ttt:theme`) |
| `sound.js` | Web-Audio tones; mute button; persists (`ttt:sound`) |
| `settings.js` | Settings page ⇄ localStorage; Reset to defaults |

**State rules:** the client owns **no game truth** — boards/turns/results always come from server responses or `RoomState`; only UI state is local (panel visibility, timers, submitting flags). After errors, the authoritative response/broadcast (or `GetRoomQuery`) repairs the UI. **Error UX:** every §12.2 code has exactly one message; unknown → "Something went wrong. Please try again."; offline → "You are offline — the game will reconnect."

## 17. Configuration & Secrets

| Key | Where | Note |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | user-secrets (dev) / host config (prod) | never committed |
| Serilog sinks/levels | `appsettings.{Env}.json` | `logs/` writability verified at first deploy |
| `RoomSettings` (code length, idle/grace/expiry seconds) | `appsettings.json` defaults | singleton injection |
| Identity lockout/policy options | `Program.cs` defaults | per §6 security |

No secrets in appsettings/code/docs/git; production values entered by the owner on the host.

## 18. Security Controls

| Control | Specification |
| --- | --- |
| AuthN | Identity PBKDF2 hashing; unique username/email; lockout 5/5min (423-equivalent); generic login errors (no user-exists oracle) |
| AuthZ | `[Authorize]` on Rooms/Account/History/Delete + hub; hub group add validated as participant server-side; identity only from the cookie principal — never client payloads |
| Anti-tampering | Server-authoritative engine validation of every move; `SaveMatchCommand` replays move lists; client results never trusted; finished games immutable (E5) |
| CSRF | Anti-forgery tokens on 100% of POST forms and JSON mutations |
| Transport | HTTPS only (Let's Encrypt at host), HTTP/3 + TLS 1.3 |
| Data access | EF parameterized only; no dynamic SQL; migrations-only schema |
| Room integrity | Deconfusable unguessable codes; one active room/player; participant checks at query + hub levels |
| Secrets/logs | user-secrets/host config only; never passwords/tokens/emails in logs; PII limited to accounts + matches |
| Data rights | FR-19 wipes account + matches in one transaction; opponent rows keep the game with "Deleted player" |
| Dependencies | Pinned versions (§2.6 D-17); audit-fix pin `Pkcs`; new package = decision entry |

## 19. Testing Strategy

| Project | Tooling | Scope |
| --- | --- | --- |
| `TicTacToe.UnitTests` | NUnit + Moq + **AutoMock (Autofac.Extras.Moq)** + Shouldly | Engine + handlers, mirrors `Features/` |
| `TicTacToe.IntegrationTests` | `WebApplicationFactory`, real test DB, SignalR .NET client | Endpoints + hub (2 clients) + DB effects |
| `TicTacToe.E2ETests` | Playwright for .NET | Two-browser online flows; a11y spot checks |

Conventions (MaleFashion style): AAA · `MethodName_Scenario_ExpectedResult` · `[OneTimeSetUp]/[SetUp]/[TearDown]/[OneTimeTearDown]` · Moq `Setup/VerifyAll` · Shouldly (`ShouldBe…`, `Should.ThrowAsync<T>`) · unit tests mock DB/network · **never delete/weaken tests**.

**What must pass:** engine tests (all 8 lines × both players, draws, illegal/out-of-range/post-game moves, X-first alternation, Easy seeded-random, Medium win-take/block, Hard **never-loses exhaustive sweep**) at 100% coverage · every §12.1 use case incl. its error codes (forged saves rejected; both-row single-commit; join-race semantics; rematch both-required; delete cascade; fake `TimeProvider` timing) · room store (unique codes, one-active-room, expiry w/ fake time) · hub integration (2 clients: MoveApplied/PlayerDisconnected/Reconnected/RoomClosed flows + §14.3 races live) · endpoint integration (anti-forgery required; route auth matrix; status mapping; per-user match isolation) · E2E (register×2 → create → join → win → both histories; local + computer; guest redirect; reload-rejoin).

**Quality gates:** `dotnet build` zero warnings · `dotnet test` all green · engine 100% · `dotnet format --verify-no-changes` · manual two-browser session per milestone · §9 a11y gates.

## 20. DevOps, CI & Deployment Readiness

- **CI (GitHub Actions, PR + main):** restore → build (warnings-as-errors) → all tests. No Docker required by the runner.
- **Local:** SQL Server Express + SSMS; `dotnet user-secrets` connection string; `dotnet ef database update --project src/TicTacToe.Infrastructure --startup-project src/TicTacToe.Web`; `dotnet run --project src/TicTacToe.Web`.
- **Host (verified §2.6 D-18):** .NET 10 ✓ · SQL Server 2025 + SSMS ✓ · SignalR/WebSockets/MVC ✓ · HTTPS/Let's Encrypt/HTTP3 ✓ · free plan 256 MB RAM / 1 GB DB / no email ⇒ **one instance, never scale out** (rooms in memory).
- **Deployment (owner, manual, M7):** `dotnet publish -c Release` + publish profile; Web Deploy/FTP; owner applies the recorded migrations path; owner enters prod secrets; **agents never deploy**; verify `logs/` writability (fallback: console + host logs). `spikes/SignalRCheck` was the M0 throwaway proof (deleted after use).

## 21. Roadmap (M0 → M7)

| Milestone | Goal | Exit ("Done when") |
| --- | --- | --- |
| M0 Setup & hosting check ✅ | skeleton, tests, CI, SQL Express, spike | builds · CI green · pins recorded · spike verified |
| M1 Engine ✅ | `Domain/Engine` | engine tests 100% coverage; purity test |
| M2 Local game ✅ | page + `PlayMoveQuery` + endpoint | local PvP playable; restart; highlight; handler/endpoint tests |
| M3 Accounts & results ✅ | Identity + Matches + stats/history + account ops | register/login/logout; forge-proof saves; totals + paged history; delete cascade |
| M4 Online multiplayer ✅ | rooms, hub, lobbies, disconnect flows | two-browser E2E; join race; leave-forfeit; 60 s grace + 10 min expiry; both-row saves |
| M5 Computer opponent ✅ | Easy/Medium/Hard + selector | AI tests incl. minimax sweep; ≤ 500 ms reply |
| M6 Theme/sound/settings/guests/a11y ✅ | polish layer | no-flash theme; sounds + mute; guest flows; a11y gates |
| M7 Verification & deploy prep (in progress) | **verify v1.0 spec end-to-end; fix deviations; deploy artifacts** | full §19 gates; Lighthouse ≥ 90/85; publish profile + migration script + deploy notes; docs match shipped behavior |

> Statuses reflect the prior `TASKS.md`. Since v1.0 is now canonical, M7 includes a **verification sweep**: walk FR-01…FR-31 + every US scenario against the live app; deviations are fixed in code (or, if genuinely better, re-recorded here first).

## 22. Risks

| Risk | Mitigation |
| --- | --- |
| Agent drift | This file canonical; per-task ACs; tests gate merges; human review per milestone |
| Over-engineering | Non-goals; thin repositories; review gates |
| Regressions | suites green before commits; DoD checklists |
| Host limits/SignalR | verified M0; host-neutral stack |
| Room loss on restart | accepted single-node trade-off; revisit on evidence |
| Real-time races | per-room lock + §14.3 + integration tests |
| Tampered results | server replay + engine validation |
| Free-plan RAM | ~200 B/room ≪ 256 MB; monitor |
| Scope creep | ideas → V2 only |

## 23. Future Scope (V2 — explicitly not in v1)

Ordered by value; each is **additive** to existing seams (no rewrites): 1) rematch/series polish + friends leaderboard · 2) **Quick match** (`MatchmakingService` wrapping Create+Join) · 3) **ELO ratings** from existing `Matches` + leaderboard query · 4) more games via an `IGameEngine` abstraction (Connect Four first) · 5) accounts v2: email confirmation/reset (premium plan), social login · 6) branding/logo/favicon · 7) ops hardening: Dockerized SQL option, deploy-approval automation, richer `/health`.

## 24. Assumptions

- Rooms live in **memory only** for v1 (restart ends in-progress games); SQL persists finished matches only. Single instance forever in v1.
- The hub is a **thin command gateway**: every mutation goes through Cortex.Mediator commands; the hub broadcasts command results only (never mutates outside the store's lock).
- Computer reply delay (300–500 ms) is a product-feel choice; the engine is instantaneous.
- Sounds are generated (Web Audio), not files — zero licensing; acceptable quality bar.
- Display names are unique ⇒ no player disambiguation anywhere.
- `Moves` strings are replayable (≤ 9 indices); no separate move-rows table in v1.
- HTTPS terminates at the host; no in-app TLS.
- Hosting-plan figures as verified 2026-11-06; owner re-checks before deployment.
- All UI strings are the literal copies from §12.2/§9; localization is V2.

*End of document — canonical specification for Tic Tac Toe v1.0.*
