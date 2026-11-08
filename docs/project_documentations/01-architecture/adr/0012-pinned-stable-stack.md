# ADR-0012: Pinned, Stable Package Stack

*Every package version is pinned centrally — upgrades are deliberate, reviewed events, not side effects of a restore.*

**Status:** Accepted (2026-11-06 from D-17 pin log, recorded standalone 2026-11-07) · **Referenced by:** [`../system-architecture.md`](../system-architecture.md) §1 · [`../adr/0007-cortex-mediator-mapster-serilog.md`](0007-cortex-mediator-mapster-serilog.md) · mirrors Senior suite ADR-0011 · `../../06-devops/ci.md` (future unit)

## Context

The app runs on a free hosting plan, is deployed manually by the owner, and is built mostly by AI agents. Unpinned dependencies would let a transitive upgrade break a green build *between* human reviews — the worst possible surprise class for this workflow.

## Decision

1. **Central Package Management** via `Directory.Packages.props`: every version is declared once; projects reference `PackageVersion` without inline numbers.
2. **The pinned set (v1):**

| Package | Version | License | Why pinned |
| --- | --- | --- | --- |
| Cortex.Mediator | 3.1.2 | MIT | CQRS dispatch — [ADR-0007](0007-cortex-mediator-mapster-serilog.md) |
| Mapster + Mapster.DependencyInjection | 10.0.14 | MIT | Mapping — [ADR-0011](0011-mapster-mapping.md) |
| Serilog.AspNetCore (+ console, file sinks) | 10.0.0 | Apache-2.0 | Logging — [ADR-0007](0007-cortex-mediator-mapster-serilog.md) |
| EF Core (SqlServer, Tools) + Identity (+ EntityFrameworkCore store) | 10.0.x | MIT | Data + accounts — [ADR-0002](0002-sql-server-with-ef-core.md), [ADR-0004](0004-identity-with-cookie-auth.md) |
| System.Security.Cryptography.Pkcs | 10.0.x | MIT | Transitive audit fix (recorded in the original pin log) |
| Test set: NUnit, Moq, Shouldly, Autofac.Extras.Moq | current stable at pin time | MIT-family | [ADR-0008](0008-nunit-moq-shouldly-testing.md) — test-only, never shipped |

3. **The .NET 10 SDK** is pinned in `global.json`; CI restores with the same file.
4. **Adding a package = a decision entry** ([ADR-0007](0007-cortex-mediator-mapster-serilog.md) rule imported here): new functionality is not a reason; a design gap is. The PR/pr description must name the gap that the framework or existing packages don't cover.
5. **Upgrade path:** patch/bugfix bumps are reviewed inline; major/.0 bumps and license changes get their own ADR. Any license moving away from MIT-family stops the bump until reviewed.

## Consequences

**Positive:** deterministic restores everywhere (owner machine, CI, future contributor machines); security-audit surface is exactly this list, reviewable in a minute; the "works on my machine" class cannot happen silently.

**Negative:** new-feature temptation (e.g. a QR library) can't be satisfied with a quick `dotnet add package` — the decision-record step is the point; pinning does not substitute for `dotnet list-package --vulnerable` checks in CI (recorded as a `ci.md` gate for the DevOps unit).

## Alternatives considered

| Alternative | Why rejected |
| --- | --- |
| Floating versions / `*` ranges | A package manager, not the owner, would make the "will it build" decision unpredictably |
| Per-project pinning (dx-level `Version=` on each reference) | Same package, multiple numbers = the drift CPM exists to kill |
| Monthly automated version-refresh PRs (dependabot-style, auto-merge) | Auto-merging dependency bumps violates the milestone-gate discipline of this workflow; bumps queue for review instead |

## Links

- Stack table source of truth: [`../system-architecture.md`](../system-architecture.md) §1
- Original pin log: D-17 in the seed doc [`../../00-product/original-technical-documentation.md`](../../00-product/original-technical-documentation.md) (§2.6)

## Assumptions

- The versions above reflect the 2026-11-06 pin log; the owner refreshes the table only via PR review, and this ADR's table is updated at the same time (docs and build files move together).
- `global.json` pinning is the last line of defense — if a new SDK is installed locally, builds still use the pinned one.
