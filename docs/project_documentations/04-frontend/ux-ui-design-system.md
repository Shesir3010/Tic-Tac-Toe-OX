# UX/UI Design System

*The single source of truth for the visual language of the Tic Tac Toe app: design tokens (Bootstrap 5.3 + CSS variables), color, typography, spacing, and the full component inventory with state-by-state specs.*

Parent document: [PRD](../00-product/PRD.md) §5 · Related: [Pages & Flows](pages-and-flows.md) · [Frontend Architecture](frontend-architecture.md) · [Animations](animations.md) · Decision records: [ADR-0004](../01-architecture/adr/0004-identity-with-cookie-auth.md) · [ADR-0007](../01-architecture/adr/0007-cortex-mediator-mapster-serilog.md)

---

## 1. Design Principles

1. **Clean, modern, calm.** Chess.com-inspired professionalism, not arcade noise. One accent color (indigo) carries interactivity; the two mark colors (sky/rose) are reserved for game semantics.
2. **Dark-first.** Slate-950 stage, slate-900 cards, subtle indigo radial ambience. A light theme is the Bootstrap 5.3 color-mode token swap (`data-bs-theme`), never a per-component concern.
3. **Generous space, rounded cards, soft shadows.** Content breathes: 24 px card padding, 24 px panel gaps, `rounded-4` cards, shadows that model depth, not drama.
4. **Feedback for every action.** Every click answers within 150 ms — hover, press, spinner, toast, shake, or animation. Silence is a bug.
5. **Shape before color.** X and O are strokes, not just hues; status always pairs a dot/shape with a text label. Color is never the only indicator (WCAG 1.4.1).
6. **Motion with restraint.** 150–500 ms, purposeful, never blocking input, always disabled under `prefers-reduced-motion`. Full policy: [animations.md](animations.md).

---

## 2. Design Tokens — Bootstrap 5.3 + CSS variables

There is **no Tailwind, no `tailwind.config.js`**. All tokens live as CSS variables in `wwwroot/css/site.css` (prefix `--ttt-`), loaded after Bootstrap's `bootstrap.min.css`. Bootstrap utilities consume them (`bg-body`, `text-secondary`, …) plus our own components (`ttt-card`, `ttt-board`, …). One new package = a decision entry ([ADR-0012](../01-architecture/adr/0012-pinned-stable-stack.md)); approved design additions: canvas-confetti, Bootstrap Icons — never a CSS framework. Sounds are synthesized via the Web Audio API (**no audio files**, no libraries — PRD FR-21).

