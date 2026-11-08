# Walkthrough Evidence

*Recorded results of the manual walkthrough scripts (W-1…W-13) — unrecorded = not passed. Every milestone gate and the M13 sweep must leave a file here.*

**Related docs:**
- Script inventory → [`../05-quality/test-plan.md`](../05-quality/test-plan.md) §2–§3
- Strategy & DoD → [`../05-quality/test-strategy.md`](../05-quality/test-strategy.md)
- Milestone gates → [`../TASKS.md`](../TASKS.md)

---

## How to record a run

1. Copy `TEMPLATE.md` to a new file named `W-{n}-{YYYY-MM-DD}.md` (one file per script per run).
2. Fill script id, date, commit hash, browser(s), tester, pass/fail, and any screenshots/logs as file paths (never passwords, tokens, or full emails).
3. Mark the corresponding box in `../TASKS.md` ✅ only after the file is committed.

## Naming

- `W-08-2026-11-08.md` — single run
- `W-01-2026-11-08-2browsers.md` — when two browsers were needed (add suffix)

## Assumptions

- This folder is the only record of manual verification; PR comments do not count.
- A failed script blocks its milestone gate — there is no "run it later" state.
