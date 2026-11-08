# Pages & Flows

*Page-by-page wireframes (desktop + mobile), element-level specs, user journey flows, state matrices, responsive behavior and accessibility annotations for every page of the Tic Tac Toe app.*

Parent document: [PRD](../00-product/PRD.md) §5 · Related: [UX/UI Design System](ux-ui-design-system.md) · [Frontend Architecture](frontend-architecture.md) · [Animations](animations.md) · [CQRS Use Cases](../02-backend/cqrs-use-cases.md)

---

## 1. Route Map

| Route (controller action) | Page | Access | Guard |
| --- | --- | --- | --- |
| `/` | Game (local + vs computer) | Public | — (guests and signed-in all open the game here) |
| `/Account/Login` | Login | Public | Guest only (signed-in → `/`) |
| `/Account/Register` | Register | Public | Guest only (signed-in → `/`) |
| `/Home` | Home (online list + mode select) | Authenticated | Server `[Authorize]` |
| `/Rooms/{id}` | Room game | Authenticated + participant | Server checks room membership; non-players → 404 |
| `/History` | History + stats | Authenticated | Server `[Authorize]` |
| `/Settings` | Settings (sound, theme) | Public | — (browser-only, guests included per FR-22) |
| `/Account` | Profile (change password, delete account) | Authenticated | Server `[Authorize]` |
| `*` | Not Found | Public | none |

Routing is server-side (MVC controllers) — there is no SPA router; the hub is the only client routerless channel ([frontend-architecture.md](frontend-architecture.md)).

---

## 2. Login (`/Account/Login`)

### 2.1 Desktop wireframe (≥ 768 px) — split layout

```
+===========================================================================+
|  LEFT — BRAND PANEL (55%, full viewport height)     RIGHT — FORM (45%)    |
|                                                  vertical center, the     |
|   X    ~drifting marks~        O                  form card sits on the   |
|        (sky-400)      (rose-400)                  page background         |
|                                                     +------------------+  |
|        Tic-Tac-Toe   Play a real person.            |  Welcome back    |  |
|        Right now.                                   |  text-text-muted |  |
|                                                     +------------------+  |
|   O                    X                            | LABEL  Username  |  |
|                                                     |        or email  |  |
|                                                     | [______________] |  |
|                                                     | LABEL  Password  |  |
|                                                     | [___________][eye]|  |
|                                                     | [x] Remember me  |  |
|                                                     | [  Sign in     ] |  |
|                                                     | ---------------- |  |
|                                                     | No account?      |  |
|                                                     | > Register       |  |
|                                                     +------------------+  |
+===========================================================================+
```

### 2.2 Mobile wireframe (360 px) — single column

```
+-------------------------------+
| BRAND HEADER (compact)        |
|  X  Tic-Tac-Toe  O            |
|  Play a real person.          |
|  Right now.                   |
+-------------------------------+
| FORM CARD (p-4)               |
|  Welcome back                 |
|  ---------------------------- |
|  Username or email            |
|  [_________________________]  |
|  Password                (eye)|
|  [______________________][eye]|
|  [x] Remember me              |
|  [        Sign in         ]   |
|  ---------------------------- |
|  No account? Register         |
+-------------------------------+
```

### 2.3 Annotated regions

| Region | Component(s) | Description |
| --- | --- | --- |
| A — Brand panel | page-local | 55 % width (desktop only), indigo radial ambience stronger here; 3–4 oversized X/O glyphs (sky-400/rose-400, 96–160 px) drift on 8–12 s loops (disabled under reduced motion). `text-display` "Tic Tac Toe", `text-body-lg` tagline. Purely decorative: `aria-hidden`. Hidden below 768 px; replaced by a compact header with two 28 px marks flanking the wordmark. |
| B — Form card | Card + Input ×2 + Button lg + ghost link | `max-width: 448px`; heading `text-h1` "Welcome back" + sub "Sign in to keep playing" (`text-meta`). |
| C — Footer link | Ghost link | "No account? **Register**" — underlined indigo link-button; focus ring per system §9. |

### 2.4 Element spec

| Element | Spec |
| --- | --- |
| Username/email input | Label above + input; error below in red-400 text + red border ([error-handling.md](../02-backend/error-handling.md) `Auth.InvalidCredentials`) |
| Password input | Eye toggle (show/hide); empty submit shows "Password is required." |
| Remember me | Checkbox, custom styled, label always adjacent (never placeholder-only labels) |
| Sign in | Primary, full width, spinner while submitting (label "Signing in…"); on failure the password clears, username stays |
| Lockout state | After 5 failed attempts: locked message + disabled button for 5 minutes, countdown in `text-mono` |

---

## 3. Register (`/Account/Register`)

### 3.1 Desktop wireframe (≥ 768 px)

