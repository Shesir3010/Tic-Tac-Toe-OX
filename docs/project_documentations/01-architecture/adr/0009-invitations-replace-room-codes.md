# ADR-0009: Invitations and Site-Wide Presence Replace Room Codes

*Online play starts from a live list of who's online, not a shared code — records the two 2026-11-07 decisions and everything they simplify away.*

**Status:** Accepted (2026-11-07) · **Supersedes:** FR-23, FR-24 and the idle-room-expiry clause of FR-30 (see [`../../00-product/PRD.md`](../../00-product/PRD.md) §9) · **Amends:** [ADR-0005](0005-in-memory-room-store.md) (no Waiting rooms, no idle expiry) · **Referenced by:** [`../realtime-signalr.md`](../realtime-signalr.md) · [`../../00-product/user-stories.md`](../../00-product/user-stories.md) (US-15…US-17)

## Context

The room-code flow (create → share 5-character code → friend joins by code) required both players to have the code, a screen to paste it on, and an idle-room expiry for codes that nobody used. User review (2026-11-07) replaced the model: **invitations** between currently-online players, delivered as an in-page popup anywhere on the site. The review itself posed the two decision points settled here:

1. **What "online" means** — logged in with the site open, vs only "on the Lobby page".
2. **Keep codes at all?** — invitations only, vs invitations + code fallback.

## Decision

**Decision 1 — site-wide presence.** A signed-in player holds one hub connection on **every page** of the site; "online" = logged in with ≥ 1 live connection. Presence is maintained by an in-memory `PresenceTracker` (Singleton, same discipline as `IRoomStore` — userId → connection count).

*Rationale:* the popup must reach the invitee wherever they are playing/browsing — Lobby-page-only presence would silently miss most invitations and make the feature feel broken. Cost is negligible: one connection per logged-in player, reused for presence, invitations and rooms.

**Decision 2 — invitations only; room codes removed.** There is no create-with-code, no join-by-code, no invite link. A room is created **atomically at the moment of acceptance**, inviter = X, both players go straight to the board. Waiting rooms, empty-room expiry and code uniqueness for users disappear as a consequence.

*Rationale:* the simplest honest design; the accepted trade-off is that two friends can play only when **both are signed in simultaneously** — the natural social case for this product. Codes surviving as a fallback would double the state surface (two entry flows, two guard sets, duplicated UI) for one persona job.

**The invitation contract (from the review, now normative):**

| Rule | Value |
| --- | --- |
| Visible statuses | Available · Invited (pending in either direction) · In game · (offline: not listed) |
| One outgoing invitation per inviter | Yes; Cancel clears it for both parties |
| Delivery | Accept/Reject popup on **every page** while signed in ([FR-34](../../00-product/PRD.md)) |
| Expiry | Unanswered for **~30 s** → inviter notified |
| Rejection cooldown | Same inviter cannot re-invite the same player for **~30 s** |
| Guards ([FR-36](../../00-product/PRD.md)) | Not self · not offline · not In game · not already Invited · not on cooldown |
| Privacy | Usernames only; guests see neither the list nor invite controls ([FR-31](../../00-product/PRD.md)) |
| On accept | Room created atomically; invitation terminal; room lifecycle takes over (60 s grace, forfeits, saves) — unchanged from the original design |

## Consequences

**Positive:** removes an entire flow (codes: generation, display, copy, join-with-code, code errors `Room.NotFound/Full/Expired`, idle-expiry cleanup); invites read as "who can I play with right now" — stronger product moment than paste-a-code; count of distinct failure messages drops.

**Negative:** both players must be signed in at the same time — explicit, accepted trade-off; popup-on-every-page means every Razor view must maintain the hub connection (one layout-level bootstrap, not per-page wiring); presence/invitation races need their own guarantees (rows 7–10 in [`../realtime-signalr.md`](../realtime-signalr.md) §6).

**Neutral:** rooms keep an internal opaque id for URLs (`/Rooms/{id}`) — never user-facing, so no unguessability requirement; the per-room lock and all match-save rules carry over untouched.

## Implementation sketch (what this decision mandates) — state-container detail: [ADR-0014](0014-in-memory-state-trio.md)

```
Infrastructure/Services/
  PresenceTracker.cs      userId → connectionCount; OnConnected/OnDisconnected flips presence
  InvitationManager.cs    pending invitations; guards; 30 s expiry + 30 s cooldown via TimeProvider;
                          atomic accept = invitation state + room creation in one locked flow
Domain/Online/
  Invitation.cs           InvitationId, FromUserId, ToUserId, status (Pending/Accepted/Rejected/
                          Expired/Cancelled), CreatedAtUtc, ExpiresAtUtc
Hub flow (Cortex commands): SendInvitationCommand · CancelInvitationCommand ·
  RespondToInvitationCommand · GetOnlinePlayersQuery — the hub only dispatches and broadcasts
```

## Alternatives considered

| Alternative | Why rejected |
| --- | --- |
| Lobby-page-only presence | Simpler, but invitations miss anyone browsing elsewhere — contradicts the popup-anywhere design |
| Invitations **+** code fallback | Two entry flows to test and secure for one job; the "friend is online but I can't spot them" case is solved by the live list itself |
| Friend-list-gated invitations | Friends are explicitly out for v1 ([v1-scope §4](../../00-product/v1-scope.md)); inviting anyone online is the friend-free, spam-guarded middle ground |

## Links

- Product wording: [`../../00-product/PRD.md`](../../00-product/PRD.md) §4.3, FR-32…FR-36
- Stories: [`../../00-product/user-stories.md`](../../00-product/user-stories.md) US-15…US-17
- Hub contract & invitation races: [`../realtime-signalr.md`](../realtime-signalr.md)

## Assumptions

- "~30 s" invitation values (expiry + cooldown) are config via `RoomSettings`-style singleton options, not literals in code — tweakable without touching logic.
- The offline-immediately rule (no extra grace presence) is deliberate: an offline invitee is either refunded by expiry or cancelled by the offline notice within a second or two; no third timing is introduced.
- Presence list updates are broadcast on change, not polled — clients hold no timers for the list.
