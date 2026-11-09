// Online room — render-only. Server RoomState replaces the board wholesale (never merges).
const roomId = document.querySelector('meta[name="ttt:room-id"]')?.content;
const boardEl = document.getElementById('board');
const statusEl = document.getElementById('room-status');
const errorEl = document.getElementById('room-error');
const graceBanner = document.getElementById('grace-banner');
const graceSeconds = document.getElementById('grace-seconds');
const leaveBtn = document.getElementById('btn-leave');
const rematchBtn = document.getElementById('btn-rematch');
const rematchStatus = document.getElementById('rematch-status');
const rematchText = document.getElementById('rematch-status-text');
const leaveModal = document.getElementById('leave-modal');
const btnLeaveCancel = document.getElementById('btn-leave-cancel');
const btnLeaveConfirm = document.getElementById('btn-leave-confirm');
const leftModal = document.getElementById('left-modal');
const btnLeftHome = document.getElementById('btn-left-home');
const winModal = document.getElementById('win-modal');
const btnWinHome = document.getElementById('btn-win-home');
const myId = document.querySelector('meta[name="ttt:user-id"]')?.content || '';
const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

const winOverlay = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
winOverlay.setAttribute('id', 'win-line');
winOverlay.setAttribute('viewBox', '0 0 100 100');
winOverlay.setAttribute('preserveAspectRatio', 'none');
winOverlay.classList.add('ttt-win-line');
winOverlay.hidden = true;
winOverlay.setAttribute('aria-hidden', 'true');
boardEl.appendChild(winOverlay);

let hubConn = null;
let state = null;
let graceTimer = null;
let pollTimer = null;
let rematchRequested = false;
let wasInGrace = false;
let prevStatus = null;

function drawWinLine(line) {
  if (!line || line.length !== 3 || reducedMotion) { winOverlay.hidden = true; winOverlay.innerHTML = ''; return; }
  const [a, , c] = line;
  const btns = boardEl.querySelectorAll('.ttt-cell');
  const boardRect = boardEl.getBoundingClientRect();
  const aRect = btns[a].getBoundingClientRect();
  const cRect = btns[c].getBoundingClientRect();
  const ax = ((aRect.left - boardRect.left) + aRect.width / 2) / boardRect.width * 100;
  const ay = ((aRect.top - boardRect.top) + aRect.height / 2) / boardRect.height * 100;
  const cx = ((cRect.left - boardRect.left) + cRect.width / 2) / boardRect.width * 100;
  const cy = ((cRect.top - boardRect.top) + cRect.height / 2) / boardRect.height * 100;
  winOverlay.innerHTML = `<line x1="${ax}%" y1="${ay}%" x2="${cx}%" y2="${cy}%"></line>`;
  winOverlay.hidden = false;
}

async function startHub() {
  hubConn = new signalR.HubConnectionBuilder().withUrl('/hubs/game').withAutomaticReconnect().build();
  hubConn.on('MoveApplied', (p) => render(p.room));
  hubConn.on('RematchStarted', (p) => { rematchRequested = false; render(p.room); toast('Rematch started — X to move.', 'success'); });
  hubConn.on('RematchRequested', (p) => { showRematch(`Rematch requested by ${p.byName || 'opponent'} — press Rematch to accept.`); });
  hubConn.on('PlayerDisconnected', (p) => startGrace(p.secondsLeft || 60));
  hubConn.on('PlayerReconnected', () => stopGrace());
  hubConn.on('RoomClosed', (p) => {
    toast(`Room closed (${p.reason}).`, 'info');
    if (state) render(state); else statusEl.textContent = 'Room closed.';
  });
  hubConn.on('PlayerLeft', (p) => {
    if (!p?.room) { showLeftModal('Opponent left the game. You win!'); return; }
    render(p.room);
    // Mid-game leave (Finished = forfeit): staying player wins and returns home.
    // Post-game leave (Closed): result already shown — just offer home, no winner claim.
    if (statusOf(p.room) === 'Finished') {
      showLeftModal('Opponent left the game. You win!');
      setTimeout(() => { if (leftModal.hidden === false) window.location.href = '/'; }, 4000);
    } else if (statusOf(p.room) === 'Closed') {
      showLeftModal('Opponent left. Room closed.');
      setTimeout(() => { if (leftModal.hidden === false) window.location.href = '/'; }, 6000);
    }
  });
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
  return room.status === 0 || room.status === 'Playing' ? 'Playing'
    : room.status === 1 || room.status === 'Finished' ? 'Finished' : 'Closed';
}
function sameId(a, b) { return String(a || '').toLowerCase() === String(b || '').toLowerCase(); }
function myMark(room) { return sameId(room.playerXId, myId) ? 0 : 1; }
function isMyTurn(room) {
  const turn = room.turn === 0 ? 0 : 1;
  return turn === myMark(room);
}

