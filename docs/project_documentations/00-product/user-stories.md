# User Stories & Acceptance Criteria

*Every user-visible behaviour of v1.0 expressed as a story with exact, test-scenario-ready acceptance criteria — the bridge between the PRD and the test suite.*

**Related docs:**
- Requirement wording → [`PRD.md`](PRD.md) §5 · scope boundary → [`v1-scope.md`](v1-scope.md)
- Literal error messages → [`../02-backend/error-handling.md`](../02-backend/error-handling.md)
- Screen definitions → [`../04-frontend/pages-and-flows.md`](../04-frontend/pages-and-flows.md)
- How stories map to tests → [`../05-quality/test-plan.md`](../05-quality/test-plan.md)

**Conventions:**
- **Sara** = the host/local player (X), **Rafi** = the online opponent (O/joiner), **Nadia** = a disconnecting opponent.
- Board cells are indices **0–8**, row-major (0 is top-left).
- "Immediately" means without a page reload.
- Every `Game.*` / `Room.*` / `Auth.*` failure code below is defined once in [`../02-backend/error-handling.md`](../02-backend/error-handling.md); stories reference codes, not sentences.
- Unhappy paths are first-class: each story lists them explicitly, because they are where v1.0 differentiates from a tutorial app.

---

| ID | Story | FRs | Complexity |
| --- | --- | --- | --- |
| US-01 | Pass-and-play locally | FR-01, FR-02, FR-11 | S |
| US-02 | See wins, draws and the winning line | FR-03, FR-04, FR-06 | S |
| US-03 | Beat (or lose to) the computer | FR-08…FR-10 | M |
| US-04 | Reset or start over | FR-07, FR-11 | S |
| US-05 | Register, sign in, sign out | FR-14 | M |
| US-06 | Keep my record | FR-15…FR-17 | L |
| US-07 | Play as a guest | FR-11, FR-18-note, FR-31 | M |
| US-08 | Delete my account | FR-19 | M |
| US-09 | ~~Create an online room~~ **superseded 2026-11-07 → US-16** | ~~FR-23, FR-30~~ | — |
| US-10 | ~~Join a friend's room by code~~ **superseded 2026-11-07 → US-16/US-17** | ~~FR-24, FR-30, FR-31~~ | — |
| US-11 | Play a full online game | FR-05, FR-25, FR-26 | L |
| US-12 | Survive leaving and disconnects | FR-28, FR-29 | L |
| US-13 | Rematch in place | FR-27 | S |
| US-14 | Comfort: theme, sound, settings | FR-13, FR-21, FR-22 | S |
| US-15 | See who's online (presence list) | FR-32, FR-31 | M |
| US-16 | Invite a player and start playing | FR-33, FR-34, FR-30 | M |
| US-17 | Decline, cancel, expire and cooldown | FR-35, FR-36 | M |

---

## US-01 — Pass-and-play locally

**As a** visitor, **I want** to play Tic Tac Toe with a friend on one device, **so that** we can play instantly without accounts.

**Acceptance criteria (happy):**
```gherkin
Given the empty board on the game page
When Sara plays cell 4 (X opens)
Then the X appears in that cell and the status reads "O's turn"
And when Rafi then plays cell 0, the O appears and the status reads "X's turn"
```

**Unhappy paths:**
```gherkin
Given a board where cell 0 holds an X
When a client submits another move into cell 0 (click, replay, or crafted request)
Then the server rejects it with Game.CellOccupied and the board is unchanged

Given it is O's turn
When a client submits X's move
Then the server rejects it with Game.NotYourTurn and nothing is broadcast or saved
```

**Also covers:** every illegal submission returns an error code; the session scoreboard is untouched by rejected moves.

## US-02 — See wins, draws and the winning line

**As a** player, **I want** the game to tell me clearly when and how it ends, **so that** the result is never ambiguous.

**Acceptance criteria (happy):**
```gherkin
Given a board with X holding cells 0 and 1
When X plays cell 2
Then the status reads "X wins!" and cells 0, 1, 2 are highlighted
And a winning line overlay connects them within 300 ms

Given a full board with no winning line
Then the status reads "It's a draw" and no cells are highlighted
```

**Unhappy paths:**
```gherkin
Given any finished game (win or draw)
When a further move is submitted by anyone through any path
Then the server rejects it with Game.AlreadyOver and the final display is unchanged
```

## US-03 — Beat (or lose to) the computer

**As a** solo player, **I want** three honest difficulty levels, **so that** the machine feels fair and never cheats.

