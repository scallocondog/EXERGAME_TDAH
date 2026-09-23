// RNF-05, RNF-06: una sola pantalla visible por vez y estados siempre claros
const screens = new Map(
  [...document.querySelectorAll('[data-screen]')].map((el) => [el.dataset.screen, el]),
);

// Perímetro del círculo del anillo (r = 52), para dibujar el avance.
const RING_LENGTH = 2 * Math.PI * 52;
const RECENTER_NOTICE_MS = 1200;

export const els = {
  codeInput: document.getElementById('code-input'),
  codeButton: document.getElementById('code-button'),
  codeNotice: document.getElementById('code-notice'),
  permissionButton: document.getElementById('permission-button'),
  deniedButton: document.getElementById('denied-button'),
  noSensorsButton: document.getElementById('no-sensors-button'),
  ringProgress: document.getElementById('ring-progress'),
  confirmButton: document.getElementById('confirm-button'),
  recenterButton: document.getElementById('recenter-button'),
  pauseButton: document.getElementById('pause-button'),
};

const status = document.getElementById('status');
const statusText = document.getElementById('status-text');
const overlay = document.getElementById('overlay');
const overlayIcon = document.getElementById('overlay-icon');
const overlayTitle = document.getElementById('overlay-title');
const overlayHelp = document.getElementById('overlay-help');

let statusTimer = 0;

export function showScreen(name) {
  for (const [key, el] of screens) el.hidden = key !== name;
}

// kind: 'on' (conectado) | 'off' (sin señal) | 'wait' (conectando)
export function setStatus(kind, text) {
  clearTimeout(statusTimer);
  status.className = `status status--${kind}`;
  statusText.textContent = text;
}

// Un aviso corto que vuelve solo al estado anterior, para no dejar la barra
// diciendo algo que ya pasó (CU-07).
export function flashStatus(text, kind, back) {
  setStatus(kind, text);
  statusTimer = setTimeout(() => setStatus(kind, back), RECENTER_NOTICE_MS);
}

export function setNotice(text) {
  els.codeNotice.textContent = text;
}

export function setRingProgress(value) {
  const clamped = Math.min(Math.max(value, 0), 1);
  els.ringProgress.style.strokeDashoffset = String(RING_LENGTH * (1 - clamped));
}

export function showOverlay({ icon, title, help = '' }) {
  overlayIcon.textContent = icon;
  overlayTitle.textContent = title;
  overlayHelp.textContent = help;
  overlay.hidden = false;
}

export function hideOverlay() {
  overlay.hidden = true;
}
