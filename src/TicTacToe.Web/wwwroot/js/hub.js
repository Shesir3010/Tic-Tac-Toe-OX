// Site-wide hub connection (FR-32 site-wide presence). One connection per signed-in page.
const conn = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/game')
  .withAutomaticReconnect({ nextRetryDelayInMilliseconds: 1000 })
  .build();

function initials(name) {
  return (name || '?').trim().slice(0, 2).toUpperCase();
}

function renderOnline(players) {
  const list = document.getElementById('online-list');
  const count = document.getElementById('lobby-count');
  if (!list) return;
  list.innerHTML = '';
  const arr = players || [];
  if (count) count.textContent = `${arr.length} online`;
  arr.forEach(p => {
    const li = document.createElement('li');
    li.className = 'ttt-player';
    const av = document.createElement('span');
    av.className = 'ttt-avatar';
    av.textContent = initials(p.displayName);
    av.setAttribute('aria-hidden', 'true');
    const name = document.createElement('span');
    name.className = 'ttt-player-name';
    name.textContent = p.displayName;
    const status = document.createElement('span');
    status.className = 'ttt-badge-status';
    status.textContent = 'Available';
    li.append(av, name, status);
    list.appendChild(li);
  });
}

conn.on('OnlinePlayersUpdated', (payload) => renderOnline(payload?.players || payload || []));

export async function startHub() {
  try {
    await conn.start();
    const initial = await conn.invoke('GetOnlinePlayers');
    renderOnline(initial?.value || initial?.players || []);
  } catch {
    /* Hub is best-effort for presence; game still works without it. */
  }
}
