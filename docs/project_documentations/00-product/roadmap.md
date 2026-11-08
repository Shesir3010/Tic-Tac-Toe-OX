# Roadmap

*Milestones M0–M13 with hard exit criteria — the delivery plan that turns the PRD into v1.0, and the verification sweep that proves it shipped.*

**Related docs:**
- What each milestone delivers → [`PRD.md`](PRD.md) §5 · [`v1-scope.md`](v1-scope.md) §3
- Stories to verify → [`user-stories.md`](user-stories.md)
- Quality gates behind every exit → [`../05-quality/test-strategy.md`](../05-quality/test-strategy.md)
- Deploy-ready definition → [`../06-devops/deployment.md`](../06-devops/deployment.md)

---

## 1. Principles

| Principle | Consequence |
| --- | --- |
| Vertical slices | Each milestone ends with *usable* behaviour, not a half-layer |
| Gates, not dates | A milestone is done when its exit criteria hold — never "close enough" |
| Verification over trust | Prior milestones' ✅ come from their recorded gates; v1.0 still gets a full sweep (M13) because claims ≠ proof |
| Deviations rewrite docs first | If reality is better than spec, the spec changes first (with a changelog entry), then the code |

## 2. Milestones — feature-wise, tick by tick

One feature per milestone. Every milestone ends with **live, checkable behaviour + tests + walkthrough evidence** — the owner signs off each milestone by clicking it (no 1-line "done", only evidence).

| Milestone | Feature (subtasks = check off each) | FRs | Status | Exit criteria ("done when") |
| --- | --- | --- | --- | --- |
| M0 | Spike: host + skeleton + pins | — | ☐ | `dotnet build` zero warnings · CI green · pins in `Directory.Packages.props` · WebSockets negotiated on the host plan |
| M1 | Engine core (Board, Rules, Evaluate, E1–E6) | FR-02…FR-05 | ☐ | All 8 win lines × both symbols, draws, illegal moves, alternation covered · engine 100% line coverage · architecture test proves Domain has zero deps |
| M2 | AI (Easy → Medium → Hard) | FR-09, FR-10 | ☐ | Hard never loses the exhaustive minimax sweep · Medium win/block tests · Easy seeded-random test · reply ≤ 500 ms incl. artificial 300–500 ms delay |
| M3 | Local play (pass-and-play) | FR-01, FR-07, FR-11 | ☐ | Pass-and-play works end-to-end · winning line highlighted · reset/new-match works · handler tests (unit) green |
| M4 | Computer play (vs computer UI + move API + difficulty select) | FR-08–FR-10 | ☐ | Bot replies every move · difficulty switch works · board disabled during thinking · W-11 passed |
| M5 | Save & forgery proof (SaveMatch + replay) | FR-15, FR-25 replay | ☐ | Forged save rejected (`Game.ForgedResult`) · honest save writes row once · W-11 passed |
| M6 | History & stats pages | FR-16, FR-17 | ☐ | Totals computed from rows · paged history with opponent names · "Deleted player" shows correctly · U-9 + W-11 passed |
| M7 | Accounts (register / login / logout / lockout) | FR-14 | ☐ | Duplicate emails/usernames inline · generic credential error · lockout after 5 tries · W-8 passed |
| M7.5 | Theme, sound & settings (polish) | FR-13, FR-21, FR-22 | ☐ | Tokens `--ttt-*` + pre-paint theme · `/Settings` public (Theme × Sound + reset, browser-only) · Web-Audio sound + mute · winning overlay · responsive 360px · keyboard + `aria-live` · Lighthouse a11y ≥ 90 / perf ≥ 85 · owner toggled theme+sound |
| M8 | Guests (play without account, browser-only) | FR-18, FR-06, FR-12 (Could) | ☐ | Guest plays local/computer · nothing hits the database · W-11 (guest) passed |
| M9 | Presence (hub gateway + online list live) | FR-32 | ☐ | List updates live, no refresh · `userId → connectionCount` flips on disconnect · status Available/Invited/In game derived correctly · walkthrough observed |
| M10 | Invitations (send, popup accept/reject, 30 s expiry, 30 s cooldown, one-outgoing) | FR-33…FR-36 | ☐ | Guard + race unit tests green (self/offline/In-game/Invited/cooldown, accept-vs-expire exactly-once) · W-1, W-2, W-5, W-10 passed |
| M11 | Rooms (move, leave/forfeit, 60 s grace, rematch, cleanup) | FR-25…FR-29, FR-31 | ☐ | Both-rows-one-transaction test green · leave-forfeit test green · fake-time grace verified · W-3, W-4, W-6, W-7, W-9 passed |
| M12 | Account extras (change password, delete account) | FR-19, FR-20 | ☐ | Verify password required · delete in active room forfeits first then cascades · opponent row keeps history as "Deleted player" · W-12 passed |
| M13 | **Final verification sweep** | all FRs | 🔄 | see §3 |

> Milestone **order**: the core before the edges (engine → AI → local/computer → persistence → accounts → online → extras → polish → sweep). If an earlier milestone fails its evidence gate, the next one does not open.

## 3. M13 — the verification sweep (was M7)

M13 is not a feature milestone; it is the acceptance gate that converts "tasks were completed" into "the product matches the spec":

1. **FR sweep** — for FR-01…FR-36, excluding the superseded FR-23/FR-24 (IDs reserved, see [`PRD.md`](PRD.md) §9): locate the covering test (automated or documented manual script) in [`../05-quality/test-plan.md`](../05-quality/test-plan.md); run it; record pass/fail in the sweep table.
2. **US sweep** — walk every happy + unhappy Gherkin block in [`user-stories.md`](user-stories.md) against the live app (two browsers for online flows).
3. **Error-copy sweep** — every failure code produces exactly its literal message, per [`../02-backend/error-handling.md`](../02-backend/error-handling.md).
4. **NFR spot-checks** — move round-trip timing, page paint, Lighthouse ≥ 90 a11y / ≥ 85 performance, zero console errors, zero build warnings.
5. **Deploy artifacts** — publish profile, produced migration script, prod secrets checklist, `logs/` writability fallback — per [`../06-devops/deployment.md`](../06-devops/deployment.md).

**Failure handling:** a deviation is fixed in code — unless the shipped behaviour is genuinely better, in which case the docs change **first** (PRD §9 changelog) and then the code already matches.

**Exit criteria (hard):**
- Sweep table complete, every Must row resolved pass or fixed-and-repassed
- All suites green locally **and** in CI on the final commit
- Manual ≤ 30 min deployment rehearsal succeeds on the host
- Documentation set matches shipped behaviour (this sweep's final check)

## 4. Beyond v1.0

There is **no V2 plan**. Ideas are recorded in the PRD changelog as rejected scope; a future feature would re-enter through a new ADR + PRD revision, never through milestone creep. See `v1-scope.md` §4 for the out-of-scope list.

## Assumptions

- M0–M12 start as **todo** — statuses are *not* inherited from the original TASKS.md checkboxes. M13 treats every milestone claim as *to re-verify*, not as a delivered guarantee — consistent with the owner's report that parts of the app don't yet feel right.
- The verification sweep uses the same environment as the delivered deployment rehearsal (same host plan constraints).
- Milestones after M13 do not exist in the v1 plan; any new scope starts only through a PRD revision and ADR.
