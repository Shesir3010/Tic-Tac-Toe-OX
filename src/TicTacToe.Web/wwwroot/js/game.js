// Tic Tac Toe board — render-only. Server is referee; state is replaced wholesale (never merged).
const boardEl = document.getElementById('board');
const statusEl = document.getElementById('status');
const errorEl = document.getElementById('error');
const scoreXEl = document.getElementById('score-x');
const scoreDrawsEl = document.getElementById('score-draws');
const scoreOEl = document.getElementById('score-o');
const modeEl = document.getElementById('mode');
const difficultyEl = document.getElementById('difficulty');
const tokenEl = document.querySelector('input[name="__RequestVerificationToken"]');

let cells = Array(9).fill(null);
let turn = 0;
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
  }
  statusEl.textContent = statusText(result);
  renderScore();
}

function resetBoard(newMatch) {
  cells = Array(9).fill(null);
  turn = 0;
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

modeEl.addEventListener('change', () => {
  difficultyEl.disabled = modeEl.value !== '1';
  resetBoard(false);
});
difficultyEl.addEventListener('change', () => resetBoard(false));

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
      mark: 0,
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
  turn = data.turn === null || data.turn === undefined ? -1 : data.turn;
  finished = data.result && data.result.outcome !== 0;

  if (finished) {
    if (data.result.outcome === 1) score.x++;
    else if (data.result.outcome === 2) score.o++;
    else if (data.result.outcome === 3) score.draws++;
    saveScore();
  }

  render(data.result);
});

difficultyEl.disabled = modeEl.value !== '1';
render(null);
