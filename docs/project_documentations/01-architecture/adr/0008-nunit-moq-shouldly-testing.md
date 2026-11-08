# ADR-0008: Unit-Only Testing with NUnit + Moq + Shouldly (+ AutoMock)

*The testing toolchain: a single unit suite chosen for failure-message clarity, with real-server behaviour delegated to documented manual walkthroughs.*

**Status:** Accepted (2026-11-06) · **Revised 2026-11-07:** the owner removed the integration suite and then the E2E/Playwright suite — **one unit suite remains** (see Consequences) · **Referenced by:** `../../05-quality/test-strategy.md` · `../../05-quality/test-plan.md` · `../system-architecture.md` §1 · old D-08/D-19

## Context

The engineering budget includes engine 100% coverage, exhaustive minimax sweeps, race-scenario logic from [the real-time contract](../realtime-signalr.md) §6, and fake-time expiry/cooldown tests. The original docs approved the toolchain (D-08) and its AutoMock extension (D-19); this ADR consolidates that into an architecture-level record reflecting the owner's one-suite decision.

## Decision

| Suite | Tooling | Scope |
| --- | --- | --- |
| `test/TicTacToe.Application.UnitTests` | **NUnit** + **Moq** + **Shouldly** + **Autofac.Extras.Moq (AutoMock)** — **the only suite** | Engine + handlers + store/presence/invitation managers, mirrors `src/` feature folders; mocked contracts, fake `TimeProvider`, no server |

> **Unit-only policy (owner decisions, 2026-11-07):** the plan originally had a three-tier test layout; the owner removed the integration suite (`WebApplicationFactory` + real DB + SignalR client) and then the E2E suite (Playwright). Automated coverage is unit-only; the coverage that used to live in those suites is **reassigned to documented manual walkthrough scripts** — executed at every milestone gate and during the M13 verification sweep — covering cookie auth redirects, `[Authorize]` enforcement, anti-forgery requirement, error-copy rendering, hub negotiate, and the two-player race manifestations. Both suite types may return later via a new ADR if a need reappears.

## Conventions

1. **AAA** naming `MethodName_Scenario_ExpectedResult`; `[OneTimeSetUp]/[SetUp]/[TearDown]/[OneTimeTearDown]` lifecycle attributes.
2. **Shouldly for assertions** (`result.ShouldBe(...)`, `Should.ThrowAsync<...>`) — its failure messages read like sentences, which pays off enormously in race/forgery test failures.
3. **Handler/store/manager tests via AutoMock** (per D-19, test-only): construct handlers with exact mocked contracts; `Mock<IRoomStore>` stubs what the store promises (validate-then-apply); `Mock<IUnitOfWork>` asserts the single-commit sequence; `VerifyAll` where interactions matter.
4. **Fake determinism:** `TimeProvider` / `IRandomSource` are injected fakes in tests ([R-A4](../system-architecture.md)); nothing sleeps in a unit test; expiry/cooldown tests advance a fake clock.
5. **Manual walkthrough scripts own what mocks can't see:** real cookie auth, `Authorize` enforcement, anti-forgery enforcement, hub negotiate, and two-player race manifestations — each a numbered, reproducible checklist in [`../../05-quality/test-plan.md`](../../05-quality/test-plan.md), run at milestone gates and the M13 sweep, with results recorded per script.
6. **Non-negotiables:** tests are never deleted or weakened; unit tests never touch DB/network; a known-flaky test is fixed or fails the build — never quarantined-and-forgotten.

## What must be proven — and by what

| Guarantee | Verified by |
| --- | --- |
| Engine invariants E1–E6, Hard AI never loses (exhaustive sweep), Easy seeded randomness | Unit (pure — zero infra) |
| Handler contracts incl. failure codes (`Game.*`, `Room.*`, `Invite.*`, `Auth.*`) | Unit (AutoMock + Shouldly) |
| Two-rows-one-commit on online finish (single `SaveChangesAsync` invocation) | Unit (`Mock<IUnitOfWork>` sequence) |
| Store / presence / invitation-manager semantics: uniqueness, one-active-game, expiry/cooldown with fake time | Unit |
| Race-table logic rows (validate-then-apply, monotonic transitions, exactly-once accept) | Unit (mocked seams) |
| Cookie auth redirects, `[Authorize]` matrix, anti-forgery required | **Manual walkthrough script** |
| Error-code → literal message copy on every screen | **Manual walkthrough script** |
| Hub two-person flows: invite → accept → play → forfeit/expiry, reconnect, once-per-player saves | **Manual walkthrough script** (two browsers) |
| Presence-list live updates; reload-rejoin; full journeys incl. local + computer play; a11y spot checks | **Manual walkthrough script** |

## Consequences

**Positive:** assert-message clarity (Shouldly) doubles as documentation; AutoMock keeps handler tests honest about dependencies; **one suite** keeps CI minimal (no server bootstrapping, no real test DB, no browser downloads) and the automated loop runs in seconds on every commit and PR.

**Negative (accepted by the owner, 2026-11-07):** several formerly-automated guarantees — auth matrix, anti-forgery, error-copy rendering, hub race manifestations, SQL-only behaviors (migrations, FK/cascades via SSMS) — now depend on **manual discipline**: the walkthrough scripts must actually be executed and recorded at every gate or they silently rot; regressions below the unit-test ceiling surface later (at the M13 sweep) instead of in CI. **Trigger to revisit:** if the same class of bug escapes twice, add the matching suite back via a new ADR.

## Alternatives considered

| Alternative | Why not chosen |
| --- | --- |
| Keep the integration suite (`WebApplicationFactory`) | Removed by owner decision (2026-11-07) for v1 simplicity; returns via new ADR if a need reappears |
| Keep a Playwright E2E suite | Removed by owner decision (2026-11-07); its coverage lives in the walkthrough scripts now; returns via new ADR if regressions slipping past the unit ceiling demand automation |
| xUnit + FluentAssertions | xUnit's philosophy (shared class contexts) fits less well with per-test isolation ids the team already uses; FluentAssertions went commercial — Shouldly keeps MIT |
| AutoMock for every test, or none | Blanket usage obscures plain ctor-arg dependencies; restricted to handler-heavy path per D-19 review |

## Links

- Strategy & suite organization: [`../../05-quality/test-strategy.md`](../../05-quality/test-strategy.md)
- Contract-by-contract test list: [`../../05-quality/test-plan.md`](../../05-quality/test-plan.md)
- CI gates: [`../../06-devops/ci.md`](../../06-devops/ci.md)
- Slice structure + unit-only floor (folder convention): [ADR-0015](0015-vertical-slices-and-unit-only-tests.md)

## Assumptions

- Unit tests may parallelize freely (no shared state; every seam is a mock or fake).
- The absence of both the integration and E2E suites is an **owner decision (2026-11-07)**, revisitable: the review trigger (the same class of bug escaping twice) reopens the question via a new ADR.
- The walkthrough scripts' results are recorded in `docs/project_documentations/walkthroughs/` — the M13 sweep treats an unrecorded script as **not passed**.
- The D-19 approval applies to **test projects only** — Autofac.Extras.Moq never enters production assemblies.
