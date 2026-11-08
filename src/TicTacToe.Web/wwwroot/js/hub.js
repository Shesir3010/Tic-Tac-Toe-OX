// Site-wide hub connection (FR-32 site-wide presence). One connection per signed-in page.
const conn = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/game')
  .withAutomaticReconnect({ nextRetryDelayInMilliseconds: 1000 })
  .build();

const myId = document.querySelector('meta[name="ttt:user-id"]')?.content || '';
let pendingInvitationId = null;
let popupTimer = null;
let popupInvitationId = null;

function initials(name) {
  return (name || '?').trim().slice(0, 2).toUpperCase();
}

function toast(message, kind = 'info') {
  const t = document.createElement('div');
  t.className = `ttt-toast show ttt-toast-${kind}`;
  t.setAttribute('role', kind === 'error' ? 'alert' : 'status');
  t.textContent = message;
  const close = document.createElement('button');
  close.className = 'btn btn-sm btn-link';
  close.setAttribute('aria-label', 'Dismiss');
  close.textContent = '×';
  t.appendChild(close);
  close.addEventListener('click', () => t.remove());
  document.body.appendChild(t);
  setTimeout(() => { t.classList.remove('show'); setTimeout(() => t.remove(), 170); }, kind === 'error' ? 6000 : 4000);
}

function showWaitingCard(name, invitationId) {
  pendingInvitationId = invitationId;
  const card = document.getElementById('waiting-card');
  const label = document.getElementById('waiting-label');
  if (!card) return;
  card.hidden = false;
  if (label) label.textContent = `Waiting for ${name}…`;
  const cancel = document.getElementById('btn-cancel-invite');
  if (cancel) cancel.onclick = async () => {
    try { await conn.invoke('CancelInvitation', invitationId); } finally { hideWaitingCard(); toast('Invitation cancelled.', 'info'); }
  };
}

function hideWaitingCard() {
  pendingInvitationId = null;
  const card = document.getElementById('waiting-card');
  if (card) card.hidden = true;
  refreshOnlineList();
}