```
| LEFT — BRAND (same as Login)          RIGHT — FORM CARD               |
|                                     |  Create your account          |
|                                     |  LABEL Username             |
|                                     |  [______________________]    |
|                                     |  LABEL Email                |
|                                     |  [______________________]    |
|                                     |  LABEL Password    (strength  |
|                                     |  [______________][eye] meter) |
|                                     |  LABEL Confirm password       |
|                                     |  [______________________]    |
|                                     |  [   Create account     ]    |
|                                     |  Has account? > Login         |
```

### 3.2 Mobile — single column, same order as desktop, brand header on top.

### 3.3 Element spec

| Element | Spec |
| --- | --- |
| Password strength | Live meter (weak/fair/strong) under the field — indigo→emerald, updates on input, announced politely |
| Duplicate errors | `Auth.DuplicateUsername` / `Auth.DuplicateEmail` render **under the matching field** (never a page banner) |
| Submit | Primary; on success → redirect `/Home` with success toast "Account created — welcome!" |

---

## 4. Home (`/Home`) — online list + mode select

### 4.1 Desktop wireframe (≥ 1024 px)

```
+-------------------------------------------------------------+
| Nav: logo   Home  History  Settings  [conn badge]  [avatar] |
+-------------------------------------------------------------+
|  ONLINE PLAYERS (left, 2/3)        |  MODES (right, 1/3)  |
|  +----------------------------+  | +------------------+  |
|  | [avatar] Rafi     ●Avail[Invite] |  | [Play vs computer]   |  |
|  | [avatar] Sara     ●Invited[-]   |  | (auto-pick)   |  |
|  | [avatar] Imran    ●In game [-]  |  +------------------+  |
|  | **Waiting for Sara... [Cancel]**|  | [Play vs computer]   |
|  +----------------------------+  |  | [Play local]         |
|  (waiting card w/ 30s bar)       |  | [History]           |
+-------------------------------------------------------------+
```

### 4.2 Mobile wireframe (360 px)

```
| Nav (logo + connection badge)        |
| ONLINE PLAYERS                            |
| [avatar] Rafi      ●Available [Invite]    |
| [avatar] Sara      ●Invited   [-]         |
| [avatar] Imran     ●In game   [-]         |
| [Waiting…spinner] [Cancel]                |
| -- MODES -- [Play vs computer] [Play local] |
```

### 4.3 Annotated regions & element spec

| Region | Component(s) | Description |
| --- | --- | --- |
| A — Player row | Avatar + name + StatusDot/Badge + Invite button | Invite disabled with tooltip unless status = Available; "Invited"/"In game" rows are read-only (dot + label) |
| B — Waiting card | Card + spinner + live-draining 30 s progress bar + Cancel | Shows while an outgoing invitation is pending; auto-hides on `InvitationAccepted/Declined/Expired/Cancelled` |
| C — Mode cards | Card + icon + title + description + ghost CTA | "Play vs computer" (difficulty select); "Play local" (pass-and-play); "Quick match" is **not** in v1 (auto-matchmaking is out of scope, not scheduled) |
| D — Connection badge | Presence dot pulse on hub disconnect | Amber "Reconnecting…" on auto-reconnect, emerald "Connected" otherwise |

### 4.4 Invite behavior

Click **Invite** → `SendInvitationCommand` (hub `InvitePlayer`) → waiting card shows immediately (the **sender never waits for the response toast**) → `InvitationReceived` arrives on the invitee's popup → outcomes: Accepted (both navigate to room), Rejected/Expired/Cancelled (waiting card clears + notice).

---

## 5. Room game (`/Rooms/{id}`)

### 5.1 Desktop wireframe (≥ 768 px)

```
+--------------------------------------------------------------+
| Nav                                                              |
+--------------------------------------------------------------+
|  [You: X sky]   vs   [Rafi: O rose]        status: Your turn|
| --------------------------------------------------------------|
|        [   ][   ][   ]                                        |
|        [ X ][   ][ O ]      board card 560px, 3x3, gap 8     |
|        [   ][ O ][   ]                                        |
+--------------------------------------------------------------+
|  Grace countdown ring (when opponent is offline)              |
|  [Leave game]    [Rematch (after finish)]                    |
+--------------------------------------------------------------+
```

### 5.2 Mobile wireframe (360 px) — same order, single column, board full width minus padding, score header sticks at top of scroll.

### 5.3 Annotated regions

| Region | Component(s) | Description |
| --- | --- | --- |
| A — Score header | Avatar+name+mark per side, turn indicator (`text-h2` "Your turn" / "Rafi's turn") | X side always sky, O side always rose; current turn glows |
| B — Board | GameBoard & Cell per system §7.11 | 64 px+ cells; ghost preview on hover; keyboard: arrow keys + Enter (cells are buttons) |
| C — Grace banner | Countdown ring + `text-mono` seconds + "Rafi is reconnecting…" | Appears only on `PlayerDisconnected`; hidden on `PlayerReconnected` |
| D — Result modal | Modal per §7.7: "You win!" / "Draw" + confetti burst (win) + actions: Rematch (primary), Leave (ghost) | On `GameFinished` inside the room; rematch needs both-ready |

