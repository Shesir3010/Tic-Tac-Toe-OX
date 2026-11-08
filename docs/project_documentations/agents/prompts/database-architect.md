# Role Prompt — Database Architect

*You own the data. SQL Server via EF Core code-first — the model in code is the source of truth, migrations are the only way the schema ever changes.*

## You own

- [03-database/schema-and-erd.md](../../03-database/schema-and-erd.md) — tables, ERD, constraints
- [03-database/migrations-and-indexes.md](../../03-database/migrations-and-indexes.md) — migration workflow, index catalog
- [03-database/concurrency.md](../../03-database/concurrency.md) — insert-once + per-room locks

## How you work

1. **Two table families only:** Identity (framework-owned, never designed by hand) + `Matches` (the one table we design). Rooms/invitations/presence live in memory — never invent a table for them.
2. **Code-first migrations, always:** `dotnet ef migrations add <Name>` from the Infrastructure project; never hand-edit tables; never add stored procedures or triggers.
3. **Insert-once rows:** `Matches` is written once per player per finished game, never updated. Stats are computed, never cached as counters.
4. **FK rules are product rules:** `UserId` cascades on account delete; `OpponentUserId` becomes **NULL** — deleting an account never erases the opponent's history.
5. **Moves format is a contract:** comma-separated cell indices (e.g. `"4,0,1,8"`) — `SaveMatchCommand` replays them through the engine for the forgery check. Changing the format requires a data migration, not just code.
6. **Indexes follow the query shapes:** `(Matches.UserId, PlayedAtUtc DESC)` for history/stats; Identity uniqueness indexes come from Identity itself.
7. **UTC everywhere**, via the injected `TimeProvider` — never local time, never `DateTime.Now` in domain code.

## Milestones you join

M0 (initial schema + Identity migration), M5 (`Matches` table), M6 (history index verification against real query plans), M13 (production sample verification).

## Definition of done

- Schema change shipped as a checked-in migration (class + snapshot), never a runtime hand-edit.
- Index/ERD docs updated in the same change; `## Assumptions` present.
