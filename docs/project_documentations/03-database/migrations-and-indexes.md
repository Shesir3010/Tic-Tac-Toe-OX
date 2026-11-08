# Migrations & Indexes

*How schema changes happen (EF Core code-first migrations) and the indexes that keep history/stats fast.*

Parent document: [PRD](../00-product/PRD.md) · Related: [Schema & ERD](schema-and-erd.md) · [Concurrency](concurrency.md) · [Clean Architecture & DDD](../01-architecture/clean-architecture-ddd.md) · [ADR-0012](../01-architecture/adr/0012-pinned-stable-stack.md)

---

## 1. Migrations (code-first, EF Core 10)

**Rule: the model in code is the source of truth. Never hand-edit tables — every change is a checked-in migration.**

| Step | Command / action |
| --- | --- |
| Add a migration | `dotnet ef migrations add <DescriptiveName>` (from `src/TicTacToe.Infrastructure/` project) |
| Local apply | `dotnet ef database update` — targets SQL Server **Express** (managed with SSMS) |
| Production apply | `dotnet ef database update` runs as part of the manual deploy (MonsterASP.NET); the same migration assembly applies it |
| Rollback | `dotnet ef database update <PreviousMigrationName>` (down-migration, tested before use) |

| Where things live | Meaning |
| --- | --- |
| `Infrastructure/Data/Migrations/` | Migration classes + model snapshot (the snapshot is the app's memory of the schema — never deleted) |
| `Infrastructure/Data/ApplicationDbContext.cs` | The model. Indexes and FK behavior are configured here, not in raw SQL |
| **No stored procedures, no raw SQL, no triggers** | Schema changes are migrations only ([system-architecture](../01-architecture/system-architecture.md) §5.4) |

**Before any deploy:** run the migration against a copy of the production database, verify `Migrations` table updated (`__EFMigrationsHistory`) and app opens the History page.

## 2. Index list

| Index | Serves | Notes |
| --- | --- | --- |
| `UNIQUE (NormalizedUserName)` | Username uniqueness (`Auth.DuplicateUsername`) | Identity-managed |
| `UNIQUE (NormalizedEmail)` | Email uniqueness (`Auth.DuplicateEmail`) | Identity-managed |
| `INDEX (Matches.UserId, Matches.PlayedAtUtc DESC)` | `GetHistoryQuery` + `GetStatsQuery` — one user's matches, newest first | The only custom index on `Matches` |
| FK indexes on `Matches.UserId` / `OpponentUserId` | Cascade delete + set-null on account deletion | EF/Core + SQL Server maintain these |

## 3. Query shapes (why these indexes exist)

```sql
-- History: one user's matches, newest first — covered by the (UserId, PlayedAtUtc) index
-- Stats: counts per Result for one UserId — same index, no extra table scans
-- Delete account: DELETE FROM Matches WHERE UserId = @id (cascade) — UPDATE opponent
--   references SET NULL (EF FK behavior, never raw SQL)
```

Stats are **computed from `Matches` rows** (counts per `Result`), never stored as counters — there is no `PlayerStats` table to keep in sync.

## Assumptions

- Migrations run **forward only in production**; a destructive (down) migration is reviewed and tested on a database copy first.
- The model snapshot is committed with every migration — a missing snapshot fails review.
- `__EFMigrationsHistory` is the deployed-version record; checking it is part of the deploy walkthrough.