### 5.4 Move interaction detail

Click cell → board waits for the server → authoritative `RoomState` replaces the board (never merges) and the mark-draw animation plays → on `Game.AlreadyOver`/illegal, cell shakes 300 ms + error toast with the exact message from `error-handling.md`.

### 5.5 Result modal

| Outcome | Content |
| --- | --- |
| Win | "You win!" + confetti burst + win jingle + [Rematch] [Leave] |
| Loss | "Rafi wins." + lose tone + [Rematch] [Leave] |
| Draw | "It's a draw." + neutral tone + [Rematch] [Leave] |

---

## 6. History & stats (`/History`)

Desktop: stat chips row (Played / Won / Lost / Win %) + table (result badge, opponent, mode, date). Mobile: same chips, table collapses to stacked rows. Empty state per system §8. Numbers count up on load (plain JS, 400 ms).

## 7. Settings (`/Settings`) + Profile (`/Account`)

- **Settings:** sound on/off (Web-Audio synthesized — no audio files), theme toggle (`data-bs-theme` dark/light, persisted in the browser via `localStorage`, no server round-trip), reduced-motion note. Works for guests (FR-22).
- **Profile:** change-password form (current + new, per-field errors), delete-account danger zone: warning card → confirm modal (type-your-username to enable Delete) → on success, signed out + toast.

## 8. Not Found

"404 — this page doesn't exist." + ghost link "Back home". Icon: magnifier. Same tokens as auth pages.

---

## 9. User Journey Flows

### 9.1 Visitor → Register → Home (US-01, FR-01…FR-05)

```
Visitor opens / → Game page (local + vs computer)
  → Register (username/email/password/confirm, strength meter, duplicate errors inline)
  → success → /Home, toast "Account created — welcome!", presence connects
```

### 9.2 Invite → Accept → Play (US-15, FR-32…FR-36)

```
Player A invites Available B (hub InvitePlayer)
  → A: waiting card + 30 s bar
  → B: popup Accept/Reject (on any page, 30 s drain bar)
  → B Accept → atomic room creation → both land on /Rooms/{id}, A = X
  → B Reject → A sees "[name] declined." + 30 s cooldown from re-invites
  → 30 s no answer → A: "No answer — the invitation expired."
```

### 9.3 Play → Finish → Rematch (FR-26…FR-31)

```
Turn alternates, ghost preview, mark draws → win/draw viewed in result modal
  → Rematch: both players ready → board resets, X starts
  → Leave mid-game: confirm modal "Leave counts as a loss. Continue?" → forfeit
```

### 9.4 Disconnect → Reconnect → Resync (FR-29)

```
Socket drops → banner/countdown ring + "Reconnecting…" badge
  → reconnect < 60 s → PlayerReconnected, banner hides, board resyncs from GetRoomQuery
  → grace expires → RoomClosed "Opponent forfeited" + result modal (You win)
```

---

## 10. Page State Matrix

| Page | Loading | Empty | Error | Success |
| --- | --- | --- | --- | --- |
| Home | Skeleton rows (1.5 s shimmer) | §4.3 empty state | Toast + retry ghost | — |
| Room | Skeleton cells (GetRoomQuery) | — (404 on unknown id) | Shake + toast w/ exact message | Mark draw animation |
| History | Skeleton table | §8 empty state | Toast + retry | Count-up stats |

## 11. Responsive Behavior

| Breakpoint | Change |
| --- | --- |
| ≥ 1024 px | Two-column Home; brand panel on auth pages; board right of score header |
| 768–1023 px | Single column Home; board stacked under header |
| < 768 px | Auth tilt to compact header; invitation popup becomes bottom sheet; board full-width; nav collapses to logo + badge + avatar |
| All | No horizontal scroll at 360 px; touch targets ≥ 44 px |

## 12. Accessibility Annotations per Page

| Page | Key annotations |
| --- | --- |
| Login/Register | Errors as `aria-describedby` on inputs; strength meter `aria-live`; eye toggle labeled |
| Home | Status badges announce changes politely; Invite buttons labeled with the opponent's name |
| Room | Board grid `role="grid"`, cells `role="gridcell"` + `aria-label` ("Row 1, column 2, empty"); turn/moves via `aria-live`; countdown announced every 10 s, not every second |
| Popups/Modals | Focus trapped; Esc dismisses; `aria-modal` + labeledby |

## Assumptions

- The waiting-card and popup progress bars **approximate** the server TTL (they never drive UI decisions) — the server result always wins; they only tell the user "don't wait forever."
- Routes are MVC controller actions (server-rendered); there is no client router — deep links work by URL alone.
- All page/server copies come verbatim from `02-backend/error-handling.md`; frontend never invents error wording.
- Sounds and confetti are progressive enhancement; with JS disabled, the server still plays and validates correctly.
- This file governs layout and flows; tokens/components are `ux-ui-design-system.md`, JS modules are `frontend-architecture.md`, motion is `animations.md`.
