# ADR-0001: Clean Architecture with ASP.NET Core MVC

*Locks the four-project layering and the MVC-with-Razor application shape that every other decision builds on.*

**Status:** Accepted (2026-11-06) · **Supersedes:** an earlier React/TypeScript frontend proposal in the original docs · **Referenced by:** `../system-architecture.md` · ADR-0003 · ADR-0006

## Context

The product must: enforce authoritative game rules server-side; be testable without a browser or a database; run as one small deployment on a low-memory free hosting plan; and be buildable/maintainable by a solo developer. An earlier frontend proposal suggested React + TypeScript with a thin API; the original docs' review rejected it in favor of server-side rendering.

## Decision

Adopt Clean Architecture with four projects (`Domain`, `Application`, `Infrastructure`, `Web`) and the dependency rule (arrows point inward, Domain references nothing). Build the **Web** tier as ASP.NET Core MVC with **Razor Views** — server-rendered HTML plus small vanilla JS modules — rather than a JavaScript SPA:

1. The server is the referee (FR-02/05/25): rules live once, in the engine, and the client never owns truth ([ADR-0006](adr/0006-pure-game-engine-in-domain.md)).
2. Razor + Bootstrap delivers every screen from one codebase with no node toolchain, no bundler, no API/SPA split ([../system-architecture.md](../system-architecture.md) §2).
3. The razor-thin JS layer (≤ 15 KB per module) only renders server state and calls server endpoints — so client/server drift is impossible on local screens and mitigated by wholesale `RoomState` replacement online ([../realtime-signalr.md](../realtime-signalr.md) §4).
4. Testability follows from the layering: engine and handler logic is unit-tested with zero infrastructure; real-server behaviour is verified by documented manual walkthrough scripts (no integration/E2E suites — owner decision, [ADR-0008](adr/0008-nunit-moq-shouldly-testing.md)).

## Consequences

**Positive:** rule duplication becomes untestable and therefore impossible (enforced by architecture tests); deployment is one app, one config story — ideal for a ≤ 30 min manual runbook; solo-dev cognitive load stays low.

**Negative:** heavier interactivity (drag, animation-heavy future features) would tax vanilla JS — accepted, with the `IGameEngine` seam as the path if the product grows.

**Neutral:** MVC and API endpoints coexist in `Web` (`POST /api/game/move` + hub); that mixed surface is intentional and documented, not accidental ([ADR-0003](adr/0003-signalr-hub-as-command-gateway.md)).

## Alternatives considered

| Alternative | Why rejected |
| --- | --- |
| React/Next SPA + thin API | Two frontends' complexity, a bundler chain and client-owned state for one game page; rejected by the old review, kept out |
| Blazor Server | Real-time fits, but tailors hosting harder (locked to SignalR circuits) and shrinks the "no framework" JS budget argument; also beyond the hiring/demo need |
| Razor Pages only (no API) | Hub + JSON endpoints need a cleaner split than Pages offers; MVC gives Areas-free organization for the controllers |
| Web Forms-style "smart views" | Violates the authoritativeness requirement directly |

## Links

- Parent structure: [`../system-architecture.md`](../system-architecture.md)
- Enforcement: [`../../05-quality/test-strategy.md`](../../05-quality/test-strategy.md) (architecture tests)

## Assumptions

- "MVC with Razor" also covers the JSON endpoints and the hub in v1 — a future split into a separate API project would be a mechanical ADR, not a rearchitecture.
- Solo-owner maintenance means the dependency rule is enforced by tooling (tests), not by convention alone.
