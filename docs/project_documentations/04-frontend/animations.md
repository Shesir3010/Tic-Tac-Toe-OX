# Animations & Motion

*The animation system for the app: motion tokens, the complete spec for all 15 animations, `prefers-reduced-motion` behavior, and the vanilla-JS/WAAPI integration. Design tokens and components: [ux-ui-design-system.md](ux-ui-design-system.md).*

Parent document: [PRD](../00-product/PRD.md) §5 · Related: [UX/UI Design System](ux-ui-design-system.md) · [Pages & Flows](pages-and-flows.md) · [Frontend Architecture](frontend-architecture.md)

---

## 1. Motion Design Principles

1. **Motion explains, it doesn't decorate.** Every animation answers: *what happened?* (a mark landed, a turn changed, someone connected, time is running out).
2. **Fast and purpose-built.** The longest anything runs is 500 ms (the winning line); loops (turn pulse, grace countdown) are the only continuous motion.
3. **Server state in, motion out.** The client never animates its own guess — motion starts when the authoritative `RoomState` arrives ([frontend-architecture.md](frontend-architecture.md) §3).
4. **Sound accompanies.** Marks, wins and pings pair with short synthesized Web-Audio tones; muted toggle in Settings, sounds only after first interaction.
5. **Motion never blocks input.** Nothing locks the board or the buttons while animating; a click during an animation still registers.

---

## 2. Motion Tokens

| Token | Value | Used for |
| --- | --- | --- |
| `--ttt-duration-fast` | 150 ms | hover, press, previews |
| `--ttt-duration-normal` | 200 ms | page, toast, modal transitions |
| `--ttt-duration-slow` | 300 ms | mark draw, shake |
| `--ttt-duration-deliberate` | 500 ms | winning line |
| `--ttt-ease-out` | `cubic-bezier(0.16, 1, 0.3, 1)` | entrances |
| `--ttt-ease-in-out` | `cubic-bezier(0.4, 0, 0.2, 1)` | loops, moves |
| `--ttt-ease-spring` | `cubic-bezier(0.34, 1.56, 0.64, 1)` | pops (invitation modal, buttons) |

Loops: turn indicator pulse 2 s (`ease-in-out`), skeleton shimmer 1.5 s, waiting dots 1.2 s, reconnecting badge pulse 2 s, grace countdown bar (continuous, server-driven).

---

## 3. Animation Specs

### 3.1 Page transition — fade + slide, 200 ms
Every page load: opacity 0→1 + translateY 8px→0 (`ease-out`, 200 ms). Board pages skip it (state matters more than entrance).

### 3.2 Lobby list enter / leave with stagger
Player rows enter with 40 ms stagger (top→bottom, 200 ms `ease-out`) on list refresh; leaving rows fade out 150 ms. Only the first render staggers — refreshes just update.

### 3.3 X mark draw — 300 ms stroke-dash
X is an SVG path; `stroke-dashoffset` animates from full → 0 over 300 ms (`ease-out`), sky-400 stroke. The mark appears only when the server confirms the move.

### 3.4 O mark draw — 300 ms stroke-dash
Same as X, rose-400 stroke, 300 ms `ease-out`.

### 3.5 Winning line draw + glow pulse — 500 ms + 2 s loop
A line through the winning cells draws in 500 ms (`ease-out`), then the winning cells pulse `shadow-glow-x/o` in a 2 s loop until the result modal opens.

### 3.6 Victory burst (canvas-confetti) — 900 ms
`confetti({…})` from the center of the board: indigo/sky/rose particles, 900 ms flight arc, no loop — once per win. Draws: 400 ms small burst. Loss: none (the tone carries it).

### 3.7 Invalid move — soft shake, 300 ms
Cell translates X ±4 px (3 ticks), 300 ms `ease-in-out`, plus error toast with the exact message from `error-handling.md`.

### 3.8 Turn indicator — pulsing ring, 2 s loop
Current player's avatar/score side gets an `ease-in-out` glow pulse (2 s loop); switches sides on `MoveApplied`.

