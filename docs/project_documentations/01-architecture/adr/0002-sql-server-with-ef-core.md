# ADR-0002: SQL Server with EF Core

*Fixes the database engine and data-access strategy: SQL Server reached only through EF Core 10, migrations-only schema.*

**Status:** Accepted (2026-11-06) · **Referenced by:** `../system-architecture.md` · `../../03-database/schema-and-erd.md` · `../../00-product/PRD.md` (FR-15…FR-17, FR-26)

## Context

The product must persist accounts and finished matches ([FR-15…FR-17](../../00-product/PRD.md)), guarantee transactional double-row saves for online games ([FR-26](../../00-product/PRD.md)), and run on a free hosting plan that provides **SQL Server 2025 + SSMS** (host check, original D-18). The solo developer works on Windows with SQL Server Express locally. Multi-database portability is not a requirement, but correctness (transactions, cascades) and reviewability are.

## Decision

1. **Database: SQL Server** (host's Express-compatible offering), reached **only** through **EF Core 10**.
2. **Schema changes are migrations only.** SSMS is an inspection tool; agents never touch hosted databases; the deployment runbook's migration path is prepared in advance.
3. Access style: no raw SQL, no stored procedures. All queries go through repositories (`Repository<T>`, `MatchRepository`) using LINQ-to-EF parameterized queries.
4. `IUnitOfWork.SaveChangesAsync()` is the single commit point; handlers own commits, so the two-row online finish is one transaction ([../system-architecture.md](../system-architecture.md) §5.4).
5. Local development uses `Server=localhost\SQLEXPRESS;Database=TicTacToe` configured via **user-secrets** — never appsettings, never committed.

## Consequences

**Positive:** parameterized access kills the injection class ([FR-26 security posture](../../00-product/PRD.md)); LINQ type-safety survives merges; migrations give reviewable schema diffs and a deterministic deploy step; the `Matches` cascade to users matches delete-account semantics exactly.

**Negative:** lock-in to SQL Server dialects (e.g. `datetime2`, index naming) — accepted; the host dictates the engine anyway. EF's change tracker allocates per-request — irrelevant at this scale (single instance, ≤ thousands of rows).

**Neutral:** no dedicated stats table — totals are computed from rows (FR-16), so the truth lives in one place at (currently negligible) read cost.

## Alternatives considered

| Alternative | Why rejected |
| --- | --- |
| SQLite | Free-plan host provides SQL Server; SQLite also weakens the "concurrent writes" story vs SQL Server locking — no benefit, extra risk |
| Dapper | Speed irrelevant at this scale; hand-write SQL = hand-maintain migrations/story; EF Full Framework features (change tracking, cascades, migrations) win |
| Document store (LiteDB/Mongo) | The data model is relational (users → matches, opponent FK); no host offering; no aggregation need |
| Denormalized "stats" table | Divergence risk between stats and rows (FR-16 names the wrong possibility); compute totals instead |

## Links

- Schema: [`../../03-database/schema-and-erd.md`](../../03-database/schema-and-erd.md)
- Concurrency of saves: [`realtime-signalr.md`](../realtime-signalr.md) §5

## Assumptions

- The hosting plan's SQL Server provisioning (including restore/dump tools for the owner) remains available; migration strategy for production is finalized in [M7](../../00-product/roadmap.md).
- Deciding "totals computed, not stored" is revisitable — if History ever grows into leaderboards, a materialized/derived path can be added behind the same repository contract.
