import { applyTheme, toggleTheme } from './theme.js';
import { applySound, sounds, toggleSound } from './sound.js';
import { startHub } from './hub.js';

applyTheme();
applySound(localStorage.getItem('ttt:sound') || 'on');

document.getElementById('nav-theme')?.addEventListener('click', () => { toggleTheme(); sounds.toggle(); });
document.getElementById('nav-sound')?.addEventListener('click', () => { toggleSound(); sounds.toggle(); });

// FR-32: logged-in players hold one hub connection on every page.
// Guests never start the hub (it is [Authorize]) — lobby shows guest notice only.
if (document.querySelector('meta[name="ttt:authenticated"]')?.content === 'true') {
  startHub();
}
