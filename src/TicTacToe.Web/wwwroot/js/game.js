// Tic Tac Toe board — render-only. Server is referee; state is replaced wholesale (never merged).
import { sounds } from './sound.js';

const boardEl = document.getElementById('board');
const statusEl = document.getElementById('status');
const errorEl = document.getElementById('error');
const scoreXEl = document.getElementById('score-x');
const scoreDrawsEl = document.getElementById('score-draws');
const scoreOEl = document.getElementById('score-o');
const modeEl = document.getElementById('mode');
const difficultyEl = document.getElementById('difficulty');
const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');
const authenticated = document.querySelector('meta[name="ttt:authenticated"]')?.content === 'true';
const reducedMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

let winOverlay = document.getElementById('win-line');
if (!winOverlay || winOverlay.tagName.toLowerCase() !== 'svg') {
  winOverlay?.remove();
  const overlay = document.createElementNS('http://www.w3.org/2000/svg', 'svg');
  overlay.setAttribute('id', 'win-line');
  overlay.setAttribute('viewBox', '0 0 100 100');
  overlay.setAttribute('preserveAspectRatio', 'none');
  overlay.classList.add('ttt-win-line');
  overlay.hidden = true;
  overlay.setAttribute('aria-hidden', 'true');
  boardEl.appendChild(overlay);
}

let cells = Array(9).fill(null);
let turn = 0;
let moves = [];
let finished = false;
let busy = false;
let thinking = false;
let score = JSON.parse(localStorage.getItem('ttt:session-score') || '{"x":0,"draws":0,"o":0}');

function saveScore() {
  localStorage.setItem('ttt:session-score', JSON.stringify(score));
}

function renderScore() {
  scoreXEl.textContent = score.x;
  scoreDrawsEl.textContent = score.draws;
  scoreOEl.textContent = score.o;
}

function statusText(result) {
  if (thinking) return 'Computer is thinking…';
  if (!result) return turn === 0 ? "X's turn" : "O's turn";
  if (result.outcome === 1) return 'X wins!';
  if (result.outcome === 2) return 'O wins!';
  if (result.outcome === 3) return "It's a draw";
  return turn === 0 ? "X's turn" : "O's turn";
}

function drawWinLine(line) {
  const overlay = document.getElementById('win-line');
  if (!overlay) return;
  if (!line || line.length !== 3 || reducedMotion) { overlay.hidden = true; overlay.innerHTML = ''; return; }
  const [a, , c] = line;
  const ax = (a % 3) * 33.333 + 16.667, ay = Math.floor(a / 3) * 33.333 + 16.667;
  const cx = (c % 3) * 33.333 + 16.667, cy = Math.floor(c / 3) * 33.333 + 16.667;
  overlay.innerHTML = `<line x1="${ax}%" y1="${ay}%" x2="${cx}%" y2="${cy}%"></line>`;
  overlay.hidden = false;
}

function render(result) {
  const btns = boardEl.querySelectorAll('.ttt-cell');
  btns.forEach((btn, idx) => {
    const v = cells[idx];
    btn.textContent = v === null ? '' : v === 0 ? 'X' : 'O';
    btn.classList.toggle('ttt-mark-x', v === 0);
    btn.classList.toggle('ttt-mark-o', v === 1);
    btn.classList.remove('ttt-winning');
    btn.disabled = finished || busy || v !== null;
    btn.setAttribute('aria-label', `Row ${Math.floor(idx / 3) + 1}, column ${(idx % 3) + 1}, ${v === null ? 'empty' : v === 0 ? 'X' : 'O'}`);
  });
  if (result && result.winningLine) {
    result.winningLine.forEach(i => btns[i].classList.add('ttt-winning'));
    drawWinLine(result.winningLine);
  } else {
    drawWinLine(null);
  }
  statusEl.textContent = statusText(result);
  renderScore();
}

function resetBoard(newMatch) {
  cells = Array(9).fill(null);
  turn = 0;
  moves = [];
  finished = false;
  thinking = false;
  errorEl.hidden = true;
  if (newMatch) {
    score = { x: 0, draws: 0, o: 0 };
    saveScore();
  }
  render(null);
}

document.getElementById('btn-reset').addEventListener('click', () => resetBoard(false));
document.getElementById('btn-new-match').addEventListener('click', () => resetBoard(true));