function showRoomStatus(message) {
  const el = document.getElementById('room-status');
  const msg = document.getElementById('room-status-text');
  if (!el || !msg) return;
  hideInviteStatus();
  el.hidden = false;
  msg.textContent = message;
  el.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

function hideRoomStatus() {
  const el = document.getElementById('room-status');
  if (el) el.hidden = true;
}

function showInviteStatus(message) {
  const el = document.getElementById('invite-status');
  const msg = document.getElementById('invite-status-text');
  if (!el || !msg) return;
  hideRoomStatus();
  el.hidden = false;
  msg.textContent = message;
}

function hideInviteStatus() {
  const el = document.getElementById('invite-status');
  if (el) el.hidden = true;
}

function refreshOnlineList() {
  conn.invoke('GetOnlinePlayers').then((res) => renderOnline(res?.value || res?.players || [])).catch(() => { /* best-effort */ });
}

function renderOnline(players) {
  const list = document.getElementById('online-list');
  const count = document.getElementById('lobby-count');
  if (!list) return;
  list.innerHTML = '';
  const arr = players || [];
  if (count) count.textContent = `${arr.length} online`;
  arr.forEach(p => {
    const isSelf = p.userId === myId;
    const li = document.createElement('li');
    li.className = 'ttt-player';
    const av = document.createElement('span');
    av.className = 'ttt-avatar';
    av.textContent = initials(p.displayName);
    av.setAttribute('aria-hidden', 'true');
    const name = document.createElement('span');
    name.className = 'ttt-player-name';
    name.textContent = isSelf ? `${p.displayName} (You)` : p.displayName;
    const status = document.createElement('span');
    status.className = 'ttt-badge-status';
    status.textContent = 'Available';
    const wrap = document.createElement('span');
    wrap.className = 'd-inline-flex align-items-center gap-2';
    wrap.append(name, status);
    if (!isSelf) {
      const btn = document.createElement('button');
      btn.type = 'button';
      btn.className = 'btn btn-sm btn-primary';
      // One outgoing at a time (FR-33): while pending, every Invite is disabled
      // so the user can't hit Invite.AlreadyPending by accident.
      const locked = !!pendingInvitationId;
      btn.textContent = locked ? 'Waiting…' : 'Invite';
      if (locked) { btn.disabled = true; btn.classList.add('is-pressed'); btn.setAttribute('aria-pressed', 'true'); }
      btn.addEventListener('click', async () => {
        btn.disabled = true;
        btn.textContent = 'Sending…';
        btn.classList.add('is-pressed');
        btn.setAttribute('aria-pressed', 'true');
        const res = await conn.invoke('InvitePlayer', p.userId);
        const err = res?.error;
        if (err) {
          toast(err.message || 'Invite failed', 'error');
          btn.disabled = false;
          btn.textContent = 'Invite';
          btn.classList.remove('is-pressed');
          btn.removeAttribute('aria-pressed');
        } else if (res?.value) {
          showWaitingCard(p.displayName, res.value.invitationId);
          toast(`Invitation sent to ${p.displayName}.`, 'info');
        }
      });
      wrap.appendChild(btn);
    }
    li.append(av, wrap);
    list.appendChild(li);
  });
}

conn.on('OnlinePlayersUpdated', (payload) => renderOnline(payload?.players || payload || []));

function showPopup(fromName, invitationId) {
  popupInvitationId = invitationId;
  const popup = document.getElementById('invite-popup');
  const backdrop = document.getElementById('invite-backdrop');
  const avatar = document.getElementById('popup-avatar');
  const nameEl = document.getElementById('popup-from-name');
  const countdownEl = document.getElementById('popup-countdown');
  if (avatar) avatar.textContent = initials(fromName);
  if (nameEl) nameEl.textContent = fromName || 'Player';
  const textEl = document.getElementById('popup-text');
  if (textEl) textEl.textContent = `${fromName || 'Player'} wants to play Tic Tac Toe with you.`;
  if (backdrop) backdrop.hidden = false;
  popup.hidden = false;
  popup.classList.add('show'); popup.style.display = 'flex';
  document.getElementById('popup-reject')?.focus();
  const bar = document.getElementById('popup-bar');
  if (popupTimer) clearInterval(popupTimer);
  let left = 30;
  const renderBar = () => {
    if (bar) bar.style.width = `${(left / 30) * 100}%`;
    if (countdownEl) countdownEl.textContent = `Expires in ${left}s`;
  };
  renderBar();
  popupTimer = setInterval(() => {
    left -= 1;
    renderBar();
    if (left <= 0) {
      clearInterval(popupTimer); popupTimer = null;
      conn.invoke('RespondToInvitation', invitationId, false).finally(() => {
        hidePopup();
        toast('Invitation expired.', 'info');
      });
    }
  }, 1000);
  const acceptBtn = document.getElementById('popup-accept');
  const rejectBtn = document.getElementById('popup-reject');
  if (acceptBtn) acceptBtn.onclick = async () => {
    acceptBtn.disabled = true; rejectBtn.disabled = true;
    acceptBtn.textContent = 'Accepting…';
    try {
      const res = await conn.invoke('RespondToInvitation', invitationId, true);
      if (res?.error) toast(res.error.message || 'Accept failed.', 'error');
      else showRoomStatus('Accepted! Room created. You are O — online play is coming in M11.');
    } finally { hidePopup(); }
  };
  if (rejectBtn) rejectBtn.onclick = async () => {
    acceptBtn.disabled = true; rejectBtn.disabled = true;
    rejectBtn.textContent = 'Rejecting…';
    try { await conn.invoke('RespondToInvitation', invitationId, false); showInviteStatus('You declined the invitation.'); } finally { hidePopup(); toast('Invitation rejected.', 'info'); }
  };
  const closeBtn = document.getElementById('popup-close');
  if (closeBtn) closeBtn.onclick = () => rejectBtn && rejectBtn.onclick && rejectBtn.onclick();
  const onKey = (e) => { if (e.key === 'Escape' && !popup.hidden) rejectBtn?.click(); };
  document.addEventListener('keydown', onKey);
  popup._onKey = onKey;
}

function hidePopup() {
  const popup = document.getElementById('invite-popup');
  const backdrop = document.getElementById('invite-backdrop');
  if (popupTimer) { clearInterval(popupTimer); popupTimer = null; }
  if (popup._onKey) document.removeEventListener('keydown', popup._onKey);
  if (popup) { popup.hidden = true; popup.style.display = 'none'; popup.classList.remove('show'); }
  if (backdrop) backdrop.hidden = true;
  popupInvitationId = null;
  const a = document.getElementById('popup-accept');
  const r = document.getElementById('popup-reject');
  if (a) { a.disabled = false; a.textContent = 'Accept'; }
  if (r) { r.disabled = false; r.textContent = 'Reject'; }
}

conn.on('InvitationReceived', (p) => showPopup(p.fromName, p.invitationId));
conn.on('InvitationDeclined', (p) => {
  hideWaitingCard();
  hidePopup();
  const msg = p?.byName ? `${p.byName} declined your invitation. You can invite again after 30s cooldown.` : 'Your invitation was declined. You can invite again after 30s cooldown.';
  showInviteStatus(msg);
  toast(msg, 'info');
});
conn.on('InvitationAccepted', (p) => {
  hideWaitingCard();
  hidePopup();
  const room = p?.room;
  if (room) {
    const isX = room.playerXId === myId;
    const symbol = isX ? 'X' : 'O';
    showRoomStatus(`Room ${room.id.slice(0, 8)}… ready — you are ${symbol}. Online play continues next (M11).`);
  } else {
    showRoomStatus('Invitation accepted! Room created.');
  }
  toast('Invitation accepted! Room created.', 'success');
});
conn.on('InvitationCancelled', (p) => {
  if (p?.invitationId && popupInvitationId === p.invitationId) hidePopup();
  if (p?.invitationId && pendingInvitationId === p.invitationId) hideWaitingCard(); else if (!popupInvitationId) hideWaitingCard();
  const msg = p?.reason === 'offline' ? 'The other player went offline. Invitation cancelled.' : 'Invitation cancelled.';
  showInviteStatus(msg);
  toast(msg, 'info');
});

export async function startHub() {
  try {
    await conn.start();
    const initial = await conn.invoke('GetOnlinePlayers');
    renderOnline(initial?.value || initial?.players || []);
  } catch {
    /* Hub is best-effort for presence; game still works without it. */
  }
}
