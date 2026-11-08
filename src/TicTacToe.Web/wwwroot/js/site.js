import { applyTheme, toggleTheme } from './theme.js';
import { applySound, sounds, toggleSound } from './sound.js';

applyTheme();
applySound(localStorage.getItem('ttt:sound') || 'on');

document.getElementById('nav-theme')?.addEventListener('click', () => { toggleTheme(); sounds.toggle(); });
document.getElementById('nav-sound')?.addEventListener('click', () => { toggleSound(); sounds.toggle(); });
