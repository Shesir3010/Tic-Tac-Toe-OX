# ADR-0010: Result Pattern for Expected Failures

*Expected failures are values, not exceptions — one typed error-code catalog is the single vocabulary shared by commands, pages, the hub and tests.*

**Status:** Accepted (2026-11-06 from original D-16 review, recorded standalone 2026-11-07) · **Referenced by:** [`../system-architecture.md`](../system-architecture.md) §5.2 · [`../adr/0003-signalr-hub-as-command-gateway.md`](adr/0003-signalr-hub-as-command-gateway.md) · `../../02-backend/error-handling.md` (post-v1 unit) · [`../../00-product/PRD.md`](../../00-product/PRD.md)

## Context

The hub returns objects to `room.js`; Razor POSTs re-render forms with inline messages; the single JSON endpoint returns HTTP statuses. Expected failures (a taken cell, a full invitation guard, a wrong password) occur in normal play and must **never** become exceptions with stack traces in logs or crash pages. A common vocabulary is needed so every caller (page, hub, future endpoint) states failures identically.

## Decision

1. Handlers return **`OperationResult<T>`** — `{ IsSuccess, Value, Error: { Code, Message } }` with factories `.Success(value)` / `.Failure(code, message)`.
2. **Codes are `Area.Reason`** dotted names, dispatched on with match-arm switch statements client-side and asserted verbatim in tests:

| Area | Codes (v1, post-invitation) |
| --- | --- |
| `Game.*` | `NotYourTurn`, `CellOccupied`, `AlreadyOver`, `InvalidCell`, `ForgedResult` |
| `Room.*` | `NotFound`, `Expired`, `NotParticipant`, `AlreadyActive` *( Room.Full retired with room codes )* |
| `Invite.*` | `Self`, `Offline`, `Busy`, `Cooldown`, `AlreadyPending`, `Forbidden` (guest) |
| `Auth.*` | `InvalidCredentials`, `LockedOut`, `DuplicateEmail`, `DuplicateUsername`, `WrongPassword` |
| `Validation.*` | `Error` (field-specific inline copy) |

3. Every code has **exactly one literal user message** (e.g. `Room.NotFound` → "Room not found."); messages live in one catalog file (`02-backend/error-handling.md` unit + code constant file) — never re-worded per page.
4. **Exception rule of thumb:** expected input/business failures = the result value; *unexpected* errors (DB connection lost, a null contract violation) = thrown exceptions caught at boundaries — hub method, DB commit, Identity call — then logged into a friendly generic failure ([ADR-0008 conventions](0008-nunit-moq-shouldly-testing.md)).
5. HTTP/JSON mapping for the one REST endpoint: the table maps code → status (`Room.NotFound` 404 · `NotYourTurn/CellOccupied/AlreadyOver/Invite.Cooldown` 409 · `ForgedResult` 422 · `Auth.InvalidCredentials` 401 · `Validation.Error` 400) with the JSON body `{"error":{"code","message"}}`. Razor POSTs ignore HTTP statuses: they re-render forms with inline fields.

## Consequences

**Positive:** one switchable vocabulary for all transports (hub result, JSON, Razor inline); happy-path signatures stay honest (`OperationResult` returned, not `throw`); tests assert *codes* which can't drift; hub clients never parse exception text.

**Negative:** handler code carries an extra result wrapper everywhere (accepted — it's the discipline that keeps logging calm); adding/renaming a code touches three spots (constant file, catalog copy, tests) — mitigated by the same single-source-of-truth discipline used in [ADR-0011](0011-mapster-mapping.md)'s profiles: one constant file, no inline strings elsewhere.

## Alternatives considered

| Alternative | Why rejected |
| --- | --- |
| Throw `DomainException` subclasses for business failures | Untraceable stack traces in logs on every not-your-turn click; wrong severity levels in Serilog |
| Per-transport error shapes (hub string + JSON object + Razor ViewData) | Three sources of truth drift out of sync — exactly the bug class "message not found on one client" |
| Enum-only codes (no message) | Forces each client to re-map codes to copy — drift returns |
| Microsoft's `ProblemDetails` everywhere | Fine for the JSON endpoint; Razor pages and hub payloads need their own shape — keep the internal type and map once |

## Links

- Catalog copy (post-v1 unit): `../../02-backend/error-handling.md`
- Where failures originate: [`../adr/0003-signalr-hub-as-command-gateway.md`](0003-signalr-hub-as-command-gateway.md) (hub invocations), [`../../04-frontend/ux-ui-design-system.md`](../../04-frontend/ux-ui-design-system.md) (display)
- The rest of the cross-cutting rules: [`../system-architecture.md`](../system-architecture.md) §5.2

## Assumptions

- The catalog above is **closed for v1** — adding a code updates this file, the constants and tests together.
- `Invite.Self` returning 400 (vs 409) is a deliberate classification: it's not a state conflict, it's an invalid request.
- Literal messages may be re-worded only via the catalog file, never inline in a page — copy edits are reviewable in one place.
