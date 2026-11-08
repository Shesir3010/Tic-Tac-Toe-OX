# Settings & Configuration

*Every key, default and environment variable in one list — so deploys never guess.*

Parent document: [PRD](../00-product/PRD.md) · Related: [System architecture](../01-architecture/system-architecture.md) §6 · [ADR-0012](../01-architecture/adr/0012-pinned-stable-stack.md) · [ADR-0004](../01-architecture/adr/0004-identity-with-cookie-auth.md) · [Deployment](../06-devops/deployment.md) (future unit)

---

## 1. `appsettings.json` (committed — no secrets)

| Key | Default | Notes |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | local SQL Server Express string | Overridden by environment in production — never commit real passwords |
| `RoomSettings:GraceSeconds` | `60` | Disconnect grace before forfeit (FR-29) |
| `RoomSettings:InvitationTtlSeconds` | `30` | Pending invitation lifetime (FR-35) |
| `RoomSettings:CooldownSeconds` | `30` | Re-invite block after a decline (FR-35) |
| `RoomSettings:CleanupIntervalSeconds` | `5` | `RoomCleanupService` tick |
| `Serilog:MinimumLevel` | `Information` (Development: `Debug`) | Structured console + rolling file |
| `Serilog:RollingFilePath` | logs under `logs/` | Never inside `wwwroot` |

## 2. Environment overrides (production)

| Variable | What it feeds |
| --- | --- |
| `ConnectionStrings__DefaultConnection` | SQL Server connection string (secret) |
| `ASPNETCORE_ENVIRONMENT` | `Production` (or `Development` locally) |
| `ASPNETCORE_URLS` | Bind address (host terminates TLS — the app never does TLS) |
| `ASPNETCORE_Kestrel__Certificates__Default__...` | (none — certificates live at the host, not the app) |
| `Serilog__MinimumLevel` | Log level without a re-deploy |

## 3. Identity cookie configuration (ADR-0004)

| Setting | Value | Why |
| --- | --- | --- |
| Scheme | `IdentityConstants.ApplicationScheme` (cookies, **no JWT**) | The same cookie authorizes pages **and** the hub |
| Lifetime | 14 days, sliding | Standard Identity expiration |
| Lockout | 5 failed attempts → **1 min** lockout (owner 2026-11-08, was 5 min) | Settings in `AddInfrastructure` + this file |
| Password rules | Identity defaults (≥ 8 chars, complexity) | Displayed per-field on register (`Validation.Error`) |
| Cookie flags | `HttpOnly` always; `Secure` in Production (host/TLS terminates at the edge) | Cookie is the only token — it must be protected |
| `SameSite` | `Lax` | Top-level GETs carry it; no cross-site embedding exists in v1 |

## 4. Secrets rules (never break)

- **Never commit** connection strings with real passwords, certificates, or API keys — they go through environment variables.
- `appsettings.Development.json` may hold a local-only connection string; it is never deployed as-is.
- Serilog never logs connection strings, passwords, tokens, or full emails (user id instead).

## 5. Rule for changing any value

Changing a timing (grace/expiry/cooldown) or a lockout/cookie rule is an **engineering decision first**: record it in the relevant ADR / changelog, update this file, then edit `appsettings.json`. Timing changes therefore never silently enter production through a config edit alone.

## Assumptions

- `RoomSettings` is the single options object for every timing in §1 — no literal seconds in handlers, services or views.
- HTTPS terminates at the host (Let's Encrypt); the app listens on plain HTTP inside the trusted deploy network.
- `Directory.Packages.props` pins are the package-version config — documented in [ADR-0012](../01-architecture/adr/0012-pinned-stable-stack.md), not here.
