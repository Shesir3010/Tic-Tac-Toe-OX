// M7.5 FR-13: theme toggle persisted in localStorage, shared by navbar + Settings.
export function getTheme() { return localStorage.getItem('ttt:theme') || 'device'; }

export function applyTheme(preference) {
  const pref = preference || getTheme();
  localStorage.setItem('ttt:theme', pref);
  const dark = pref === 'dark' || (pref === 'device' && window.matchMedia('(prefers-color-scheme: dark)').matches);
  document.documentElement.dataset.bsTheme = dark ? 'dark' : 'light';
  const btn = document.getElementById('nav-theme');
  if (btn) btn.textContent = dark ? '☀️' : '🌙';
}

export function toggleTheme() {
  const next = document.documentElement.dataset.bsTheme === 'dark' ? 'light' : 'dark';
  applyTheme(next);
}
