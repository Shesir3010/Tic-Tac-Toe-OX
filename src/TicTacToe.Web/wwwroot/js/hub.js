// Site-wide hub connection (FR-32 site-wide presence). One connection per signed-in page.
const conn = new signalR.HubConnectionBuilder()
  .withUrl('/hubs/game')
  .withAutomaticReconnect({ nextRetryDelayInMilliseconds: 1000 })
  .build();

function renderOnline(players) {
  const list = document.getElementById('online-list');
  if (!list) return;
  list.innerHTML = '';
  (players || []).forEach(p => {
    const li = document.createElement('li');
    li.className = 'list-group-item d-flex justify-content-between';
    const name = document.createElement('span');
    name.textContent = p.displayName;
    const status = document.createElement('span');
    status.className = 'badge bg-success';
    status.textContent = 'Available';
    li.append(name, status);
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
