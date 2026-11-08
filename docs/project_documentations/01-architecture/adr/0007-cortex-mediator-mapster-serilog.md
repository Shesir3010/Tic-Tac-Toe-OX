# ADR-0007: Cortex.Mediator + Mapster + Serilog

*Choosing the supporting cast once — the CQRS dispatcher, the mapper and the logger — so layers share one vocabulary instead of three half-adopted libraries.*

**Status:** Accepted (2026-11-06) · **Referenced by:** `../system-architecture.md` §1 §5 · `../../02-backend/cqrs-use-cases.md` · old D-16/D-17

## Context

The Application layer dispatches commands/queries; Domain DTOs flow to views/hub payloads and back; logging must be structured and review-friendly with zero secrets. Candidates in the mainstream (and originally recorded in D-16): MediatR (commercial changes), AutoMapper (configuration auto-magic), the classic `ILogger` (pesistent). Non-negotiables: ctor injection, SOLID, only necessary pipeline behaviors, pins via `Directory.Packages.props`.

## Decision

| Concern | Choice | Version | License |
| --- | --- | --- | --- |
| CQRS dispatch | **Cortex.Mediator** | 3.1.2 | MIT |
| Mapping | **Mapster** + Mapster.DependencyInjection | 10.0.14 | MIT |
| Logging | **Serilog** via Serilog.AspNetCore | 10.0.0 | Apache-2.0 |

Ground rules:

1. **One use case = one `ICommand<T>`/`IQuery<T>` + one handler**; controllers and the hub dispatch via `IMediator` only ([../02-backend/cqrs-use-cases.md](../../02-backend/cqrs-use-cases.md)) — the dispatch API's benefit is the enforceable discipline, whatever package provides it.
2. **Only Cortex's logging behavior is enabled**; extra behaviors (validation, transactions) get written as reviewed code or documented additions — pipeline magic is not free behavior on by default.
3. **Mapping is configuration, not convention:** explicit `IRegister` profiles live in `Application/Mapping/`; a unit test compiles the whole config so renamed members break at CI, not at runtime.
4. **Serilog config:** one point (Program.cs); console + rolling file sinks; structured templates elsewhere via `ILogger<T>`; never log credentials/tokens/full emails ([../system-architecture.md](../system-architecture.md) §5.1).
5. All versions pinned in `Directory.Packages.props` as transitive-audit pins (per the original pin log: EF Core/Identity 10.0.x, `System.Security.Cryptography.Pkcs` audit fix); **adding a package = a decision entry** — no casual adds.

## Consequences

**Positive:** MIT-licensed, small-surface, well-understood toolchain; behavior turned on deliberately gives every command/query a consistent log; Mapster's compile-check protects DTO drift; pinning makes supply-chain review cheap.

**Negative:** the ecosystem docs for these are thinner than the mainstream alternatives (accepted — usage surface is tiny); `AutoMapper`'s attribute-route self-configuration is not available, *deliberately*.

## Alternatives considered

| Concern | Alternative | Why rejected |
| --- | --- | --- |
| Dispatch | MediatR | Steering commercial licensing; Cortex is a like-for-like MIT tiny-dispatcher with equal conceptual footprint |
| Dispatch | Hand-rolled interface-per-use-case dispatch | Achieves the discipline, reinvents a trivial wheel, loses pipeline behaviors |
| Mapping | AutoMapper | Self-magical conventions hide DTO drift; Mapster profiles are explicit and compile-checked |
| Logging | MSTest/`ILogger`-only | Needs centralized sink config & enrichment (correlation ids) — Serilog standard practice, trivial to keep |

## Links

- Cross-cutting rules: [`../system-architecture.md`](../system-architecture.md) §5
- Use-case catalog: [`../../02-backend/cqrs-use-cases.md`](../../02-backend/cqrs-use-cases.md)

## Assumptions

- The three tools remain compatible with .NET 10 LTP-style upgrades; a major version jump is treated as a normal, recordable migration — not automatic.
- Any future pipeline behavior (validation, caching, unit-of-work wrappers) gets enabled one-at-a-time with a test, respecting "boring and explicit" over pluggable magic.
