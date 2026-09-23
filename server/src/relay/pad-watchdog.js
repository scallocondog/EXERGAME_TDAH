// RF-07: un mando sin mensajes durante un tiempo se considera desconectado
export class PadWatchdog {
  #lastSeen = new Map();
  #timeoutMs;
  #onTimeout;

  constructor({ timeoutMs, onTimeout }) {
    this.#timeoutMs = timeoutMs;
    this.#onTimeout = onTimeout;
  }

  touch(padId, now) {
    this.#lastSeen.set(padId, now);
  }

  forget(padId) {
    this.#lastSeen.delete(padId);
  }

  // Avisa una sola vez por silencio: el mando vuelve a vigilarse con el próximo touch.
  sweep(now) {
    for (const [padId, lastSeen] of this.#lastSeen) {
      if (now - lastSeen >= this.#timeoutMs) {
        this.#lastSeen.delete(padId);
        this.#onTimeout(padId);
      }
    }
  }
}
