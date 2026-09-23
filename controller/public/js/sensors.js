// RF-03, RF-04: permiso de sensores y lectura de orientación y aceleración
// Tope de 60 Hz: el rango del protocolo es 30–60 Hz y el servidor descarta
// todo lo que pase de 80 Hz.
const MIN_INTERVAL_MS = 16;

// Si tras este tiempo no llegó ninguna lectura, no hay sensores que valgan
// (navegador de escritorio, permiso concedido pero bloqueado por el sistema).
export const SENSOR_SILENCE_MS = 2500;

// iOS pide el permiso explícitamente y solo dentro de un gesto del jugador.
export function sensorsNeedPermission() {
  return typeof window.DeviceOrientationEvent?.requestPermission === 'function';
}

export async function requestSensorPermission() {
  if (!sensorsNeedPermission()) return 'granted';
  try {
    const answers = await Promise.all([
      window.DeviceOrientationEvent.requestPermission(),
      typeof window.DeviceMotionEvent?.requestPermission === 'function'
        ? window.DeviceMotionEvent.requestPermission()
        : 'granted',
    ]);
    return answers.every((answer) => answer === 'granted') ? 'granted' : 'denied';
  } catch {
    // Fuera de un gesto del usuario, o sin HTTPS: iOS lanza en vez de responder.
    return 'denied';
  }
}

// Une los dos eventos del navegador en una sola lectura del protocolo.
// Los valores van tal cual los da el navegador: el reconocedor de Unity usa la
// magnitud de la aceleración, así que el signo invertido de iOS no lo afecta.
export class SensorStream {
  #orientation = null;
  #acceleration = null;
  #lastSentAt = 0;
  #onSample;
  #onOrientation;
  #onMotion;
  #running = false;

  constructor({ onSample }) {
    this.#onSample = onSample;
    this.#onOrientation = (event) => this.#readOrientation(event);
    this.#onMotion = (event) => this.#readMotion(event);
  }

  get hasReadings() {
    return this.#orientation !== null;
  }

  start() {
    if (this.#running) return;
    this.#running = true;
    window.addEventListener('deviceorientation', this.#onOrientation);
    window.addEventListener('devicemotion', this.#onMotion);
  }

  stop() {
    this.#running = false;
    window.removeEventListener('deviceorientation', this.#onOrientation);
    window.removeEventListener('devicemotion', this.#onMotion);
  }

  #readOrientation(event) {
    if (event.alpha === null && event.beta === null && event.gamma === null) return;
    this.#orientation = {
      alpha: event.alpha ?? 0,
      beta: event.beta ?? 0,
      gamma: event.gamma ?? 0,
    };
    this.#emit();
  }

  #readMotion(event) {
    const acc = event.accelerationIncludingGravity;
    if (!acc) return;
    this.#acceleration = { x: acc.x ?? 0, y: acc.y ?? 0, z: acc.z ?? 0 };
    this.#emit();
  }

  // Manda la última orientación conocida junto con la última aceleración: los
  // dos eventos llegan por separado y a ritmos distintos.
  #emit() {
    if (!this.#orientation) return;
    const now = Date.now();
    if (now - this.#lastSentAt < MIN_INTERVAL_MS) return;
    this.#lastSentAt = now;
    this.#onSample({
      ts: now,
      ori: this.#orientation,
      acc: this.#acceleration ?? { x: 0, y: 0, z: 0 },
    });
  }
}
