# Frontend Architecture

*How the Razor + vanilla-JS client is organized: module map, layering rules, hub ownership, state replacement, and the design-tech choices (confetti, sounds, icons, color mode).*

Parent document: [PRD](../00-product/PRD.md) §5 · Related: [UX/UI Design System](ux-ui-design-system.md) · [Pages & Flows](pages-and-flows.md) · [Real-time & SignalR](../01-architecture/realtime-signalr.md) · Decision records: [ADR-0003](../01-architecture/adr/0003-signalr-hub-as-command-gateway.md)

---

## 1. Overview

The client is **server-rendered Razor views + vanilla JavaScript ES modules** — no SPA framework, no bundler, no build step. The server is the only source of game truth: the client renders whatever `RoomState` the hub/endpoint returns and never merges, diffs, or decides rules locally. The JS layer's job is presentation only: render state, play motion/sound, surface messages.

```text
wwwroot/
├── css/
│   ├── site.css           — design tokens (--ttt-*) + component classes (ttt-*)
│   └── board.css          — board layout + mark animation specs (from animations.md)
├── js/
│   ├── router.js          — (no SPA router) — path → module boot per page
│   ├── hub.js             — SignalR connection singleton (site-wide presence)
│   ├── lobby.js           — Home: presence list, invite buttons, waiting card
│   ├── popup.js           — invitation modal (Accept/Reject + 30 s bar)
│   ├── board.js           — Room: render RoomState, cell input, turn indicator
│   ├── room.js            — Room: hub events (MoveApplied, grace, result)
│   ├── toast.js           — message rendering (error-handling.md copies)
│   ├── sound.js           — synthesized Web-Audio tones (no audio files) + mute (Settings)
│   ├── confetti.js        — win celebration (canvas-confetti)
│   ├── history.js         — History/stats fetch + skeleton render
│   ├── signup.js / login.js — auth form behaviors (eye toggle, strength meter)
│   └── settings.js        — theme (data-bs-theme) + sound prefs
└── lib/
    ├── bootstrap.min.css/js  — Bootstrap 5.3
    ├── bootstrap-icons.*     — icons
    ├── signalr.min.js        — @microsoft/signalr
    ├── howler.min.js         — sounds
    └── confetti.min.js       — celebrations
```

## 2. Folder structure (web project)

| Folder | Contains | Rules |
| --- | --- | --- |
| `wwwroot/css/` | Tokens + components + board | Colors only via `--ttt-*` variables — never typed hex in views |
| `wwwroot/js/` | One ES module per job (above) | No module over ~15 KB; no inter-module state except through `hub.js`/`toast.js` |
| `Views/{Section}/` | Razor views per controller (Home, Rooms, Account, History, Settings, Shared) | Views render server data; forms POST to controller actions |
| `Controllers/` | Home, Rooms, Account, History, Settings | Thin: dispatch to Cortex commands/queries, return views/JSON ([ADR-0003](../01-architecture/adr/0003-signalr-hub-as-command-gateway.md)) |
| `Hubs/` | `GameHub` (command gateway) | Authenticate → dispatch → broadcast. Zero rules. |

## 3. Layering rules (client-side)

| Rule | Meaning |
| --- | --- |
| **Render-only** | JS never validates moves or decides turns — the server's `RoomState` *replaces* local state wholesale (no merging, no diffing) |
| **Transport dumb** | `hub.js` only connects + listens; every screen module subscribes and renders. No hub logic in views or `board.js` |
| **One module, one job** | Presence list code lives only in `lobby.js`; board rendering only in `board.js`; sounds only in `sound.js` |
| **Messages come from the catalog** | Frontend copies come verbatim from `02-backend/error-handling.md` — the client never invents error wording |
| **Server wins ties** | The board waits for the server and the returned `RoomState` replaces local state wholesale; no optimistic render is kept |
| **Sound settings** | Web Audio toggles persist in the browser (`localStorage`), work for guests, and need no server round-trip (Settings is public, FR-22) |