**Acceptance criteria (happy):**
```gherkin
Given difficulty Hard
When the solo player plays any legal sequence
Then the computer never loses and replies within 500 ms including a visible 300–500 ms thinking delay

Given difficulty Easy and the same seeded random source
When the computer chooses
Then it plays uniformly among open cells

Given difficulty Medium and one cell completing the computer's own line
When the computer chooses
Then it takes the win
And when instead one cell completes the human's line, it blocks
```

**Unhappy paths:**
```gherkin
Given the computer is thinking (delay in progress)
When the human tries to play
Then the board is non-interactive and no request is accepted
```

**Also covers:** the human is always X (see FR-12); difficulty is selectable before a match and respected by every move.

## US-04 — Reset or start over

**As a** player, **I want** quick restarts, **so that** a finished match doesn't cost me navigation.

**Acceptance criteria (happy):**
```gherkin
Given any game state (mid-game or after a result)
When Sara clicks Reset
Then the board empties immediately, no page reload occurs, the winner's line vanishes

Given the same moment
When she clicks New match instead
Then the board empties AND the session scoreboard zeroes
```

**Unhappy paths:**
```gherkin
Given a mid-game reset scenario
When Reset is used mid-game
Then no match is saved automatically (saving happens only on finished games, per FR-15)
```

## US-05 — Register, sign in, sign out

**As a** visitor, **I want** my own account, **so that** my results survive.

**Acceptance criteria (happy):**
```gherkin
Given the registration form with a fresh username, email and valid password
When Rafi submits it
Then he is signed in immediately (no email step) and lands where he intended

Given a signed-in Rafi
When he logs out
Then all user-only links disappear and the session score is treated as a guest's
```

**Unhappy paths:**
```gherkin
Given an already-registered username
When someone registers with it
Then the exact inline error for that field appears and nothing is created
  (separate errors: username taken / email taken — no generic blur)

Given wrong credentials
When Rafi attempts to log in
Then he sees exactly one generic failure message;
  after the configured lockout threshold attempts, the lockout message appears
```

## US-06 — Keep my record

**As a** returning player, **I want** everything I finished saved and counted, **so that** history is trustworthy.

**Acceptance criteria (happy):**
```gherkin
Given a signed-in Sara finishes a local match, winning
Then exactly one match row is saved: mode Local, symbol X, result Win, replayable move list, UTC timestamp
And her Account totals show the new totals correctly (computed from rows)

Given the same match in History view
Then it shows newest-first with opponent name when applicable, and pages for large lists
```

**Unhappy paths:**
```gherkin
Given a signed-in Sara replays a finished game's cells and clicks the save path
  with a forged move list / forged result (client-crafted POST)
When the server replays the received move list
Then it rejects with Game.ForgedResult and writes nothing

Given Sara is not signed in
When a save attempt occurs (crafted or leftover)
Then it is rejected as unauthenticated; guest data never reaches the database
```

**Also covers:** deleting the opponent later flips the opponent display to "Deleted player" without touching Sara's results (US-08).

## US-07 — Play as a guest

**As a** guest, **I want** to play local and vs-computer freely, **so that** I only create an account when I want online play.

**Acceptance criteria (happy):**
```gherkin
Given a fresh visitor session with no account
When Sara plays local matches or vs-computer
Then the session scoreboard works and updates, and every save is refused before the database

Given that same guest clicks "Play online"
Then she is redirected to login with "Login is required to play online."
```

**Unhappy paths:**
```gherkin
Given a guest somehow reaches a room URL directly
When the page loads
Then the redirect/authorization rejects them identically to the lobby path
```

## US-08 — Delete my account

**As a** user, **I want** to remove myself completely, **so that** my data doesn't outlive me.

**Acceptance criteria (happy):**
```gherkin
Given the delete page with the correct password re-entered
When Sara confirms (single explicit confirm button)
Then her account and all her match rows are deleted in one transaction
And she is signed out immediately; the old credentials now fail to log in

Given her opponent Rafi's History
Then past matches with Sara still exist and the opponent column reads "Deleted player"
```

**Unhappy paths:**
```gherkin
Given the password field holding the wrong password
When Sara clicks Confirm
Then no account, no match row and no history entry changes and the inline error appears

Given two near-simultaneous delete requests for the same user
Then exactly one succeeds and the second fails coherently (no partial deletion)
```

## US-11 — Play a full online game

**As an** online player, **I want** the board to feel like a shared table, **so that** the game is real.

**Acceptance criteria (happy):**
```gherkin
Given a Playing room
When Sara plays an empty cell on her turn
Then both boards show the X immediately (round-trip p95 < 500 ms), the turn indicator flips for both

Given the game reaches a win on Sara's move
Then both players see the result and highlight simultaneously
And exactly one match row per player is saved in one transaction — including Rafi's loss row

Given Rafi reloads the page mid-game
Then GetRoomQuery is the source of truth: the UI rebuilds the current RoomState
  and the group rejoin completes without a duplicate or a lost move
```