function render(room) {
  const prev = prevStatus;
  state = room;
  const st = statusOf(room);
  prevStatus = st;
  const cells = boardEl.querySelectorAll('.ttt-cell');
  const result = room.result;
  const winning = result?.winningLine || result?.WinningLine;
  cells.forEach((cell, i) => {
    const v = room.board?.[i];
    cell.textContent = v === 0 ? 'X' : v === 1 ? 'O' : '';
    cell.classList.toggle('ttt-mark-x', v === 0);
    cell.classList.toggle('ttt-mark-o', v === 1);
    cell.classList.remove('ttt-winning');
    cell.disabled = st !== 'Playing' || !isMyTurn(room);
    cell.setAttribute('aria-label', `Row ${Math.floor(i / 3) + 1}, column ${(i % 3) + 1}, ${v === 0 ? 'X' : v === 1 ? 'O' : 'empty'}`);
  });
  if ((st === 'Finished' || st === 'Closed') && winning) {
    winning.forEach(i => cells[i].classList.add('ttt-winning'));
    drawWinLine(winning);
  } else {
    drawWinLine(null);
  }
  const markLabel = myMark(room) === 0 ? 'X' : 'O';
  const winText = (r) => {
    const outcome = r?.outcome ?? r?.Outcome;
    if (outcome === 3) return "It's a draw.";
    if (outcome === 1) return myMark(room) === 0 ? 'You win!' : 'X wins!';
    if (outcome === 2) return myMark(room) === 1 ? 'You win!' : 'O wins!';
    return 'Finished.';
  };
  if (st === 'Playing') {
    statusEl.textContent = isMyTurn(room) ? `Your turn as ${markLabel}` : `Opponent's turn`;
    rematchBtn.hidden = true;
    hideRematch();
    errorEl.hidden = true;
  } else if (st === 'Finished') {
    let text = winText(result);
    if (rematchRequested) text += ' Waiting for opponent…';
    statusEl.textContent = text;
    rematchBtn.hidden = false;
    rematchBtn.disabled = rematchRequested;
    rematchBtn.textContent = rematchRequested ? 'Waiting…' : 'Rematch';
    errorEl.hidden = true;
    // Offline-timeout forfeit: we were in grace, now Finished with our win → win popup + home.
    if (wasInGrace && prev === 'Playing' && text === 'You win!') {
      stopGrace();
      showWinModal();
      setTimeout(() => { if (winModal.hidden === false) window.location.href = '/'; }, 3000);
    }
  } else {
    statusEl.textContent = `Room closed. ${winText(result)}`;
    rematchBtn.hidden = true;
    hideRematch();
    cells.forEach(c => c.disabled = true);
    drawWinLine(null);
  }
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

leaveBtn.addEventListener('click', () => { leaveModal.hidden = false; btnLeaveCancel.focus(); });
btnLeaveCancel.addEventListener('click', () => { leaveModal.hidden = true; });
leaveModal.addEventListener('click', (e) => { if (e.target === leaveModal) leaveModal.hidden = true; });
document.addEventListener('keydown', (e) => {
  if (e.key === 'Escape') { leaveModal.hidden = true; }
});
btnLeaveConfirm.addEventListener('click', async () => {
  leaveModal.hidden = true;
  await hubConn.invoke('Leave', roomId);
  window.location.href = '/';
});

rematchBtn.addEventListener('click', async () => {
  rematchBtn.disabled = true;
  const res = await hubConn.invoke('RequestRematch', roomId);
  if (res?.error) {
    if (res.error.code === 'Game.AlreadyOver') { rematchRequested = true; showRematch('Rematch requested — waiting for opponent…'); render(state); }
    else toast(res.error.message || 'Rematch not ready.', 'info');
  } else if (res?.value) {
    if (statusOf(res.value) === 'Playing') { rematchRequested = false; render(res.value); }
    else { rematchRequested = true; showRematch('Rematch requested — waiting for opponent…'); render(res.value); }
  }
  rematchBtn.disabled = rematchRequested;
});

btnLeftHome.addEventListener('click', () => { window.location.href = '/'; });
btnWinHome.addEventListener('click', () => { window.location.href = '/'; });
function showWinModal() { winModal.hidden = false; btnWinHome.focus(); }
function showLeftModal(msg) {
  document.querySelector('#left-modal p').textContent = msg || 'Opponent left the game. You win!';
  leftModal.hidden = false; btnLeftHome.focus();
}
function showRematch(msg) { rematchStatus.hidden = false; rematchText.textContent = msg; }
function hideRematch() { rematchStatus.hidden = true; }

function startGrace(secondsLeft) {
  wasInGrace = true;
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
