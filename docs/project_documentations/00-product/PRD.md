# Product Requirements Document (PRD)

*Defines what Tic Tac Toe v1.0 must do and why — the product contract every other doc and every test traces back to.*

**Related docs:**
- Scope boundaries → [`v1-scope.md`](v1-scope.md) · future work is out of scope until a PRD revision schedules it
- Stories with full acceptance criteria → [`user-stories.md`](user-stories.md)
- Delivery plan → [`roadmap.md`](roadmap.md)
- Origin/history → [`original-technical-documentation.md`](original-technical-documentation.md)
- Technical baseline → [`../01-architecture/system-architecture.md`](../01-architecture/system-architecture.md)

**Stable IDs:** requirements are referenced everywhere as `FR-XX` and stories as `US-XX`. Adding, removing or renumbering a requirement is a product decision recorded in this file's revision note — never in code comments.

---

## 1. Vision

A polished, friendly Tic Tac Toe that anyone can open and play instantly: two players on one device, a solo player against the computer, or two friends online via invitations — no codes, no links — clean rules, fair server refereeing, saved histories for logged-in players, and an interface that looks professionally designed.

Everything about the product is judged against one sentence: **it should feel small, fast and finished — never a tutorial project.**

## 2. Problem & Goals

### 2.1 Why this product

Tic Tac Toe is universally known: zero onboarding cost, well-defined rules with sharp edge cases (alternation, immutability of finished boards, simultaneity races) that expose real engineering skill. It is deliberately chosen as the proving ground for a repeatable AI-driven build process:

```
PRD → tasks → code → tests → review → human sign-off per milestone
```

### 2.2 Goals

| # | Goal | Evidence this doc provides |
| --- | --- | --- |
| G-1 | Ship a polished, bug-free, deployment-ready web app | §6 functional requirements + §7 quality bar |
| G-2 | Online multiplayer built *completely*, not demo-grade | FR-25…FR-29, FR-32…FR-36 (invitations, rooms, disconnects, forfeits, saves) |
| G-3 | Registered players' results persist across sessions | FR-15…FR-17 |
| G-4 | Validate the AI workflow end-to-end with timeboxed human effort | §8 success metrics, only milestone reviews are human |
| G-5 | Keep the codebase small, readable and well-tested | Covered by the in-line conventions in [`../01-architecture/system-architecture.md`](../01-architecture/system-architecture.md) (§5 — logging, errors, mapping, persistence, SOLID) and the testing gates in [`../05-quality/test-strategy.md`](../05-quality/test-strategy.md) |

### 2.3 Non-goals (v1)

Anything below is out of scope for v1 — proposals are recorded and rejected for v1, not parked in a V2 plan:

- Social features: chat, friends, spectator mode, ratings/ELO, quick match, leaderboards, tournaments, game clocks/timers · private/password-protected rooms
- Platforms beyond the web app (mobile or desktop apps, CLI)
- Board sizes other than 3×3; more than 2 players per game
- Settings beyond Theme and Sound (e.g. "default difficulty" was rejected — see §5 note on FR-12)
- Branding/logo work (name stays "Tic Tac Toe" for v1)
- Email confirmation, password reset email, social login (the free hosting plan has no email service)
- Join-by-code rooms and invite links — **superseded 2026-11-07** by the invitation system (§4.3; FR-23/FR-24 marked superseded)
- Monetization or an admin dashboard

## 3. Personas

| Persona | Profile | Want | Pain to avoid |
| --- | --- | --- | --- |
| **Sara — casual player** | Faces the game page with no account, often on a phone | Open the page and play immediately — local pass-and-play or vs the computer | Any signup wall before the first move |
| **Rafi — returning player** | Registers once, comes back over days | Totals and full history still there; accepts online invitations from friends | Losing his record because the tab was closed |
| **Nadia — opponent on another device** | Sara's or Rafi's friend, accepts an invitation | Fast join, see opponent moves instantly, fair outcome even if *she* loses connection | Losing a winning game to a network hiccup |
| **Owner — MD. Mahid Hasan Shesir** | Developer/deployer, sole operator | Clean, tested, documented app; a hand-actionable, ≤ 30 min manual deployment | Surprises at deploy time (websockets, DB, secrets) |

## 4. Product scope