**Unhappy paths:**
```gherkin
Given it is O's turn (Rafi)
When Sara (or any of her tabs) submits a move
Then the server rejects it with Game.NotYourTurn and neither board changes

Given Rafi submits moves for cells out of turn, out of range, occupied, or after game over
Then responses are Game.NotYourTurn / Game.InvalidCell / Game.CellOccupied /
  Game.AlreadyOver respectively; nothing is broadcast; no row is written
```

## US-12 — Survive leaving and disconnects

**As an** online player, **I want** the outcome to be fair when the room is disturbed, **so that** nobody wins by accident.

**Acceptance criteria (happy):**
```gherkin
Given a Playing room where Nadia's socket drops
Then Sara sees "Opponent disconnected" plus a ~60 s countdown
  (Nadia, once back on any device before expiry, sees her game intact)

Given Nadia reconnects before expiry
Then the countdown is cancelled and play continues from the exact server state

Given one player clicks Leave mid-game
Then the opponent is awarded the win, both match rows are saved once, the room closes
```

**Unhappy paths:**
```gherkin
Given the grace deadline passes with no reconnect
Then the connected player wins by forfeit and both rows are saved exactly once
  (never two rows for either player even under a disconnect/expiry race)

Given a room in Finished state
When a player leaves or disconnects
Then no new rows are written; the room closes silently

Given a reconnect and the expiry clock firing simultaneously
Then exactly one of the two outcomes materializes (return or forfeit), never both —
  guarded by the room lock and the monotonic status transition rules
```

## US-13 — Rematch in place

**As** players who just finished, **I want** to run it back in the same room, **so that** the code stays alive.

**Acceptance criteria (happy):**
```gherkin
Given a Finished room
When Rafi presses Play again
Then his status reads "Waiting for opponent..." and Sara's shows his readiness

Given both indicated readiness
Then the board resets in the same room with X to start (Sara's original seat) and status Playing
```

**Unhappy paths:**
```gherkin
Given a Finished room where only one player pressed Play again
When the other player presses Leave instead
Then the room closes (per FR-28) and the waiting text collapses cleanly

Given a room Finished long enough to hit expiry rules
When rematch is requested
Then Room.Expired is returned if the room was closed first, with no partial state
```

## US-14 — Comfort: theme, sound, settings

**As a** player, **I want** the app to adapt and give feedback, **so that** long sessions feel good.

**Acceptance criteria (happy):**
```gherkin
Given a first visit with a device dark-theme preference
Then the page renders dark from the first paint (no white flash)

Given Sara toggles theme or sound anywhere (navbar or Settings)
Then the change applies immediately, persists in the browser, and Settings reflects it

Given a finished local game
Then the result sound plays; when muted, the same information remains visible in text
```

**Unhappy paths:**
```gherkin
Given the browser had no user interaction yet this session
When a remote event would make a sound
Then no audio plays (autoplay policy) and the status text carries the information instead

Given reduced-motion is enabled by the device
When marks appear or the winning line is drawn
Then animations are suppressed (state still changes, instantly)
```

---

## US-09 — Create an online room *(superseded 2026-11-07)*

