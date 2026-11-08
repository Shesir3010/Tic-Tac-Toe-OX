# ADR-0004: ASP.NET Core Identity with Cookie Authentication

*Why accounts use the framework's Identity system with its default cookie flow — and nothing fancier.*

**Status:** Accepted (2026-11-06) · **Amended 2026-11-08:** `UserName` is the display name (no separate `DisplayName` column); username regex `^[A-Za-z0-9_]{3,20}$` enforced by validator — see [`schema-and-erd.md`](../../03-database/schema-and-erd.md) · **Referenced by:** `../system-architecture.md` · `../../05-quality/security.md` §3.1–§3.2 · `../../00-product/PRD.md` (FR-14, FR-19, FR-20, FR-31)

## Context

v1 needs: register/login/logout with no email step ([FR-14](../../00-product/PRD.md)), automatic sign-out after account deletion ([FR-19](../../00-product/PRD.md)), account deletion requiring a password recheck on-site, browser-based reauth for the hub, lockout behavior with friendly copy, and guests explicitely excluded from online ([FR-31](../../00-product/PRD.md)). The deployment plan has no email service (hosting check).

## Decision

1. **ASP.NET Core Identity** owns credentials: `ApplicationUser : IdentityUser<Guid>` where `UserName` **is** the display name (unique, shown in rooms/history, regex `^[A-Za-z0-9_]{3,20}$`); default hashing, lockout (5 attempts / 5 min), and the standard stores. No separate `DisplayName` column in v1.
2. **Cookie authentication only** — the browser cookie is the sole credential for pages **and** the hub (SignalR negotiate uses the same cookie). No JWTs, no token-based flows.
3. Login accepts **username or email**; duplication is rejected with inline per-field errors; wrong credentials produce exactly one generic message (never "email not found").
4. No email confirmation, no email reset (no mail service → D-10); a forgotten password = create a new account until an email service lands.
5. The identity surface: `/Identity` scaffolding pages + app controllers for operations ([`../../05-quality/security.md`](../../05-quality/security.md) §§3.1–3.2); `[Authorize]` protects Rooms, Account, History, Delete and the hub.
6. `CurrentUser : ICurrentUser` is the identity seam in `Application`, so commands never touch `HttpContext`.

## Consequences

**Positive:** the auth code is framework-tested and boring; lockout/hashing/session risks are inherited fixed; deletion + sign-out integrates with the cookie lifecycle in one place; hub auth needs zero additional machinery.

**Negative:** cookie sessions are per-browser-instance; "my account on my phone and my laptop" means two sessions — accepted and irrelevant at v1 scope.

## Alternatives considered

| Alternative | Why rejected |
| --- | --- |
| JWT + fetch-based auth | The app is a single MVC origin; JWT adds storage/rotation risk and doesn't cover hub negotiate more cleanly than the cookie |
| Custom password hashing + session store | Rebuilding what Identity battle-tests; no feature gain |
| External sign-in (Google/Microsoft) at v1 | Demo account with provider console setup for zero persona need — parked, not scheduled |
| Auth0/third-party identity | Costs, external dependency, and no offline behavior on the free plan |

## Links

- Identity detail: [`../../05-quality/security.md`](../../05-quality/security.md) §§3.1–3.2
- Guest rule: [`../../00-product/PRD.md`](../../00-product/PRD.md) FR-31

## Assumptions

- The cookie lifetime equals the browser session by default; the owner may tune it in `Program.cs` without an ADR (behavioral nuance, not architecture).
- Lockout counts/timings are the Identity defaults from [the test plan](../../05-quality/test-plan.md); changing them is a config change, not a decision — unless strong customer feedback says otherwise.
