// RF-05, CU-03: la posición neutra se fija sosteniendo el celular quieto
const HOLD_MS = 2000;
const TOLERANCE_DEGREES = 7;

// No mide el reposo absoluto sino que el celular no se mueva respecto de donde
// empezó: un niño nunca lo tiene perfectamente quieto (RNF-05).
export class NeutralHold {
  #holdMs;
  #tolerance;
  #onProgress;
  #onComplete;
  #reference = null;
  #startedAt = 0;
  #done = false;

  constructor({ holdMs = HOLD_MS, tolerance = TOLERANCE_DEGREES, onProgress, onComplete }) {
    this.#holdMs = holdMs;
    this.#tolerance = tolerance;
    this.#onProgress = onProgress;
    this.#onComplete = onComplete;
  }

  observe({ ts, ori }) {
    if (this.#done) return;

    if (!this.#reference || this.#movedTooMuch(ori)) {
      this.#reference = ori;
      this.#startedAt = ts;
      this.#onProgress(0);
      return;
    }

    const elapsed = ts - this.#startedAt;
    if (elapsed < this.#holdMs) {
      this.#onProgress(elapsed / this.#holdMs);
      return;
    }

    this.#done = true;
    this.#onProgress(1);
    this.#onComplete();
  }

  reset() {
    this.#reference = null;
    this.#startedAt = 0;
    this.#done = false;
    this.#onProgress(0);
  }

  #movedTooMuch(ori) {
    return (
      angleDelta(ori.beta, this.#reference.beta) > this.#tolerance ||
      angleDelta(ori.gamma, this.#reference.gamma) > this.#tolerance
    );
  }
}

// Distancia más corta entre dos ángulos, para que 179° y -179° no parezcan lejos.
function angleDelta(a, b) {
  const diff = Math.abs(a - b) % 360;
  return diff > 180 ? 360 - diff : diff;
}