### 3.9 Button press — scale 0.97, 150 ms
Every primary button: press `scale(0.97)` 150 ms (`ease-out`), release spring back. Never disables.

### 3.10 Skeleton shimmer — 1.5 s loop
Slate-800 blocks with a moving highlight (1.5 s loop, `ease-in-out`). History rows, stats, board cells while loading.

### 3.11 Waiting-for-opponent — animated dots
Three dots pulse 1.2 s stagger (indigo) next to "Waiting for [name]…". Used while an outgoing invitation is pending (the waiting card in `pages-and-flows.md` §4.3).

### 3.12 Hover preview fade-in — 150 ms
Ghost mark (sky/rose at 40% opacity) fades in on cell hover in 150 ms; fades out on leave. Never on touch devices.

### 3.13 Toast slide-in / fade-out — 200 ms / 150 ms
Toasts slide up 12 px + fade in (`ease-out`, 200 ms); exit fade 150 ms. Stack max 3, oldest exits first.

### 3.14 Result modal + invitation popup — scale + fade entrance, 200 ms spring
Modal scales 0.96→1 and fades in (`ease-spring`, 200 ms). The invitation popup's 30 s progress bar drains linearly underneath (motivation: visible deadline). Esc = reject (invitation) or dismiss (result).

### 3.15 ConnectionBadge — soft opacity pulse (Reconnecting) + grace countdown ring
Reconnecting badge pulses 0.6↔1 opacity (2 s loop). The 60 s grace shows a countdown ring + `text-mono` seconds next to the board (no beeping except the last 10 s, only if sound is on).

---

## 4. `prefers-reduced-motion`

When the user (or their OS) requests reduced motion, **all** animations collapse to instant state with one exception class:

| Behavior | Under reduced motion |
| --- | --- |
| Fades, slides, draws, staggers, pops | Instant (duration 0) — final state shown immediately |
| Loops (turn pulse, shimmer, badge pulse) | Stopped — static final state |
| Confetti | Replaced by a subtle CSS glow (no particles) |
| Grace countdown | Kept (it's information, not decoration) — no pulsing |
| Sound | Unchanged (sound is not motion) |

Detection: `matchMedia('(prefers-reduced-motion: reduce)')` — `animations.js` reads it once at boot; every spec above is gated behind it. Reduced-motion users still see *every* state, just without the movement.

---

## 5. Vanilla JS / WAAPI integration

No animation library — specs run on CSS classes and the Web Animations API (WAAPI):

```js
// board.js — example: X draw per §3.3
export async function drawMark(cell, mark) {           // called ONLY on server RoomState
  if (reducedMotion) return cell.settle(mark);          // instant state
  cell.classList.add(`mark-${mark}`);
  await cell.querySelector('path').animate(
    [{ strokeDashoffset: 100 }, { strokeDashoffset: 0 }],
    { duration: 300, easing: 'cubic-bezier(0.16,1,0.3,1)' }
  ).finished;
  cell.playSound(mark === 'x' ? 'markX' : 'markO');    // sound.js
}
```

Rules:

| Rule | Reason |
| --- | --- |
| CSS classes carry the keyframes (in `board.css`) | Declarative, GPU-friendly, reviewable next to tokens |
| WAAPI for draw animations and one-offs | Procedural paths (stroke-dash) that CSS can't easily parametrize |
| Every animation gated behind `animations.js` (`reducedMotion` flag) | One place implements §4 — specs never each re-check the media query |
| Sounds fired from the same call sites | Animation and sound are one reaction to one server event (mark lands → draw + click together) |

---

## Assumptions

- 500 ms is the absolute ceiling for any single animation; loops are the only continuous motion.
- Grace/countdown timers are information, not decoration — they keep animating even under reduced motion (without pulse).
- Motion tokens live in `site.css` beside the design tokens (one file, one source).
- If a future animation needs more than CSS + WAAPI, it comes with its own ADR line ([ADR-0012](../01-architecture/adr/0012-pinned-stable-stack.md)) — never a silent add.