```css
/* wwwroot/css/site.css — tokens */
:root {
  /* ---------- Surfaces & text (slate scale) ---------- */
  --ttt-bg:              #020617; /* slate-950 — app background */
  --ttt-surface:         #0f172a; /* slate-900 — cards, panels, board card */
  --ttt-surface-raised:  #1e293b; /* slate-800 — hover fills, inputs, chips */
  --ttt-border-subtle:   #1e293b; /* slate-800 — card & divider borders */
  --ttt-border-strong:   #334155; /* slate-700 — input rest borders */
  --ttt-text-primary:    #f1f5f9; /* slate-100 — headings, body emphasis */
  --ttt-text-secondary:  #cbd5e1; /* slate-300 — body copy, labels */
  --ttt-text-muted:      #94a3b8; /* slate-400 — meta, hints, timestamps */
  --ttt-text-disabled:   #64748b; /* slate-500 — disabled labels only */

  /* ---------- Brand & semantics ---------- */
  --ttt-primary:         #6366f1; /* indigo-500 — primary actions, links, active tab */
  --ttt-primary-hover:   #818cf8; /* indigo-400 — primary hover, focus ring */
  --ttt-mark-x:          #38bdf8; /* sky-400    — X mark, Player X identity */
  --ttt-mark-o:          #fb7185; /* rose-400   — O mark, Player O identity */
  --ttt-success:         #10b981; /* emerald-500 — Available / Connected */
  --ttt-danger:          #ef4444; /* red-500     — errors, invalid move flash */
  --ttt-danger-text:     #f87171; /* red-400     — error text (AA on dark surfaces) */
  --ttt-status-playing:  #fbbf24; /* amber-400   — "In game", "Reconnecting..." */

  /* ---------- Typography ---------- */
  --ttt-font-sans: "Inter", ui-sans-serif, system-ui, -apple-system, sans-serif;
  --ttt-font-mono: "JetBrains Mono", ui-monospace, "SFMono-Regular", Menlo, monospace;

  /* ---------- Radius ---------- */
  --ttt-radius-card:    1rem;    /* cards, panels, board card (rounded-4) */
  --ttt-radius-control: 0.75rem; /* buttons, inputs, cells, toasts (rounded-3) */
  --ttt-radius-pill:    9999px;  /* badges, count pills, connection badge */

  /* ---------- Shadows & glow ---------- */
  --ttt-shadow-card:    0 1px 2px 0 rgb(2 6 23 / 0.50), 0 8px 24px -8px rgb(2 6 23 / 0.60);
  --ttt-shadow-overlay: 0 12px 40px -8px rgb(2 6 23 / 0.80);
  --ttt-shadow-glow-primary: 0 0 20px rgb(99 102 241 / 0.40);
  --ttt-shadow-glow-x:       0 0 16px rgb(56 189 248 / 0.35);
  --ttt-shadow-glow-o:       0 0 16px rgb(251 113 133 / 0.35);

  /* ---------- Motion (specs: animations.md) ---------- */
  --ttt-duration-fast:       150ms;  /* hover, press, previews */
  --ttt-duration-normal:     200ms;  /* page, toast, modal transitions */
  --ttt-duration-slow:       300ms;  /* mark draw, shake */
  --ttt-duration-deliberate: 500ms;  /* winning line */
  --ttt-ease-out:   cubic-bezier(0.16, 1, 0.3, 1);   /* entrances */
  --ttt-ease-in-out:cubic-bezier(0.4, 0, 0.2, 1);   /* loops, moves */
  --ttt-ease-spring:cubic-bezier(0.34, 1.56, 0.64, 1); /* pops */
}

/* App backdrop: slate-950 with a subtle indigo radial ambience */
body {
  background-color: var(--ttt-bg);
  background-image: radial-gradient(ellipse 80% 50% at 50% -10%, rgb(99 102 241 / 0.08), transparent);
  background-attachment: fixed;
  color: var(--ttt-text-secondary);
  font-family: var(--ttt-font-sans);
  -webkit-font-smoothing: antialiased;
}
```

Fonts are self-hosted (Inter 400–700, JetBrains Mono) so first paint never flashes a system serif.

---

## 3. Color System — Usage & Contrast

