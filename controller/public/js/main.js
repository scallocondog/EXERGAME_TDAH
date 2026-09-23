// RF-02, RF-03, RF-05, RF-08, RF-18: el mando, de la conexión a la partida
import { NeutralHold } from './calibration.js';
import { Connection } from './connection.js';
import { vibrate } from './haptics.js';
import { isValidRoomCode, keepValidChars, roomFromUrl } from './room-code.js';
import { SENSOR_SILENCE_MS, SensorStream, requestSensorPermission } from './sensors.js';
import * as ui from './ui.js';
import { keepScreenAwake, watchScreenAwake } from './wake-lock.js';

// Errores que el jugador puede resolver; bad_message es cosa del servidor.
const ERROR_MESSAGES = {
  room_not_found: 'No encontramos esa sala. Mira el código en la pantalla grande.',
  room_full: 'Esa sala ya tiene dos mandos.',
};

// idle: sin sala · permission: esperando el toque · calibrating: buscando la
// posición neutra · playing: el mando ya manda gestos.
let phase = 'idle';
let statusLabel = 'Conectado';
let silenceTimer = 0;

const connection = new Connection({ onState: handleState, onMessage: handleMessage });

const sensors = new SensorStream({ onSample: handleSample });

const neutral = new NeutralHold({
  onProgress: ui.setRingProgress,
  onComplete: finishCalibration,
});

boot();

function boot() {
  // Sin HTTPS el navegador no entrega los sensores (RNF-04).
  if (!window.isSecureContext) {
    ui.showScreen('insecure');
    ui.setStatus('off', 'Sin conexión segura');
    return;
  }

  wireButtons();
  watchScreenAwake();
  connection.start();

  const room = roomFromUrl();
  if (room) {
    connection.join(room);
    waitForGame();
  } else {
    ui.showScreen('code');
    ui.setStatus('wait', 'Escribe el código');
  }
}

// --- conexión (RF-02, RF-07) ---

function handleState(state, msg) {
  if (state === 'connecting') {
    ui.setStatus('wait', 'Conectando…');
    return;
  }

  if (state === 'disconnected') {
    ui.setStatus('off', 'Reconectando…');
    // La partida no se pierde: el servidor guarda el slot y el juego se pausa
    // hasta que volvamos (RNF-07, CU-08).
    if (phase === 'calibrating' || phase === 'playing') {
      ui.showOverlay({
        icon: '📡',
        title: 'Se cortó la señal',
        help: 'No sueltes el celular, volvemos solos.',
      });
    }
    return;
  }

  statusLabel = `Mando ${msg.slot} conectado`;
  ui.setStatus('on', statusLabel);
  ui.hideOverlay();
  if (phase === 'idle') askPermission();
}

function handleMessage(msg) {
  switch (msg.t) {
    case 'error':
      return handleError(msg.code);
    case 'haptic':
      // RF-18: el juego avisa del acierto o del error y el mando lo hace sentir.
      vibrate(msg.pattern);
      return;
    case 'state':
      return handleGameState(msg.value);
  }
}

function handleError(code) {
  const message = ERROR_MESSAGES[code];
  if (!message) return;

  sensors.stop();
  clearTimeout(silenceTimer);
  phase = 'idle';
  ui.hideOverlay();
  ui.setNotice(message);
  ui.showScreen('code');
  ui.setStatus('off', 'Sin sala');
}

function handleGameState(value) {
  if (value === 'playing') return ui.hideOverlay();
  if (value === 'paused') {
    // RF-19: la pausa se avisa sin regañar; el juego espera (RNF-05).
    return ui.showOverlay({ icon: '⏸', title: 'Juego en pausa', help: 'Mira la pantalla grande' });
  }
  if (value === 'disconnected') {
    ui.showOverlay({ icon: '📺', title: 'El juego se cerró', help: 'Vuelve a abrirlo en la PC' });
  }
}

function waitForGame() {
  ui.showOverlay({ icon: '📡', title: 'Conectando con el juego' });
}

// --- sensores y calibración (RF-03, RF-05, CU-03) ---

function askPermission() {
  phase = 'permission';
  ui.showScreen('permission');
}

async function grantSensors() {
  const answer = await requestSensorPermission();
  if (answer !== 'granted') {
    ui.showScreen('denied');
    return;
  }
  keepScreenAwake();
  startCalibration();
}

function startCalibration() {
  phase = 'calibrating';
  neutral.reset();
  ui.showScreen('calibrate');
  sensors.start();

  // Permiso concedido pero sin lecturas: no es un celular con sensores.
  clearTimeout(silenceTimer);
  silenceTimer = setTimeout(() => {
    if (!sensors.hasReadings) ui.showScreen('no-sensors');
  }, SENSOR_SILENCE_MS);
}

function handleSample(sample) {
  connection.sendMotion(sample);
  if (phase === 'calibrating') neutral.observe(sample);
}

function finishCalibration() {
  connection.sendCalibrate();
  vibrate('tap');
  phase = 'playing';
  ui.showScreen('pad');
}

// --- botones (RF-08, RF-19, CU-07) ---

function wireButtons() {
  ui.els.codeInput.addEventListener('input', (event) => {
    event.target.value = keepValidChars(event.target.value);
  });
  ui.els.codeInput.addEventListener('keydown', (event) => {
    if (event.key === 'Enter') submitCode();
  });
  ui.els.codeButton.addEventListener('click', submitCode);

  ui.els.permissionButton.addEventListener('click', grantSensors);
  ui.els.deniedButton.addEventListener('click', grantSensors);
  ui.els.noSensorsButton.addEventListener('click', startCalibration);

  // pointerdown y no click: el gesto tiene que salir en cuanto el dedo toca (RNF-01).
  onPress(ui.els.confirmButton, () => send('confirm'));
  onPress(ui.els.pauseButton, () => send('pause'));
  onPress(ui.els.recenterButton, () => {
    send('recenter');
    neutral.reset();
    ui.flashStatus('Recentrado', 'on', statusLabel);
  });

  // Mantener pulsado no debe abrir el menú del navegador a media partida.
  document.addEventListener('contextmenu', (event) => event.preventDefault());
}

function onPress(button, action) {
  button.addEventListener('pointerdown', (event) => {
    event.preventDefault();
    action();
    vibrate('tap');
  });
}

function send(action) {
  connection.sendButton(action);
}

function submitCode() {
  const code = keepValidChars(ui.els.codeInput.value);
  ui.els.codeInput.value = code;
  if (!isValidRoomCode(code)) {
    ui.setNotice('El código tiene 4 letras o números.');
    return;
  }
  ui.setNotice('');
  ui.els.codeInput.blur();
  connection.join(code);
  waitForGame();
}
