# CQRS Use Cases

*The full command and query catalog for the backend — every use case in the project's CQRS template with its inputs, guards, returns and error codes.*

Parent document: [PRD](../00-product/PRD.md) · Related: [CQRS & Mediator](../01-architecture/cqrs-and-mediator.md) · [Clean Architecture & DDD](../01-architecture/clean-architecture-ddd.md) · [Real-time & SignalR](../01-architecture/realtime-signalr.md) · Decision records: [ADR-0007](../01-architecture/adr/0007-cortex-mediator-mapster-serilog.md) · [ADR-0010](../01-architecture/adr/0010-result-pattern-expected-failures.md) · [ADR-0015](../01-architecture/adr/0015-vertical-slices-and-unit-only-tests.md)

---

## 0. The template (every Features/ folder follows this)

Every use case is one command or one query with one handler, side by side — the owner-standard CQRS layout ([ADR-0015](../01-architecture/adr/0015-vertical-slices-and-unit-only-tests.md)):

```text
Features/{Area}/{Feature}/
├── Command/
│   ├── XxxCommand.cs              # the write request
│   └── XxxCommandHandler.cs       # the business logic that executes it
└── Query/
    ├── GetXxxQuery.cs             # the read request
    └── GetXxxQueryHandler.cs      # the logic that fetches data
```

Catalog-wide rules: commands return `OperationResult<T>` ([ADR-0010](../01-architecture/adr/0010-result-pattern-expected-failures.md)); identity comes from `ICurrentUser` (cookie claims), never from the payload; handlers own their commits; expected failures are values, never exceptions.

---

## 1. `Game/Moves` — local & vs-computer play

### `PlayMoveCommand` — `Features/Game/Moves/Command/`

| Field | Value |
| --- | --- |
| Inputs | `index` (0–8) |
| Guards | index in range · cell empty · game unfinished · player is the side to move |
| Returns | `OperationResult<GameDto>` (board + turn + result) |
| Errors | `Game.InvalidCell`, `Game.CellOccupied`, `Game.AlreadyOver` |
| Transport | JSON endpoint (`POST /api/game/move`) — **no hub connection needed offline** |
| Notes | The same engine (`Rules`) the online path uses — rules exist once ([ADR-0006](../01-architecture/adr/0006-pure-game-engine-in-domain.md)) |

### `SaveMatchCommand` — `Features/Matches/History/Command/`

| Field | Value |
| --- | --- |
| Inputs | submitted move list + claimed result |
| Guards | **forgery check:** re-run the engine over the move list; claimed result must match |
| Returns | `OperationResult` (row written once) |
| Errors | `Game.ForgedResult` (no row written) |
| Transport | Razor POST |
| Notes | A mismatch proves the client invented the result — reject before any write |

### `GetHistoryQuery` / `GetStatsQuery` — `Features/Matches/History/Query/`

