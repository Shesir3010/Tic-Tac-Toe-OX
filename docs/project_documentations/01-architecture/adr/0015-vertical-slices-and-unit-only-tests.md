# ADR-0015: Vertical Slices and Unit-Only Testing

*Features are organized as one-folder vertical slices through the layers, and the automated floor is a single unit suite — deciding folder structure and test policy together because they shape the same work.*

**Status:** Accepted (2026-11-07, consolidating the customs from the original docs + owner's test-scope decisions of 2026-11-07) · **Amended 2026-11-08 (owner standard):** slice folder shape is now `Features/{Area}/{Feature}/Command|Query/` (files side by side), replacing the `{Area}/{Command|Query}/{Name}/` nesting · Mirrors Senior suite ADR-0015 (their slice-structure + unit-only stance) · **Referenced by:** [`../system-architecture.md`](../system-architecture.md) §3–§5 · [`../adr/0008-nunit-moq-shouldly-testing.md`](0008-nunit-moq-shouldly-testing.md) · `../../05-quality/test-strategy.md` (future unit)

## Context

The dependency rule ([ADR-0001](0001-clean-architecture-with-mvc.md)) tells developers *where code may not go* — but not *where new code goes*. Without a folder convention, handlers drift into loosely-typed service blobs. Separately, the test scope needed a final decision: the owner removed the integration suite and then the E2E/Playwright suite on 2026-11-07, leaving one automated floor.

## Decision

### Vertical slices

1. **Owner-standard CQRS layout** (amended 2026-11-08 — see Status): one use case = one pair of files, side by side, under `Application/Features/{Area}/{Feature}/Command/` (`XxxCommand.cs` + `XxxCommandHandler.cs`) for writes and `…/Query/` (`GetXxxQuery.cs` + `GetXxxQueryHandler.cs`) for reads. Slices never call each other's handlers — shared orchestration happens through `IMediator` or extracted domain services.

```
Features/Game/Moves/Command/PlayMoveCommand.cs
Features/Game/Moves/Command/PlayMoveCommandHandler.cs
Features/Game/Moves/Query/GetBoardQuery.cs
Features/Game/Moves/Query/GetBoardQueryHandler.cs
```

2. **Slices are vertical, not horizontal:** a slice may read Domain, build its DTO via the Mapster profile ([ADR-0011](0011-mapster-mapping.md)), commit via its repository and UoW — the whole flow lives in the slice, so "find where invites are sent" resolves to *one folder*, not one file per project layer.
3. Current areas: `Game` (feature `Moves`), `Matches` (feature `History`), `Rooms` (features `Invitations`, `Online`), `Account` (feature `Profile`) (+ `Presence`/`Invitations` commands dispatched from the hub through the invitation manager — visible in [ADR-0014](0014-in-memory-state-trio.md)).
4. **Unit tests mirror slices** — `test/TicTacToe.Application.UnitTests/Application/Features/{Area}/{Feature}/…` — so a slice's test names read beside its code in review.

### Unit-only automated floor

5. The **only automated suite** is `test/TicTacToe.Application.UnitTests` (NUnit + Moq + Shouldly + AutoMock, per [ADR-0008](0008-nunit-moq-shouldly-testing.md)); integration and E2E suites are **owner-removed decisions**, revisitable by the "same bug class escapes twice" trigger recorded there.
6. **Mock seams are provided by architecture, not by helper code:** every slice-visible dependency is an interface (`IRoomStore`, `IPresenceTracker`, `IInvitationManager`, `IUnitOfWork`, repositories, `ICurrentUser`), plus `TimeProvider`/`IRandomSource` fakes ([R-A4](../system-architecture.md)) so slices are unit-testable *by construction*.
7. **Architecture tests live in the unit suite** and enforce the dependency rule: `Domain` references no NuGet package, no SignalR/EF/ASP.NET types; `Application` never sees SignalR; `R-A2` engine purity (invariants E1–E6, [ADR-0006](0006-pure-game-engine-in-domain.md)). A violating slice fails CI — no architecture-integrity policing via review alone.
8. **Manual walkthrough scripts** (recorded consequences of the removed suites) cover real-server guarantees — cookie auth, hub negotiate, two-player races — executed at milestone gates and the M7 sweep ([ADR-0008 conventions](0008-nunit-moq-shouldly-testing.md)).

## Consequences

**Positive:** finding and changing any behaviour = opening exactly one folder; slice boundaries keep code reviews small ("this PR touches the Invite slice"); unit-testability by construction keeps the one-suite floor honest instead of mocking-heavy heroics; the architecture tests make the layering self-defending.

**Negative:** strict slice isolation forbids convenient cross-slice shortcuts — a genuinely shared computation becomes a domain service or is duplicated intentionally (duplication is chosen over hidden coupling below the handler level); without integration/E2E, slice correctness against the real DI graph is caught by manual scripts, not CI.

## Alternatives considered

| Alternative | Why not chosen |
| --- | --- |
| Horizontal layering (`AllCommands/`, `AllHandlers/`) | A feature change touches N folders per project; the slice-per-folder review story is lost |
| CQRS "micro-services" — a slice calling another slice's handler | Hidden coupling + transitive dispatch chains; orchestration goes through `IMediator`/domain services instead |
| Keep the integration suite as the floor | Owner decision 2026-11-07 — v1 optimizes for one suite and manual scripts (recorded in [ADR-0008](0008-nunit-moq-shouldly-testing.md)) |
| Property-based testing for the engine | At 3×3, the exhaustive minimax sweep *is* the property check, fully deterministic at every run |

## Links

- Folder structure source of truth: [`../system-architecture.md`](../system-architecture.md) §3 solution tree
- Toolchain + suite responsibilities: [`../adr/0008-nunit-moq-shouldly-testing.md`](0008-nunit-moq-shouldly-testing.md)
- Backend contract details (future unit): `../../02-backend/cqrs-use-cases.md`

## Assumptions

- "One slice per use case" counts *use cases*, not supporting types: a slice may host 2–3 small supporting types (e.g. an invitation-slot result record) without splitting.
- The architecture tests above are among the very first tests written for the unit suite; nothing else ships ahead of them ([M1 exit](../../00-product/roadmap.md)).
- Both test-scope decisions (no integration; no E2E) are recorded with their revisit triggers in [ADR-0008](0008-nunit-moq-shouldly-testing.md); this ADR governs neither re-adding suites nor shrinking them — only the slice structure and the unit-only floor.