All ratios computed against `--ttt-bg` (#020617) unless noted; WCAG 2.1 AA requires 4.5:1 (normal text), 3:1 (large text ≥ 24 px / 18.66 px bold, and UI component boundaries), 7:1 (AAA normal text).

| Token | Value | Contrast vs bg | Usage | Contrast note (WCAG AA) |
| --- | --- | --- | --- | --- |
| `--ttt-bg` | #020617 | — | App background | Base for all ratios below |
| `--ttt-surface` | #0f172a | 1.1:1 | Cards, panels, board card, input fills | Text on surface: primary 16.3:1, secondary 12.0:1, muted 7.0:1 — all pass |
| `--ttt-surface-raised` | #1e293b | 1.4:1 | Hover fills, chips, secondary buttons, toasts | slate-100 text on it: 13.4:1 — pass |
| `--ttt-border-subtle` | #1e293b | 1.4:1 | Card/divider borders | Decorative structure; labels + focus ring carry meaning |
| `--ttt-border-strong` | #334155 | 2.0:1 | Input rest borders | Paired with always-visible labels; focus state provides the 3:1 boundary (indigo-400: 6.8:1) |
| `--ttt-text-primary` | #f1f5f9 | 18.4:1 | Headings, emphasized text | AAA |
| `--ttt-text-secondary` | #cbd5e1 | 13.6:1 | Body copy, form labels | AAA |
| `--ttt-text-muted` | #94a3b8 | 7.9:1 | Hints, timestamps, placeholders, meta | AAA; **the** muted-text color — slate-500 is forbidden for readable text |
| `--ttt-text-disabled` | #64748b | 4.2:1 | Disabled control labels only | Disabled elements are WCAG-exempt; below AA on purpose to read "inert" |
| `--ttt-primary` | #6366f1 | 4.5:1 | Primary buttons (fill), links, focus accents | As fill, its label is **slate-950** (4.5:1) — see Button spec |
| `--ttt-primary-hover` | #818cf8 | 6.8:1 | Primary hover fill, focus rings, links on hover | Passes AA text; the focus-ring color (3:1 UI requirement: pass) |
| `--ttt-mark-x` | #38bdf8 | 9.4:1 | X strokes, X-player name/label, X win glow | AAA as text; never used for actions |
| `--ttt-mark-o` | #fb7185 | 7.5:1 | O strokes, O-player name/label, O win glow | AAA as text |
| `--ttt-success` | #10b981 | 8.0:1 | Success toast icon/border, Available dot, Connected dot | Dots always paired with text label (1.4.1) |
| `--ttt-danger` | #ef4444 | 5.4:1 | Error borders/icons, invalid-move flash | AA; not used for body text |
| `--ttt-danger-text` | #f87171 | 7.3:1 | Error messages | AAA |
| `--ttt-status-playing` | #fbbf24 | 12.1:1 | "In game" dot, "Reconnecting..." dot | AAA; distinct hue from both marks and primary |
| Avatar tint text (300-level: sky-300, rose-300, indigo-300, …) | e.g. #7dd3fc | ≥ 9:1 | Avatar initials on `tint/15` fills | AAA on every surface used |

Rules:

- Never place `--ttt-primary` text on `--ttt-surface-raised` or colored fills; use it only on `--ttt-bg`/`--ttt-surface` or as a fill itself.
- White (`#ffffff`) is reserved for modal headings; body text uses the slate scale for calmer contrast.
- Marks (`--ttt-mark-x/o`) are game semantics only — never buttons, links, or generic accents.

## 4. Typography

| Style | Token | Bootstrap mapping | Usage |
| --- | --- | --- | --- |
| `text-display` | 48 px / 1.1 / 700 | — | Brand headline, "Tic Tac Toe" wordmark |
| `text-h1` | 32 px / 1.2 / 700 | `fs-1` | Page titles |
| `text-h2` | 24 px / 1.3 / 600 | `fs-3` | Section titles, board header |
| `text-body-lg` | 18 px / 1.5 / 400 | `fs-5` | Taglines, emphasized body |
| `text-body` | 16 px / 1.5 / 400 | base | Body copy |
| `text-small` | 14 px / 1.4 / 500 | `fs-6` | Labels, sidebar |
| `text-meta` | 12 px / 1.4 / 500 | `fs-6` + muted | Timestamps, hints, captions |
| `text-mono` | 14 px / 1.4 / 500 mono | — | Scores, error codes, countdown numbers |

Letter-spacing: −0.01 em on display/h1 only. Line length for readable copy: 60–75ch.

## 5. Spacing & Layout Scale

| Token | Value | Usage |
| --- | --- | --- |
| `--space-1` | 4 px | Icon gaps, inline padding |
| `--space-2` | 8 px | Cell grid gap, chip padding |
| `--space-3` | 12 px | Button padding-x, card internal gaps |
| `--space-4` | 16 px | Form field gaps |
| `--space-5` | 24 px | **Card padding, panel gaps** |
| `--space-6` | 32 px | Section spacing |
| `--space-7` | 48 px | Page top/chrome spacing |
| `--space-8` | 64 px | Page sections (desktop) |

Layout: container `max-width: 1140px` centered; form cards `max-width: 448px`; board `max-width: 560px`. Section gap 24 px between panels; page chrome (nav) 16 px horizontal.

## 6. Elevation & Radius

| Level | Shadow | Usage |
| --- | --- | --- |
| `shadow-card` | `0 1px 2px 0 rgb(2 6 23 / 0.50), 0 8px 24px -8px rgb(2 6 23 / 0.60)` | Cards, panels, board card |
| `shadow-overlay` | `0 12px 40px -8px rgb(2 6 23 / 0.80)` | Modals, toasts, invitation popup |
| `shadow-glow-primary` | `0 0 20px rgb(99 102 241 / 0.40)` | Primary CTA on auth/hero |
| `shadow-glow-x/o` | `0 0 16px rgb(.../0.35)` | Winning mark glow, turn indicator |

Radius: cards `rounded-4` (16 px), controls `rounded-3` (12 px), badges/toasts `rounded-pill` (9999 px). Depth must model elevation, never decoration: one card per screen carries `shadow-card`; nested panels use border only.

## 7. Component Inventory

Each component lists its states (rest / hover / focus / disabled / loading where applicable). Bootstrap utilities + `site.css` component classes; **no colors typed outside the tokens.**

### 7.1 Button

| Variant | Rest | Hover | Focus | Disabled | Loading |
| --- | --- | --- | --- | --- | --- |
| Primary (`btn-primary` + indigo var) | indigo-500 fill, slate-950 text, `rounded-3` | indigo-400 fill + `shadow-glow-primary` | 2 px indigo-400 ring, 2 px offset | slate-700 fill, slate-500 text | Spinner + label kept ("Joining…") |
| Secondary (`btn-outline`) | transparent, slate-700 border, slate-100 text | slate-800 fill | same ring | slate-700 border, slate-500 text | — |
| Danger (`btn-outline-danger`) | transparent, red-500 border, red-400 text | red-500/10 fill + red border | same ring (red) | slate-700, slate-500 | — |
| Ghost / link | indigo text only | underline offset 4 px | ring | — | — |

Touch target ≥ 44 px. On press: `scale(0.97)` 150 ms ([animations.md](animations.md) §3.9).

### 7.2 Input

Rest: slate-900 fill, slate-700 border, `rounded-3`, 12 px padding-x, always-visible label above. Focus: indigo-400 border + 3:1 ring. Error: red-500 border + red-400 message below (never color-only). Password field has the eye toggle. Disabled: slate-800 fill, slate-500 text.

### 7.3 Card

`ttt-card`: slate-900 fill, slate-800 border, `rounded-4`, 24 px padding, `shadow-card`. No other card style exists in the app.

### 7.4 Avatar

40 px circle, initials in 300-level tint text on `tint/15` fill (sky/rose/indigo palette by player status), always next to the name text (initials are decorative `aria-hidden`; the name is the accessible label).

### 7.5 Badge & StatusDot

Pill badge: tint/15 fill + tint text + dot + label — three statuses: Available (emerald), Invited (amber), In game (amber solid). The dot is an 8 px circle; **the label is never removed** (WCAG 1.4.1). Connection badge in the nav: emerald "Connected" / amber pulsing "Reconnecting…" / slate "Offline".

### 7.6 Toast

Bottom-right, slate-900 fill, `shadow-overlay`, `rounded-4`, icon + title + message + close. Kinds: info (indigo), success (emerald), warning/danger (amber/red). Auto-dismiss success 4 s; danger persists. Slides in 200 ms / fades 150 ms ([animations.md](animations.md) §3.13).

### 7.7 Modal (invitation popup)

The invitation card (`InvitationReceived`): centered modal (desktop) / bottom sheet (mobile), `shadow-overlay`, `rounded-4`, avatar + "X invited you to play" + Accept (primary) / Reject (ghost) + **30 s progress bar** draining under the buttons (approximate — the server's TTL always wins, never block a click on the bar). Esc = Reject (never Accept). Focus lands on Reject first ([animations.md](animations.md) §3.14). **One modal at a time:** the server guarantees at most one pending invitation per invitee, so a second popup cannot exist; if a result modal were ever visible when one arrives, the invitation waits underneath and appears on dismiss — both can never be interactive at once.

### 7.8 Tabs

(Not needed in v1 — history/stats are one page.) Reserved for settings sections; if added: underline-indicator tabs per primary token.

### 7.9 Spinner

Bootstrap spinner, indigo-400, 24 px (18 px small). Always paired with a label ("Joining…", "Saving…") — never a lone spinner.

### 7.10 Skeleton

Slate-800 shimmer blocks in list/card shapes, 1.5 s loop ([animations.md](animations.md) §3.10). Used while History/stats load; board cells use it while `GetRoomQuery` resolves.

### 7.11 GameBoard & Cell

Board card: slate-900 fill, `shadow-card`, grid 3×3, gap 8 px, cells slate-800 `rounded-3`, hover: lift + ghost mark preview (sky/rose at 40% — [animations.md](animations.md) §3.12), placed mark strokes in mark colors. Winning cells pulse `shadow-glow-x/o` + win line animates 500 ms ([animations.md](animations.md) §3.5). Invalid cell: soft shake 300 ms ([animations.md](animations.md) §3.7).

### 7.12 ConnectionBadge

Nav pill per §7.5. The "Reconnecting…" state pulses at 2 s loop ([animations.md](animations.md) §3.15); aria-live announces state changes politely.

### 7.13 Iconography

Bootstrap Icons (outline style, 16/20/24 px). Icon-only buttons get `aria-label` + `title`. Icons never appear without text labels in body copy.

### 7.14 Wordmark & favicon

| Asset | Spec | Notes |
| --- | --- | --- |
| Wordmark (nav/brand panel) | "Tic Tac Toe" in `text-display`, flanked by one sky X and one rose O glyph (28 px nav, 96 px brand panel) | Pure text/CSS; no image needed |
| `favicon.ico` + `icon-192.png` | 32 px rounded square (slate-950 fill), X sky-400 stroke, O rose-400 stroke | The wordmark beside it is the accessible name; the glyphs are decorative `aria-hidden` |

## 8. Empty States

| Page | Empty state |
| --- | --- |
| Home (no other players online) | "No one else is online right now." + ghost link "Play vs computer" |
| History | "No games yet — play your first game!" + primary CTA "Play vs computer" |
| Stats | Zeroed chips (0/0/0) + hint text |

Empty states always carry a **forward action** (primary/ghost CTA), never a dead end.

## 9. Focus Ring Spec

- `outline: 2px solid var(--ttt-primary-hover); outline-offset: 2px` on all interactive elements when focused (`:focus-visible`).
- The ring color (indigo-400) is the only focus color; danger contexts use the same ring (always visible against slate surfaces).
- Focus order = visual order (DOM order is the visual order; no positive `tabindex` anywhere).

## 10. Accessibility Foundations

| Concern | Rule |
| --- | --- |
| Text contrast | ≥ 4.5:1 normal text on its surface (§3 table) |
| Non-text contrast | Focus ring and interactive boundaries ≥ 3:1 |
| Status | Always dot **+ text label** (never color alone) |
| Touch | ≥ 44 px targets; board cells ≥ 64 px |
| Motion | `prefers-reduced-motion` disables every animation ([animations.md](animations.md) §4) |
| Announcements | `aria-live="polite"` on status/connection/badge text; board announces moves (`aria-live` on turn indicator) |
| Full keyboard | Every action reachable and operable keyboard-only (board cells are `<button>`s) |

## Assumptions

- Dark-first per §1; light theme is a token swap through Bootstrap's `data-bs-theme="light"` — no component rework.
- No avatar APIs (privacy); initials are generated locally.
- Component classes live in `wwwroot/css/site.css` (`ttt-*` prefix) — Bootstrap utilities first, custom CSS only where a token or a state needs it.
- Sounds, confetti and countdown rings are progressive enhancement: with JS disabled the app still plays correctly (server-authoritative).
- **Production guarantee:** every enhancement is null-guarded — a missing sound file, a blocked font, or a failed confetti load degrades to silence or a plain page, never a broken render, a console crash, or a blank board.
