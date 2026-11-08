// Site-wide hub connection (FR-32 site-wide presence). One connection per signed-in page.
const conn = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/game')
  .withAutomaticReconnect({ nextRetryDelayInMilliseconds: 1000 })
  .build();

const myId = document.querySelector('meta[name="ttt:user-id"]')?.content || '';

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
      btn.textContent = 'Invite';
      btn.addEventListener('click', async () => {
        // Pressed state: disable immediately so a second click can't double-invite.
        btn.disabled = true;
        btn.textContent = 'Waiting…';
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
          const card = document.getElementById('waiting-card');
          card.hidden = false;
          document.getElementById('waiting-name').textContent = p.displayName;
          document.getElementById('btn-cancel-invite').onclick = () => conn.invoke('CancelInvitation', res.value.invitationId).finally(() => { card.hidden = true; btn.disabled = false; btn.textContent = 'Invite'; btn.classList.remove('is-pressed'); });
        }
      });
      wrap.appendChild(btn);
    }
    li.append(av, wrap);
    list.appendChild(li);
  });
}

conn.on('OnlinePlayersUpdated', (payload) => renderOnline(payload?.players || payload || []));

function showPopup(title, text, invitationId) {
  const popup = document.getElementById('invite-popup');
  document.getElementById('popup-title').textContent = title;
  document.getElementById('popup-text').textContent = text;
  popup.hidden = false;
  popup.classList.add('show'); popup.style.display = 'block';
  document.getElementById('popup-reject').focus();
  const bar = document.getElementById('popup-bar');
  let end = Date.now() + 30000;
  const tick = setInterval(() => {
    const left = Math.max(0, end - Date.now());
    bar.style.width = `${(left / 30000) * 100}%`;
    if (left <= 0) clearInterval(tick);
  }, 500);
  document.getElementById('popup-accept').onclick = () => { conn.invoke('RespondToInvitation', invitationId, true).finally(() => hidePopup()); };
  document.getElementById('popup-reject').onclick = () => { conn.invoke('RespondToInvitation', invitationId, false).finally(() => hidePopup()); };
}

function hidePopup() {
  const popup = document.getElementById('invite-popup');
  popup.hidden = true;
  popup.style.display = 'none';
  popup.classList.remove('show');
}

conn.on('InvitationReceived', (p) => showPopup('Invite', `${p.fromName || 'Player'} invited you to play`, p.invitationId));
conn.on('InvitationDeclined', (p) => toast(p.byName ? `${p.byName} declined.` : 'Declined.', 'info'));
conn.on('InvitationAccepted', () => toast('Room created! Online play continues in M11 room page.', 'success'));

export async function startHub() {
  try {
    await conn.start();
    const initial = await conn.invoke('GetOnlinePlayers');
    renderOnline(initial?.value || initial?.players || []);
  } catch {
    /* Hub is best-effort for presence; game still works without it. */
  }
}