### 4.1 Game modes

| Mode | Target user | Tech surface | Saves |
| --- | --- | --- | --- |
| Local pass-and-play (X vs O on one device) | Sara | Game page, JS + server engine | Logged in: match row via Razor POST (`SaveMatchCommand`) · guest: session score only |
| Vs Computer (Easy / Medium / Hard) | Sara, Rafi | Same page; server engine computes reply | Same as local |
| Online by invitation | Rafi + his friend | Presence list, in-page invite popup, SignalR room | Both players get one match row each, in one transaction |

### 4.2 Accounts & data

- Registration/login/logout via ASP.NET Core Identity; **no email confirmation** — account is usable immediately after registering.
- Account page: win/loss/draw totals, link to History, change password (current password required), delete account (password re-entry + one confirm).
- History page: the user's finished matches, newest first, paged, opponent's name shown.
- Guests can play local and vs-computer with a *session* scoreboard; guests never reach the online lobby (redirect to login with a clear message).

### 4.3 Online play (invitation-based — revised 2026-11-07)

Online play is **invitation-driven**: a player invites a friend who is currently online, the friend accepts from a popup that reaches them on any page, and the server creates the room at the moment of acceptance — there are no room codes to type and no invite links to share.

- **Presence:** every signed-in player keeps a SignalR connection open on every page of the site. "Online" means *logged in with the site open* (≥ 1 live connection), not "on the Lobby page". The Lobby lists all online registered players — username only, never email — each marked **Available**, **Invited** (pending invitation in either direction) or **In game**; the list updates live with no refresh.
- **Invite:** an Available player receives an invitation; the inviter sees "Waiting for [name]…" with a Cancel action. A player holds **at most one outgoing invitation** at a time.
- **Accept / reject:** the invited player gets an Accept/Reject popup wherever they are. On Accept the server creates the room in one atomic step and both players land directly on the board, **inviter = X**, game started. On Reject the inviter sees "[name] declined." and cannot re-invite that player for **~30 s** (anti-spam cooldown).
- **Expiry:** an unanswered invitation expires after **~30 s** and the inviter is told; if a party goes offline while the invitation is pending, it is cancelled with a clear message.
- **Invite guards:** you cannot invite yourself, an offline player, a player who is In game or Invited, or a player currently on a rejection cooldown.
- **Play** is unchanged from the original design: server-authoritative moves over SignalR; the connect grace is **~60 s** with a visible countdown; leaving mid-game counts as a loss; results are saved once per player in one transaction ([FR-26/28/29](#52-online-play)).
- **Rematch** is unchanged: both press "Play again" and the board resets in the same room, X first ([FR-27](#52-online-play)).
- **No idle-room expiry:** rooms are created only at acceptance and never wait empty — the old ~10-minute unused-room rule is gone. An orphaned room (both players disconnected past grace) is closed by cleanup without saving a result.

## 5. Functional requirements

> **Priority:** *Must* = absence fails v1.0 · *Should* = ship in v1 unless it threatens a Must · *Could* = accepted idea, not planned (moved to V2 if ever built).
> **Verification form:** each row is a testable sentence; the matching tests are cataloged in [`../05-quality/test-plan.md`](../05-quality/test-plan.md).

### 5.1 Core game

| ID | Requirement | Priority |
| --- | --- | --- |
| FR-01 | An empty 3×3 board is displayed at the start of every game and at every page load of the game screen | Must |
| FR-02 | The game enforces strict X-first alternation; occupied cells never change; illegal or non-alternating moves are rejected **server-side**, regardless of the client that submits them | Must |
| FR-03 | The server detects a win on any of the 8 lines (3 rows, 3 columns, 2 diagonals) and ends the game at the winning move | Must |
| FR-04 | The server detects a draw when all 9 cells are filled with no winner | Must |
| FR-05 | After a game ends, every further move — from any player, via any client path — is rejected; finished games are immutable | Must |
| FR-06 | When a game ends in a win, the winning cells are visually highlighted with both a distinct background and an overlay line through the cells | Must |
| FR-07 | "Reset" restarts the *current* game instantly without a page reload and keeps the session scoreboard | Must |
| FR-08 | The game page offers mode selection (local PvP / vs Computer) and a visible entry point to the online Lobby for registered users | Must |
| FR-11 | The session scoreboard (X wins / Draws / O wins) is tracked for all players including guests and is only reset by "New match" | Must |

### 5.2 Computer opponent

| ID | Requirement | Priority |
| --- | --- | --- |
| FR-09 | Vs Computer difficulty: **Easy** plays uniformly random open cells · **Medium** takes an immediate win, else blocks an immediate loss, else prefers center → corner → any · **Hard** is an exhaustive minimax player that never loses | Must |
| FR-10 | The computer reply is computed by the server engine and applied to the board within 500 ms total, including a deliberate 300–500 ms "thinking" delay during which the board is non-interactive and a thinking status is shown | Should |
| FR-12 | Choosing who (user or computer) plays X — **not planned in v1**; the human is always X | Could |

### 5.3 Accounts & persistence

| ID | Requirement | Priority |
| --- | --- | --- |
| FR-13 | Light/dark theme ("use device setting" included) is switchable from the navbar and Settings, applied to the page **before first paint** (no white flash), and stored in the browser | Must |
| FR-14 | Registration, login and logout with inline per-field duplicate errors; wrong credentials produce exactly one generic error message; too many failures lock the account with a friendly message | Must |
| FR-15 | Every completed match of a logged-in user is saved with mode, difficulty (vs Computer only), the user's symbol, the result from that user's perspective, the replayable move list, and a UTC timestamp | Must |
| FR-16 | Persistent total wins / losses / draws are always **computed from** saved matches (never stored separately and never out of sync with them) | Must |
| FR-17 | The History page lists the user's matches newest-first, paged, with the opponent's name displayed — or "Deleted player" when the opponent account was deleted | Must |
| FR-19 | Deleting the account requires re-entering the password and one confirmation; it deletes the account and **all** match history in one transaction, signs the user out; login with the old credentials fails afterwards | Must |
| FR-20 | The Account page shows persistent totals, links to History, change password and delete account | Must |

### 5.4 Sound & settings

| ID | Requirement | Priority |
| --- | --- | --- |
| FR-21 | Sound effects (move placed, win, lose, draw, remote opponent move) are synthesized with Web Audio (no audio files), default **on**, with an always-visible mute toggle; changes apply immediately and persist in the browser | Should |
| FR-22 | The Settings page exposes exactly Theme (Light/Dark/Device, default Device) × Sound (On/Off, default On) plus "Reset to defaults"; preferences live in the browser, work for guests, and require no server round-trip | Must |

### 5.5 Online play

> **2026-11-07 revision:** the invitation system replaces room codes. FR-23 and FR-24 are **superseded** (IDs reserved) by FR-32…FR-36; FR-30 is amended. FR-25…FR-29, FR-31 carry over unchanged.

| ID | Requirement | Priority |
| --- | --- | --- |
| ~~FR-23~~ | ~~Room creation returning a 5-character code with the creator waiting as X~~ **Superseded 2026-11-07 → FR-32…FR-36 (invitation flow, no codes)** | — |
| ~~FR-24~~ | ~~Join-by-code becoming O with distinct not-found/full/expired messages~~ **Superseded 2026-11-07 → FR-34/FR-35** | — |
| FR-25 | Online moves are sent and shown in real time (SignalR); the server validates turn and legality and broadcasts each result to both players | Must |
| FR-26 | When an online game ends, exactly one match row per player is saved in **one transaction** — never zero, never two per player | Must |
| FR-27 | Rematch: either player presses "Play again"; when **both** have done so the board resets within the same room with X to start; each player sees the other's waiting state | Must |
| FR-28 | A player can leave at any time; leaving a mid-game room awards the opponent the win and saves both rows | Must |
| FR-29 | If a player's connection drops, the opponent sees a **~60 s** grace countdown; a reconnect inside the window cancels it and play continues; expiry awards the win to the connected player | Must |
| FR-30 | A player participates in **at most one active game** (per FR-32 status "In game"); rooms are created **only** by an accepted invitation — no idle-room expiry exists; an orphaned room (both players disconnected past grace) is closed by cleanup **without saving a result** | Must |
| FR-31 | Online play is available to registered users only; guests are redirected to login with a clear explanation and can neither see the players list nor send/answer invitations | Must |
| FR-32 | The Lobby lists every currently-online registered player — username only, never email — marked **Available**, **Invited** or **In game**; the list updates live via SignalR with no page refresh; "online" = logged in with the site open (≥ 1 live hub connection) | Must |
| FR-33 | A logged-in player can invite an **Available** player; while waiting they see "Waiting for [name]…" with a **Cancel** action; a player holds **at most one outgoing invitation** at a time; cancelling clears it immediately for both parties | Must |
| FR-34 | An invited player receives an **Accept / Reject popup on every page** while signed in; on Accept the server creates the room atomically and both players land directly on the board, **inviter = X**, game started immediately | Must |
| FR-35 | Rejecting shows the inviter "[name] declined." and blocks re-inviting that same player for **~30 s**; an unanswered invitation expires after **~30 s** and the inviter is notified; if either party goes offline while pending, the invitation is cancelled with a clear message | Must |
| FR-36 | Invitation guards: a player cannot invite **themself**, an **offline** player, a player who is **In game** or **Invited**, or a player inside a rejection cooldown; a rejected invitation never creates a room | Must |

> **FR-18 note:** the guest rule (guests play local/computer only, nothing they do is written to the database) is expressed through FR-15/FR-31 wording; its ID remains reserved so historical references to FR-18 stay resolvable.

## 6. Non-functional requirements

| Category | Requirement |
| --- | --- |
| Performance | Every page is interactive in < 2 s on broadband; an online move round-trip (click → both boards updated) completes in p95 < 500 ms; a computer reply lands within 500 ms total (per FR-10); each JS module stays ≤ 15 KB; no SPA framework |
| Scalability | On the free host plan the single instance sustains ≥ 100 concurrent rooms and ≥ 200 concurrent authenticated connections; the architecture assumes **exactly one app instance** (active rooms live in memory) |
| Reliability | The server is the only authority on rules and results; two near-simultaneous moves produce exactly one accepted move per turn (per-room locking); client-crafted data can never corrupt a board or a result |
| Security | Identity defaults (hashing, lockout), HTTPS only, anti-forgery on all POSTs, `[Authorize]` on all user-only surfaces incl. the hub, every move/result validated server-side, invitation guards enforced server-side, all DB access parameterized via EF |
| Privacy | Stored personal data is limited to the account (username, normalized email, password hash) and matches; no third-party tracking; full account deletion (FR-19); theme/sound never leave the browser |
| Accessibility | WCAG 2.1 AA: contrast ≥ 4.5:1, complete keyboard operation (arrow keys move board focus, Enter/Space places), `aria-live=polite` announcement of turns/results/connection state, shape+text redundancy (never color or sound alone) |
| Compatibility | Latest 2 versions of Chrome, Firefox, Safari, Edge, plus current iOS/Android browsers |
| Quality | Engine: 100% line coverage · zero console errors on the happy path · zero build warnings · every suite green at milestone exit |
| Maintainability | SOLID, CQRS, constructor DI with an interface per service, minimal comments, structured logging that never contains passwords/tokens/emails; users see friendly errors, never stack traces |
| Operations | Hosting is verified for .NET 10 / SQL Server / SignalR-WebSockets / HTTPS; the documented manual deployment executes in ≤ 30 min by the owner |

## 7. Success metrics

v1.0 is *done* when all of the following hold:

1. Every **Must** requirement in §5 passes an automated or documented manual test.
2. The Hard computer player loses **0 games** out of an exhaustive minimax sweep of all reachable board states.
3. A live two-browser online session: invite → accept → play to a win → **both** signed-in users see the match in their History with the opponent's name.
4. Unit tests prove exactly-once semantics at the store/invitation-manager level: concurrent invocations resolve to one winner, the online finish commits once.
5. Lighthouse on the game page: Accessibility ≥ 90, Performance ≥ 85.
6. `dotnet build` zero warnings; `dotnet test` fully green locally and in CI.
7. Human effort stays limited to milestone reviews plus the documented manual walkthrough scripts ([../01-architecture/adr/0008-nunit-moq-shouldly-testing.md](../01-architecture/adr/0008-nunit-moq-shouldly-testing.md)).

## 8. Known trade-offs accepted in v1

| Trade-off | Consequence | Accepted because |
| --- | --- | --- |
| Active rooms in memory, not persisted | A server restart terminates in-progress online games (finished matches are always safe in SQL) | Single-instance v1; materializes in ~200 B per room on a 256 MB plan |
| No email account recovery | A forgotten password means creating a new account | Free plan has no email service; recovery is V2 |
| One transaction per finished online game | Saves are atomic but the same instance writes them | Simplest correct design at single-node scale |
| **Invitations replace room codes (2026-11-07)** | Two friends can play only when **both are signed in at the same time** — there is no code to share for later joining | Invitations are the simplest honest design; both players being online is the natural social case; presence is cheap (one connection per logged-in player) |

## 9. Requirements changelog

| Date | Change |
| --- | --- |
| 2026-11-06 | v1.0 set frozen as FR-01…FR-31 / US-01…US-14, consolidated from the original docs; FR-12 downgraded to Could; M7 redefined as the verification sweep in [`roadmap.md`](roadmap.md) |
| 2026-11-07 | **Invitations replace room codes.** FR-23/FR-24 superseded by new FR-32…FR-36; FR-30 amended (no idle-room expiry, orphan-room rule); US-09/US-10 superseded by new US-15…US-17 in [`user-stories.md`](user-stories.md); §2.3 and §4.3 rewritten; two decisions recorded (site-wide presence; invitations-only) in [`../01-architecture/adr/0009-invitations-replace-room-codes.md`](../01-architecture/adr/0009-invitations-replace-room-codes.md) |
| 2026-11-07 | **Testing suites reduced to unit + E2E** (owner decision): the standalone integration-test project (`WebApplicationFactory` + SignalR client) is removed from the plan; its coverage was reassigned — endpoint/auth/anti-forgery/error-copy checks moved to E2E, handler-transaction guarantees to unit tests ([`../01-architecture/adr/0008-nunit-moq-shouldly-testing.md`](../01-architecture/adr/0008-nunit-moq-shouldly-testing.md)); milestone exit criteria reworded in [`roadmap.md`](roadmap.md) |
| 2026-11-07 | **Testing reduced to unit only** (owner decision): the E2E/Playwright suite is withdrawn as well; real-server guarantees (auth matrix, anti-forgery, error-copy rendering, hub race manifestations, presence updates) move to documented **manual walkthrough scripts** executed at milestone gates and the M13 sweep ([ADR-0008](../01-architecture/adr/0008-nunit-moq-shouldly-testing.md) §consequences); success metric 4 reworded to unit-level exactly-once semantics |
| 2026-11-08 | **Doc sync (owner decisions):** milestones standardized to **M0–M13** (sweep = M13); `/` = Game page for all (guests + signed-in); `/Settings` is **public** (browser-only, FR-22); `SaveMatchCommand` transported via **Razor POST** (single JSON endpoint stays `POST /api/game/move`); `PlayMove` standardized to **`PlayMoveCommand`** (not Query); **`UserName` is the display name** (no separate `DisplayName` column, regex `^[A-Za-z0-9_]{3,20}$`); board renders **wait-for-server** with no optimistic render; walkthrough results recorded in `docs/project_documentations/walkthroughs/` |

## Assumptions

- The prioritization (Must/Should/Could) above is the **only** scope authority — code and UI deviations from a Must row are defects, not design choices.
- Feature completeness beats feature count for v1: the requirements above are frozen until v1.0 ships.
- Timing decisions now carried by the design: **60 s disconnect grace**, **30 s invitation expiry**, **30 s rejection cooldown**; the 10-minute idle-room expiry was **removed** with room codes. Changing any of them is an engineering rationale recorded in [`../01-architecture/adr/`](../01-architecture/adr/) plus a changelog entry here first.
- Presence, invitations and rooms all live **in memory** behind singletons for v1 (single instance); restart ends pending invitations and in-progress games — finished matches are always safe in SQL.
- All user-facing message copy referenced by FRs lives verbatim in [ADR-0010](../01-architecture/adr/0010-result-pattern-expected-failures.md), [`../02-backend/error-handling.md`](../02-backend/error-handling.md) and [`../04-frontend/ux-ui-design-system.md`](../04-frontend/ux-ui-design-system.md); the PRD defines *behaviour*, those files define *wording*.
