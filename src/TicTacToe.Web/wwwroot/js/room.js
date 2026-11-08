// Online room — render-only. Server RoomState replaces the board wholesale (never merges).
import { sounds } from './sound.js';

const roomId = document.querySelector('meta[name="ttt:room-id"]')?.content;
const boardEl = document.getElementById('board');
const statusEl = document.getElementById('room-status');
const errorEl = document.getElementById('room-error');
const graceBanner = document.getElementById('grace-banner');
const graceSeconds = document.getElementById('grace-seconds');
const leaveBtn = document.getElementById('btn-leave');
const rematchBtn = document.getElementById('btn-rematch');
const myId = document.querySelector('meta[name="ttt:user-id"]')?.content || '';

let hubConn = null;
let state = null;
let graceTimer = null;
let pollTimer = null;

async function startHub() {
  hubConn = new signalR.HubConnectionBuilder().withUrl('/hubs/game').withAutomaticReconnect().build();
  hubConn.on('MoveApplied', (p) => render(p.room));
  hubConn.on('RematchStarted', (p) => { render(p.room); toast('Rematch started — X to move.', 'success'); });
  hubConn.on('PlayerDisconnected', (p) => startGrace(p.secondsLeft || 60));
  hubConn.on('PlayerReconnected', () => stopGrace());
  hubConn.on('RoomClosed', (p) => { toast(`Room closed (${p.reason}).`, 'info'); statusEl.textContent = 'Room closed.'; });
  hubConn.on('PlayerLeft', (p) => { if (p?.room) render(p.room); });
  await hubConn.start();
  await loadRoom();
  pollTimer = setInterval(loadRoom, 5000);
}

async function loadRoom() {
  if (!hubConn) return;
  const res = await hubConn.invoke('GetRoom', roomId);
  if (res?.error) { errorEl.hidden = false; errorEl.textContent = res.error.message || 'Room not found.'; return; }
  if (res?.value) render(res.value);
}

function statusOf(room) {
  // Wire enums are numbers (RoomStatus Playing=0, Finished=1, Closed=2) — accept strings too.
  return room.status === 0 || room.status === 'Playing' ? 'Playing'
    : room.status === 1 || room.status === 'Finished' ? 'Finished' : 'Closed';
}

function render(room) {
  state = room;
  const st = statusOf(room);
  const cells = boardEl.querySelectorAll('.ttt-cell');
  cells.forEach((cell, i) => {
    const v = room.board?.[i];
    cell.textContent = v === 0 ? 'X' : v === 1 ? 'O' : '';
    cell.disabled = st !== 'Playing' || !isMyTurn(room);
    cell.setAttribute('aria-label', `Row ${Math.floor(i / 3) + 1}, column ${(i % 3) + 1}, ${v === 0 ? 'X' : v === 1 ? 'O' : 'empty'}`);
  });
  if (st === 'Playing') {
    statusEl.textContent = isMyTurn(room) ? 'Your turn' : "Opponent's turn";
    rematchBtn.hidden = true;
    errorEl.hidden = true;
  } else if (st === 'Finished') {
    const r = room.result;
    statusEl.textContent = r ? (r.outcome === 3 ? "It's a draw." : myMark(room) === r.outcome ? 'You win!' : 'Opponent wins.') : 'Finished.';
    rematchBtn.hidden = false;
  } else {
    statusEl.textContent = 'Room closed.';
    rematchBtn.hidden = true;
    cells.forEach(c => c.disabled = true);
  }
}

function sameId(a, b) { return String(a || '').toLowerCase() === String(b || '').toLowerCase(); }
function myMark(room) { return sameId(room.playerXId, myId) ? 0 : 1; }
function isMyTurn(room) {
  const turn = room.turn === 0 ? 0 : 1;
  return turn === myMark(room);
}

boardEl.addEventListener('click', async (e) => {
  const btn = e.target.closest('.ttt-cell');
  if (!btn || btn.disabled || !hubConn) return;
  const index = Number(btn.dataset.index);
  const res = await hubConn.invoke('MakeMove', roomId, index);
  if (res?.error) {
    errorEl.hidden = false;
    errorEl.textContent = res.error.message || 'Move failed.';
    if (res.error.code === 'Game.CellOccupied' || res.error.code === 'Game.NotYourTurn') await loadRoom();
  }
});

leaveBtn.addEventListener('click', async () => {
  if (!confirm('Leave counts as a loss. Continue?')) return;
  await hubConn.invoke('Leave', roomId);
  window.location.href = '/';
});

rematchBtn.addEventListener('click', async () => {
  rematchBtn.disabled = true;
  const res = await hubConn.invoke('RequestRematch', roomId);
  if (res?.error && res.error.code !== 'Game.AlreadyOver') toast(res.error.message || 'Rematch not ready.', 'info');
  if (res?.value) render(res.value);
  rematchBtn.disabled = false;
});

function startGrace(secondsLeft) {
  let left = Number(secondsLeft) || 60;
  graceBanner.hidden = false;
  graceSeconds.textContent = String(left);
  if (graceTimer) clearInterval(graceTimer);
  graceTimer = setInterval(() => {
    left -= 1;
    graceSeconds.textContent = String(Math.max(0, left));
    if (left <= 0) stopGrace();
  }, 1000);
}
function stopGrace() {
  graceBanner.hidden = true;
  if (graceTimer) { clearInterval(graceTimer); graceTimer = null; }
}

function toast(message, kind = 'info') {
  const t = document.createElement('div');
  t.className = `ttt-toast show ttt-toast-${kind}`;
  t.setAttribute('role', 'status');
  t.textContent = message;
  document.body.appendChild(t);
  setTimeout(() => { t.classList.remove('show'); setTimeout(() => t.remove(), 170); }, 4000);
}

startHub();
