# Deployment

*Manual deploy runbook for MonsterASP.NET — owner is the release captain, agents prepare everything but never deploy. Placeholder draft for v1, completed at M13.*

**Related docs:**
- Config & secrets → [`../02-backend/settings-and-configuration.md`](../02-backend/settings-and-configuration.md)
- Migration workflow → [`../03-database/migrations-and-indexes.md`](../03-database/migrations-and-indexes.md)
- Delivery plan → [`../00-product/roadmap.md`](../00-product/roadmap.md) M13

---

## 1. What this file is

A thin, checkable runbook: publish profile, migration script, secrets checklist, `logs/` writability, and the ≤30 min rehearsal. It is finished at **M13** (final verification sweep), not before.

## 2. Deploy steps (owner, manual, ≤30 min)

1. **Publish:** `dotnet publish -c Release` from the exact commit being released; attach the publish profile (Web Deploy / FTP) to this section with host name + date.
2. **Migrate:** run `dotnet ef database update` against a copy of the production database first (backup verified, rollback rehearsed); then apply the same migration assembly on the host. Check `__EFMigrationsHistory` updated and open the History page.
3. **Secrets:** enter `ConnectionStrings__DefaultConnection` (and any env overrides) on the host only — never commit real passwords, certificates, or keys. Grep the repo + publish output before deploy.
4. **Verify:** WebSockets negotiate (`/hubs/game`), HTTPS + HSTS served, `logs/` writable (fallback: console + host logs), app opens on `/` and `/Home` loads for a signed-in user.
5. **Rollback:** record the `PreviousMigrationName` down-migration + previous publish artifact path here; rehearse rollback on the copy, timed.

## 3. CI shape

`.github/workflows/dotnet.yml` → restore → build (warnings as errors) → `dotnet test`. No npm, no Docker, no auto-deploy.

## Assumptions

- Single instance, forever (v1): rooms/presence/invitations live in memory; a restart ends in-progress games while finished `Matches` persist in SQL.
- TLS terminates at the host (Let's Encrypt); the app listens on plain HTTP inside the trusted deploy network.
- Owner re-checks host plan (.NET 10, SQL Server, SignalR/WebSockets, 256 MB RAM / 1 GB DB) before the M13 rehearsal.