// M7.5 polish: segmented Mode chips + difficulty chips sync hidden selects (game rules read from selects).
const modeLocalBtn = document.getElementById('mode-local');
const modeComputerBtn = document.getElementById('mode-computer');
const difficultyGroup = document.getElementById('difficulty-group');
function syncModeChips() {
  const vs = modeEl.value === '1';
  modeLocalBtn.classList.toggle('is-active', !vs);
  modeComputerBtn.classList.toggle('is-active', vs);
  modeLocalBtn.setAttribute('aria-pressed', String(!vs));
  modeComputerBtn.setAttribute('aria-pressed', String(vs));
  difficultyGroup.hidden = !vs;
}
function syncDifficultyChips() {
  difficultyGroup.querySelectorAll('button').forEach(b => {
    const on = b.dataset.difficulty === difficultyEl.value;
    b.classList.toggle('is-active', on);
    b.setAttribute('aria-pressed', String(on));
  });
}
modeLocalBtn.addEventListener('click', () => { modeEl.value = '0'; modeEl.dispatchEvent(new Event('change')); syncModeChips(); });
modeComputerBtn.addEventListener('click', () => { modeEl.value = '1'; modeEl.dispatchEvent(new Event('change')); syncModeChips(); });
difficultyGroup.querySelectorAll('button').forEach(b => {
  b.addEventListener('click', () => { difficultyEl.value = b.dataset.difficulty; difficultyEl.dispatchEvent(new Event('change')); syncDifficultyChips(); });
});

modeEl.addEventListener('change', () => {
  difficultyEl.disabled = modeEl.value !== '1';
  resetBoard(false);
});
difficultyEl.addEventListener('change', () => resetBoard(false));

function moveFocus(e, idx) {
  const r = Math.floor(idx / 3), c = idx % 3;
  if (e.key === 'ArrowLeft') { e.preventDefault(); focusCell(r * 3 + Math.max(0, c - 1)); }
  if (e.key === 'ArrowRight') { e.preventDefault(); focusCell(r * 3 + Math.min(2, c + 1)); }
  if (e.key === 'ArrowUp') { e.preventDefault(); focusCell(Math.max(0, r - 1) * 3 + c); }
  if (e.key === 'ArrowDown') { e.preventDefault(); focusCell(Math.min(2, r + 1) * 3 + c); }
}

function focusCell(idx) {
  boardEl.querySelectorAll('.ttt-cell')[idx]?.focus();
}

boardEl.addEventListener('keydown', (e) => {
  const btn = e.target.closest('.ttt-cell');
  if (btn) moveFocus(e, Number(btn.dataset.index));
});

boardEl.addEventListener('click', async (e) => {
  const btn = e.target.closest('.ttt-cell');
  if (!btn || busy || finished) return;
  const index = Number(btn.dataset.index);
  if (cells[index] !== null) return;

  busy = true;
  thinking = modeEl.value === '1';
  errorEl.hidden = true;
  render(null);

  const res = await fetch('/api/game/move', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
      'RequestVerificationToken': tokenEl ? tokenEl.value : '',
    },
    body: JSON.stringify({
      cells,
      moveIndex: index,
      mark: turn,
      mode: Number(modeEl.value),
      difficulty: modeEl.value === '1' ? Number(difficultyEl.value) : null,
    }),
  });

  busy = false;
  thinking = false;
  const data = await res.json();

  if (!res.ok) {
    errorEl.hidden = false;
    errorEl.textContent = data.error ? data.error.message : 'Something went wrong. Please try again.';
    render(null);
    return;
  }

  cells = data.board;
  moves.push(index);
  if (data.computerMoveIndex !== null && data.computerMoveIndex !== undefined) moves.push(data.computerMoveIndex);
  turn = data.turn === null || data.turn === undefined ? -1 : data.turn;
  finished = data.result && data.result.outcome !== 0;

  sounds.place();

  if (finished) {
    if (data.result.outcome === 1) score.x++;
    else if (data.result.outcome === 2) score.o++;
    else if (data.result.outcome === 3) score.draws++;
    saveScore();
    if (data.result.outcome === 1) sounds.win();
    else if (data.result.outcome === 2) sounds.lose();
    else sounds.draw();

    if (authenticated && !res.redirected) {
      const claimed = data.result.outcome === 3 ? 2 : (data.result.outcome === 1 ? 0 : 1);
      const fd = new FormData();
      fd.append('moves', moves.join(','));
      fd.append('symbol', '0');
      fd.append('mode', String(modeEl.value));
      fd.append('difficulty', modeEl.value === '1' ? difficultyEl.value : '');
      fd.append('claimedResult', String(claimed));
      fd.append('__RequestVerificationToken', tokenEl ? tokenEl.value : '');
      await fetch('/Matches/Save', { method: 'POST', body: fd });
    }
  }

  render(data.result);
});

difficultyEl.disabled = true;
difficultyGroup.hidden = modeEl.value !== '1';
syncModeChips();
syncDifficultyChips();
render(null);
