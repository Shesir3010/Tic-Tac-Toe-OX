// M7.5 FR-21: synthesized Web Audio, no files, null-guarded. Only after first interaction.
let ctx = null;

export function getSound() { return localStorage.getItem('ttt:sound') || 'on'; }

export function applySound(value) {
  localStorage.setItem('ttt:sound', value);
  const btn = document.getElementById('nav-sound');
  if (btn) btn.textContent = value === 'on' ? '🔊' : '🔇';
}

export function toggleSound() {
  applySound(getSound() === 'on' ? 'off' : 'on');
}

function tone(freq, durationMs, delayMs = 0) {
  if (getSound() !== 'on') return;
  try {
    ctx ??= new (window.AudioContext || window.webkitAudioContext)();
    if (ctx.state === 'suspended') ctx.resume();
    const t = ctx.currentTime + delayMs / 1000;
    const osc = ctx.createOscillator();
    const gain = ctx.createGain();
    osc.type = 'sine';
    osc.frequency.value = freq;
    gain.gain.setValueAtTime(0.12, t);
    gain.gain.exponentialRampToValueAtTime(0.001, t + durationMs / 1000);
    osc.connect(gain).connect(ctx.destination);
    osc.start(t);
    osc.stop(t + durationMs / 1000);
  } catch { /* silence degrades gracefully */ }
}

export const sounds = {
  place: () => tone(440, 90),
  win: () => { tone(523, 140); tone(659, 140, 120); tone(784, 200, 240); },
  lose: () => tone(220, 220),
  draw: () => tone(330, 180),
  toggle: () => tone(600, 60),
};