| Field | Value |
| --- | --- |
| Inputs | current user (from `ICurrentUser`) |
| Guards | registered only |
| Returns | `OperationResult<IReadOnlyList<MatchHistoryItem>>` / `OperationResult<PlayerStats>` |
| Errors | none expected (auth failures are the page's `[Authorize]` redirect) |
| Transport | Razor pages (History / Stats views) |
| Notes | Read-side projections over `Matches` — never touch rooms or aggregates |

---

## 2. `Rooms/Invitations` — presence & invitations

### `SendInvitationCommand` — `Features/Rooms/Invitations/Command/`

| Field | Value |
| --- | --- |
| Inputs | target `userId` |
| Guards (FR-36, re-evaluated inside `InvitationManager` under its lock) | not self · target online (≥ 1 live connection) · target status `Available` · inviter has **no outgoing invitation** · target not on rejection cooldown (~30 s) |
| Returns | `OperationResult<InvitationState>` (waiting) |
| Errors | `Invite.Self`, `Invite.Offline`, `Invite.Busy`, `Invite.Cooldown`, `Invite.AlreadyPending` |
| Transport | Hub `InvitePlayer` |
| Notes | One outgoing invitation per player at a time; the guard set is the *only* place this rule lives |

### `CancelInvitationCommand` — `Features/Rooms/Invitations/Command/`

| Field | Value |
| --- | --- |
| Inputs | `invitationId` |
| Guards | only the inviter cancels |
| Returns | `OperationResult` (success) |
| Errors | `Invite.Forbidden` (guest / non-inviter), `Room.NotFound` (already terminal) |
| Transport | Hub `CancelInvitation` |
| Notes | Clears state for both parties immediately — broadcasts `InvitationCancelled` |

### `RespondToInvitationCommand` — `Features/Rooms/Invitations/Command/`

| Field | Value |
| --- | --- |
| Inputs | `invitationId`, `accept` (bool) |
| Guards | only the target responds · invitation still pending (checked inside the manager lock) |
| Returns | accept → `OperationResult<RoomState>` · reject → `OperationResult` |
| Errors | `Invite.Forbidden`, `Room.Expired` (idle too long), `Invite.Offline` (target offline) |
| Transport | Hub `RespondToInvitation` |
| Notes | **Accept is atomic:** in one locked flow the invitation flips to accepted and the room is created (inviter = X) — never a room without an invitation, never two rooms ([ADR-0009](../01-architecture/adr/0009-invitations-replace-room-codes.md)) |
| Effects | Reject → inviter sees "[name] declined."; target locked from that inviter's re-invites ~30 s (offers `Invite.Cooldown` on the next attempt) |

### `GetOnlinePlayersQuery` — `Features/Rooms/Invitations/Query/`

| Field | Value |
| --- | --- |
| Inputs | — |
| Guards | registered only |
| Returns | `OperationResult<IReadOnlyList<OnlinePlayerDto>>` (`{ userId, displayName, status }`) |
| Errors | none expected |
| Transport | Hub `GetOnlinePlayers` (initial lobby list) |
| Notes | Status derived from `PresenceTracker` + `InvitationManager` + room store on every call — never cached |

---

## 3. `Rooms/Online` — in-game moves

### `MakeOnlineMoveCommand` — `Features/Rooms/Online/Command/`

| Field | Value |
| --- | --- |
| Inputs | `roomId`, `index` |
| Guards (inside `IRoomStore.ApplyAsync` under the per-room lock) | caller is a participant · status `Playing` · it is that player's turn · engine legality (`Rules`) |
| Returns | `OperationResult<RoomState>` |
| Errors | `Room.NotParticipant`, `Game.NotYourTurn`, `Game.CellOccupied`, `Game.InvalidCell`, `Game.AlreadyOver` |
| Transport | Hub `MakeMove` |
| Notes | On finish, both `Match` rows are written **once**, one transaction, inside the same lock-held flow (FR-26) |

### `LeaveRoomCommand` — `Features/Rooms/Online/Command/`

| Field | Value |
| --- | --- |
| Inputs | `roomId` |
| Guards | caller is a participant |
| Returns | `OperationResult` (success) |
| Errors | `Room.NotParticipant` |
| Transport | Hub `Leave` |
| Notes | Leaving mid-game = **forfeit** — the opponent wins, both rows saved once; leaving a finished room only closes it |

### `RequestRematchCommand` — `Features/Rooms/Online/Command/`

| Field | Value |
| --- | --- |
| Inputs | `roomId` |
| Guards | caller is a participant · room finished · both players ready (two flags under the lock) |
| Returns | `OperationResult<RoomState>` (reset board, X to start) |
| Errors | `Room.NotParticipant`, `Game.AlreadyOver` (until both ready) |
| Transport | Hub `RequestRematch` |
| Notes | Board resets `RoomState` in place; no new room is needed |

### `GetRoomQuery` — `Features/Rooms/Online/Query/`

| Field | Value |
| --- | --- |
| Inputs | `roomId` |
| Guards | caller is a participant |
| Returns | `OperationResult<RoomState>` — the resync source of truth |
| Errors | `Room.NotFound`, `Room.NotParticipant` |
| Transport | Hub reconnect + room page load (the only way a client rebuilds state — never trusts its own memory) |

---

## 4. `Account/Profile` — account use cases

### `ChangePasswordCommand` / `DeleteAccountCommand` — `Features/Account/Profile/Command/`

| Field | Value |
| --- | --- |
| Inputs | current + new password · — (current user) |
| Guards | current password correct · not locked out |
| Returns | `OperationResult` |
| Errors | `Auth.WrongPassword`, `Auth.LockedOut`, `Validation.Error` |
| Transport | Razor POST |
| Notes | **Delete is forfeit-first:** if the deleting player is in an active room, the leave-forfeit path runs first (inside the room lock, opponent's row written), then the cascade delete executes |

---

## Assumptions

- This catalog is **closed for v1** — a new use case gets a new slice folder in this shape before it is coded.
- Auth register/login/logout are deliberately **not** commands: they go directly through Identity/`IAccountService` (framework calls, not domain use cases) — this is the honest divergence from the seniors' catalog, recorded here so no one "fixes" it.
- Error codes below are the v1 contract; literal message copy lives in `error-handling.md` and new codes must be added there and to tests in the same change.
- The hub transport never names or reaches these handlers directly — every line in "Transport" goes hub → `IMediator` → handler ([ADR-0003](../01-architecture/adr/0003-signalr-hub-as-command-gateway.md)).
