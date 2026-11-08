# Error Handling

*The exact messages users see, where each error code renders, and how unexpected failures degrade — in one file, so wording never drifts.*

Parent document: [PRD](../00-product/PRD.md) · Related: [ADR-0010](../01-architecture/adr/0010-result-pattern-expected-failures.md) · [CQRS & Mediator](../01-architecture/cqrs-and-mediator.md) · [CQRS Use Cases](cqrs-use-cases.md) · [Real-time & SignalR](../01-architecture/realtime-signalr.md) · Decision record: [ADR-0007](../01-architecture/adr/0007-cortex-mediator-mapster-serilog.md)

---

## 1. The contract

Expected failures are **values** (`OperationResult<T>.Failure(code)`), not exceptions ([ADR-0010](../01-architecture/adr/0010-result-pattern-expected-failures.md)). Every code has exactly **one literal message** below; pages, the hub and the JSON endpoint all copy from this file — never re-word per page. Tests assert **codes**, so copy can be edited without breaking tests.

## 2. The full catalog (v1, post-invitation)

### `Game.*` — board & play

| Code | Message | Where it renders |
| --- | --- | --- |
| `Game.NotYourTurn` | "It's not your turn." | Room page inline · hub result to the mover |
| `Game.CellOccupied` | "That cell is already taken." | Room page inline · hub result |
| `Game.AlreadyOver` | "This game is already finished." | Room page inline · rematch view |
| `Game.InvalidCell` | "That cell doesn't exist." | Hub result (tampered request) |
| `Game.ForgedResult` | "The submitted result doesn't match the moves." | `SaveMatchCommand` rejection (silent — no row written, no user accusation) |

### `Room.*` — rooms

| Code | Message | Where it renders |
| --- | --- | --- |
| `Room.NotFound` | "Room not found." | 404 page for unknown ids · hub result |
| `Room.Expired` | "This room is no longer active." | Hub result (late accept / stale room page) |
| `Room.NotParticipant` | "You are not playing in this room." | Hub result |
| `Room.AlreadyActive` | "You already have a game in progress." | Invitation accept + account delete guard |

### `Invite.*` — invitations

| Code | Message | Where it renders |
| --- | --- | --- |
| `Invite.Self` | "You can't invite yourself." | Lobby popup (400 — invalid request, not a conflict) |
| `Invite.Offline` | "[name] is offline." | Lobby popup + offline notice |
| `Invite.Busy` | "[name] is busy right now." | Lobby popup ("In game" / "Invited" status) |
| `Invite.Cooldown` | "You can try inviting [name] again in a moment." | Lobby popup after a decline (~30 s) |
| `Invite.AlreadyPending` | "You already have an invitation waiting." | Lobby popup (one outgoing per player) |
| `Invite.Forbidden` | "You can't do that." | Hub result (guest / non-owner attempts) |

### `Auth.*` / `Validation.*` — accounts & forms

| Code | Message | Where it renders |
| --- | --- | --- |
| `Auth.InvalidCredentials` | "Invalid username or password." | Login form (one generic message — never "email not found") |
| `Auth.LockedOut` | "Too many failed attempts. Try again later." | Login form after 5 failed attempts in 5 minutes |
| `Auth.DuplicateEmail` / `Auth.DuplicateUsername` | "That email / username is already taken." | Registration form, per-field inline |
| `Auth.WrongPassword` | "Current password is incorrect." | Change-password form |
| `Validation.Error` | Per-field message (e.g. "Password must be 8+ characters.") | The failing form field, inline |

## 3. How each transport renders errors

| Transport | Rendering rule |
| --- | --- |
| **Razor POSTs** | Ignore HTTP statuses. Re-render the form with the message inline (per-field for `Auth.*`, page-level for others) |
| **JSON endpoint** (`POST /api/game/move`) | HTTP status per §4 + body `{"error":{"code","message"}}` — the client shows `message`, logs `code` in dev |
| **Hub invocations** | Return the `OperationResult` object directly; the client shows `message` and offers the matching action (e.g. refresh) |
| **Invitation popups** | `InvitationDeclined` shows "[name] declined."; `InvitationExpired` shows "No answer — the invitation expired."; offline shows "[name] went offline." |

## 4. Error → HTTP status mapping (JSON endpoint only)

| Status | Codes |
| --- | --- |
| 400 | `Validation.Error`, `Invite.Self` |
| 401 | `Auth.InvalidCredentials` |
| 403 | `Invite.Forbidden` |
| 404 | `Room.NotFound` |
| 409 | `Game.NotYourTurn`, `Game.CellOccupied`, `Game.AlreadyOver`, `Invite.Cooldown`, `Invite.AlreadyPending` |
| 422 | `Game.ForgedResult` |

`Invite.Self` is 400, not 409: it is an invalid request, not a state conflict ([ADR-0010](../01-architecture/adr/0010-result-pattern-expected-failures.md)).

## 5. Unexpected failures (not this catalog)

Unexpected errors (DB connection lost, null contract violation, commit failure) are **thrown exceptions**, caught at boundaries — hub method, DB commit, Identity call, cleanup loop — logged with context, then translated into a friendly generic failure ("Something went wrong. Please try again."). `GlobalExceptionHandler` is the last-resort net (friendly page + correlation id). Users **never** see stack traces or SQL.

**Logging levels** ([ADR-0007](../01-architecture/adr/0007-cortex-mediator-mapster-serilog.md)): **Warning** = expected failures here (`Game.NotYourTurn`, `Invite.Cooldown`) · **Error** = unexpected exceptions · **Information** = business events (match saved, account deleted). Never log passwords, tokens, connection strings, or full emails.

## Assumptions

- Copy edits happen **only in this file** — a wording change is reviewable here and nowhere else.
- Codes asserted verbatim in tests; message copy can change without breaking the suite.
- Adding a code means: this file → the constants → the catalog tests, in the same change.
- `Auth` lockout (5 attempts / 5 minutes) is the Identity default — changing it is a config decision recorded in `settings-and-configuration.md`.