## 4. Hub connection ownership

- One **connection singleton** (`hub.js`) created after login, on **every page** (site-wide presence, [ADR-0009](../01-architecture/adr/0009-invitations-replace-room-codes.md)). The auth cookie authorizes the negotiate — no token plumbing ([ADR-0004](../01-architecture/adr/0004-identity-with-cookie-auth.md)).
- `withAutomaticReconnect` (backoff ‖ 30 s) → on reconnect, the room page re-fetches `GetRoomQuery` and rebuilds the board.
- Page modules subscribe/unsubscribe on load/unload; the connection itself never closes while signed in.
- Event vocabulary: the **13 events** ([realtime-signalr](../01-architecture/realtime-signalr.md) §4) — the client has no vocabulary beyond them.

## 5. Design tech choices (one line each — each is a package-pin decision)

| Tech | Role | Why it's worth it |
| --- | --- | --- |
| Bootstrap 5.3 | Layout, components, `data-bs-theme` dark/light | The design system (tokens defined in `site.css`); zero learning curve for the owner |
| Vanilla JS ES modules | All client behavior | No build step; every file ≤ ~15 KB; natively loadable |
| canvas-confetti | Win burst | The game's single celebration moment (900 ms + shapes spec in [animations.md](animations.md) §3.6) |
| Synthesis (Web Audio) | Sounds (click/win/lose/ping + draw) | Fulfills the PRD sound settings (FR in Settings); synthesized in the browser, no audio files, no licensing |
| Bootstrap Icons | All icons | Zero code — crisp outline icons |

**Rejected explicitly:** Tailwind (would fight Bootstrap's components), GSAP (overkill for a 9-cell board — CSS + WAAPI suffices), chart/avatar APIs (privacy + weight), any SPA framework (server-rendered is the decided shape).

## 6. Error & failure display mapping

| Failure kind | Client display |
| --- | --- |
| `OperationResult` error (hub/JSON) | Toast with the exact `message` from `error-handling.md`; the `code` is shown in dev log only |
| `Validation.Error` on forms | Per-field inline message (never a toast) |
| Illegal move | Cell shake 300 ms + toast ([animations.md](animations.md) §3.7) |
| Hub reconnecting | ConnectionBadge pulses; board shows skeletons until `GetRoomQuery` resolves |
| Hub down | Persistent banner "Connection lost — your game is saved. Reconnecting…" |

## 7. Build & deploy (no build step)

`wwwroot/` is deployed verbatim — no bundler, no npm build. Fonts are self-hosted (Inter, JetBrains Mono) so first paint never flashes a serif. Static assets cache via the host's caching rules; `site.css` versioned with a query string per deploy.

## 8. Sound design (Web Audio — no audio files)

| Tone | Trigger | Spec |
| --- | --- | --- |
| `place` | every cell placement (paired with the mark draw) | short oscillator blip, < 0.15 s |
| `ping` | `InvitationReceived` | soft two-tone, non-alarming |
| `win` | win (pairs with confetti) | rising arpeggio, 1–2 s |
| `lose` | loss | falling tone, 1 s |
| `toggle` | Settings switches (sound/theme) | < 0.2 s click |

All tones are **synthesized with the Web Audio API** — no audio files, no libraries, zero licensing. Volume is fixed (no volume slider in v1 — only on/off); sound is on/off and persists in the browser per user (Settings, `localStorage`). Sounds play only after first user interaction (browser autoplay policy). Missing sounds degrade to silence — never a broken page.

## Assumptions

- ES modules (`type="module"`) load in all supported modern browsers; any legacy fallback is out of scope for v1.
- The design-tech set (Bootstrap, vanilla JS, confetti, Icons + synthesized audio) is closed for v1 — a new library is a package-pin decision + an ADR line, not a casual add.
- With JS disabled, the server still plays local/computer games and shows pages correctly (progressive enhancement); online play requires JS by definition.
