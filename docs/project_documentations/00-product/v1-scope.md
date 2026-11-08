# V1 Scope

*The hard boundary of version 1.0 — one page that answers "is this in v1?" for every idea, before any code is written.*

**Related docs:**
- Full requirement wording → [`PRD.md`](PRD.md) (§5)
- Stories → [`user-stories.md`](user-stories.md)
- Delivery slicing → [`roadmap.md`](roadmap.md)

---

## 1. Purpose of this file

Scope discussions die here. The rule:

1. A feature is in v1 **only** if it appears in §3 (in) and traces to at least one **Must/Should** row in [`PRD.md`](PRD.md) §5.
2. Everything §4 (out) is rejected for v1 regardless of implementation cost.
3. New ideas go through [§5](#5-intake-route) — they never get "slipped in".

## 2. Scope pillars

| Pillar | Promise | Verified by |
| --- | --- | --- |
| **Play** | Local, vs-computer and online 3×3 Tic Tac Toe with flawless rules | FR-01…FR-11, FR-25…FR-28 |
| **Persist** | Registered players keep totals + history; guests stay guests | FR-14…FR-20, FR-31 |
| **Fairness** | The server referees everything; clients cannot lie | FR-02/05, FR-25/26, server-replay saves |
| **Reach** | Online players find each other through live presence + invitations, not shared codes | FR-32…FR-36 |
| **Resilience** | Disconnects, forfeits, invitation expiry and races behave by contract | FR-28…FR-30, FR-35, race-safety tests |
| **Polish** | Theme, sound, accessibility, responsive layout | FR-13, FR-21, FR-22, a11y gates |

## 3. In scope (v1.0)

### 3.1 Modes & core game
- 3×3 board, X always moves first, strict alternation
- Local pass-and-play (two humans, one device)
- Vs Computer with **Easy / Medium / Hard** (random / win-take-block / never-losing minimax)
- Online play **by invitation** (registered users only, from a live online-players list), inviter = X
- Win detection on all 8 lines, draw detection, winning-line highlight
- Reset (restarts game, keeps session scoreboard) and New match (zeroes scoreboard)

### 3.2 Accounts & data
- Register / login / logout (Identity, cookies); **no email confirmation step**
- Account page: totals, History link, change password, delete account (password re-entry)
- Every completed match of a logged-in user persisted (mode, difficulty?, symbol, result, moves, UTC time)
- Totals computed from stored matches, never stored separately
- History page: newest-first, paged, opponent display name (or "Deleted player")
- Guest session scoreboard (never touches the database)

### 3.3 Online lifecycle (invitation-based)
- **Presence:** signed-in players hold a hub connection on **every page**; "online" = logged in with the site open
- Lobby shows online registered players (username only) marked **Available / Invited / In game**, updating live
- Invite an Available player → "Waiting for [name]…" with Cancel; at most **one outgoing invitation** per player
- Invitee gets an **Accept/Reject popup on any page**; Accept creates the room atomically and both land on the board, inviter = X
- Guards: cannot invite self / offline / In-game / Invited players; **~30 s rejection cooldown** per re-inviting the same player
- Unanswered invitations expire after **~30 s**; either party going offline cancels a pending invitation
- Real-time moves via SignalR, server-authoritative, broadcast to both players
- Rematch inside the same room after both players press "Play again"
- Leave anytime: mid-game = loss for the leaver; both rows saved once
- ~60 s disconnect grace with visible countdown; expiry = win for the connected player
- No idle-room expiry: rooms exist only while played; both-players-gone rooms are closed by cleanup without a result

### 3.4 Presentation & settings
- Light / dark / device-default theme, applied pre-paint (no white flash)
- Synthesized Web-Audio sounds with visible mute; persisted
- Settings page: Theme × Sound only, plus "Reset to defaults"
- Responsive layout down to 360 px; keyboard-operable board; WCAG 2.1 AA

### 3.5 Engineering
- Automated coverage is **unit-only** (owner decision 2026-11-07 — no integration or E2E suites); real-server flows (cookies, hub negotiate, two-player races) are verified by documented manual walkthrough scripts per [`../05-quality/test-strategy.md`](../05-quality/test-strategy.md)
- GitHub Actions CI; manual owner deployment to MonsterASP.NET

## 4. Out of scope (v1.0) — with reasons

| Excluded | Reason | Status |
| --- | --- | --- |
| Room codes / join-by-code / invite links | **Superseded 2026-11-07** by invitations ([PRD §4.3](PRD.md)); keeping both flows doubles state for one persona job | never, without a PRD changelog decision |
| Chat, friends, spectator mode | Social layer needs identity/profile work beyond v1 goals (invitations are deliberately friend-free — invite anyone online) | out of v1, not scheduled |
| ELO / ratings / leaderboards | Requires calibrated matchmaking data | out of v1, not scheduled |
| Quick match | Superseded in spirit by invitations (invite any online player directly); auto-matchmaking is **not** in v1 | out of v1, not scheduled |
| Tournaments, game clocks | Product surface grows without proving more engineering | out of v1, not scheduled |
| Mobile / desktop apps | Web covers all personas; native adds a second frontend | not planned |
| Board sizes ≠ 3×3, >2 players | The engine seam exists, but no v1 requirement does | out of v1, not scheduled |
| Email confirmation / reset / social login | Free host has no email service | out of v1, not scheduled |
| Branding / logo | Name and plain wordmark ship v1 | out of v1, not scheduled |
| Monetization / admin dashboard | No operator scale to manage | not planned |
| "Default difficulty" or "play as X" settings | Persona research showed no demand; FR-12 stays Could | accepted idea |

## 5. Intake route

```
Idea → new feature? → propose in PRD changelog as Could
     → only upgrade to Must/Should before v1.0 if a persona story fails without it
     → out of v1 scope entirely otherwise (no future-scope list is maintained)
```

Changes to this file appear in [`PRD.md`](PRD.md) §9 changelog with date and reason.

## Assumptions

- "In scope" rows are claims about *behaviour*, verified by the test plan — not a checklist of files to write.
- Host constraints (single instance, in-memory rooms, no email) are durable until the owner changes hosting plan; scope items dependent on them (§4 rows) stay out until then.
- Scope changes after 2026-11-06 require a changelog entry in [`PRD.md`](PRD.md) §9 — silent scope changes are defects in the docs.