> **Superseded by [US-16](#us-16--invite-a-player-and-start-playing) / [US-17](#us-17--decline-cancel-expire-and-cooldown)** when room codes were replaced by invitations ([ADR-0009](../01-architecture/adr/0009-invitations-replace-room-codes.md)). Kept for ID traceability.

*(Was: create a room → receive a 5-character code → wait up to the ~10 min idle expiry.)*

## US-10 — Join a friend's room *(superseded 2026-11-07)*

> **Superseded by [US-16](#us-16--invite-a-player-and-start-playing) / [US-17](#us-17--decline-cancel-expire-and-cooldown).** The join-race guarantee (exactly one winner) carries over into the invitation race rules: a player already Invited or In game can never double-book ([FR-36](../00-product/PRD.md)).

*(Was: join by 5-character code with not-found / full / expired messages.)*

---

## US-15 — See who's online (presence list)

**As a** registered player, **I want** a live list of who is online, **so that** I can find someone to play right now.

**Acceptance criteria (happy):**
```gherkin
Given Rafi is logged in with the site open (on any page)
Then he is visible in the online-players list to other logged-in players
  as "Available", and his status changes to "Invited" the moment an
  invitation involving him is pending (either direction)

Given Rafi closes his last tab (0 live connections)
Then every other player's list shows him as offline within ~2 s — live update, no refresh

Given a guest (not signed in) opens the lobby
Then they see only a login prompt; no list, no invite controls (FR-31/FR-32)
```

**Unhappy paths:**
```gherkin
Given a stale connection (network drop) with the site still open
Then the status flips to offline when the server drops the connection,
  and flips back on reconnect — presence follows the server, not the client's word
```

**Also covers:** usernames only are ever displayed; emails never appear in the list or anywhere else.

## US-16 — Invite a player and start playing

**As a** registered player, **I want** to invite someone online directly, **so that** we play without exchanging codes.

**Acceptance criteria (happy):**
```gherkin
Given an Available player's row on the lobby list
When the inviter clicks Invite
Then they see "Waiting for [name]…" with a Cancel button,
  and the invitee gets an Accept/Reject popup on whatever page they are on
  (the popup reaches them because every logged-in page keeps a live connection)

Given the invitee clicks Accept
Then the server creates the room in one atomic step and both players are
  taken to the board immediately — inviter as X, game started
  (no empty Waiting room ever exists)

Given either player clicks Cancel from the waiting state
Then the invitation clears for both parties and both statuses return to Available
```

**Unhappy paths:**
```gherkin
Given the inviter already has one outgoing invitation
When they try to invite a second player
Then the request fails cleanly with a clear message (FR-33, one-outgoing rule)

Given the target is In game, Invited, offline, or the inviter themself
When an invite is attempted (UI or crafted request)
Then it fails with the matching error and no invitation exists (FR-36)

Given the invitee accepted a different invitation in the same instant
When this invitation's accept is processed
Then exactly one invitation wins, a room is created exactly once,
  and the loser is told the player is no longer available
  (same exactly-one-winner discipline as the old join race, from US-10)
```

## US-17 — Decline, cancel, expire and cooldown

**As** either party, **I want** invitation dead-ends to be explicit and spam-proof, **so that** nobody is left hanging or pestered.

**Acceptance criteria (happy/unhappy):**
```gherkin
Given a pending invitation
When the invitee clicks Reject
Then the inviter sees "[name] declined." and both return to Available,
  no room is created, no rows written

Given the same inviter tries to re-invite the same player within ~30 s of the rejection
Then the attempt fails cleanly and the row still reads Available
  (the cooldown stops invitation spam, FR-35)

Given a pending invitation with no response for ~30 s
Then it expires: the inviter is told, the invitee's popup disappears, both go Available

Given the invitee (or inviter) goes offline while an invitation is pending
Then the invitation is cancelled with a clear message on the other side
  — nothing hangs silently (FR-35)
```

**Also covers:** every declined/expired/cancelled path writes **no match rows**; room creation happens only on Accept.

---

## Story → requirement traceability

| US | Must rows (verification mandatory) | Should rows |
| --- | --- | --- |
| US-01, US-02, US-04 | FR-01…FR-07, FR-11 | — |
| US-03 | FR-08, FR-09 | FR-10 |
| US-05 | FR-14 | — |
| US-06 | FR-15, FR-16, FR-17 | — |
| US-07 | FR-11, FR-31 | — |
| US-08 | FR-19 | — |
| US-09 *(superseded)* | ~~FR-23, FR-30~~ → US-16 | — |
| US-10 *(superseded)* | ~~FR-24, FR-30, FR-31~~ → US-16/US-17 | — |
| US-11 | FR-05, FR-25, FR-26 | — |
| US-12 | FR-28, FR-29 | — |
| US-13 | FR-27 | — |
| US-14 | FR-13, FR-22 | FR-21 |
| US-15 | FR-32, FR-31 | — |
| US-16 | FR-33, FR-34, FR-30 | — |
| US-17 | FR-35, FR-36 | — |

Every Must row in [`PRD.md`](PRD.md) §5 traces to at least one story; every story's unhappy paths map to named failure codes — the test suite derives both from those two facts.

## Assumptions

- Names Sara/Rafi/Nadia are **scenario-only** conventions; no product feature depends on them.
- Story numbering US-01…US-14 is stable; stories are never merged or renumbered — superseded intent gets a Gherkin block marked superseded, preserving the ID.
- `p95 < 500 ms` is measured as the round trip from the acting player's click to the *temporally-later* board update (self + opponent), per instrumentation in [`../05-quality/test-plan.md`](../05-quality/test-plan.md).
- Gherkin blocks here are executable intent, not the literal Cucumber feature files; the runner mapping lives in the test plan.
- FR-18 remains a reserved ID (guest rule covered via FR-15/FR-31 wording in the PRD); any doc referencing FR-18 points to this note.
