// RF-04: limitador de frecuencia por mando (protocolo-ws.md §4)
// El mando envía a 30–60 Hz; el servidor descarta lo que supere 80 Hz.

const DEFAULT_MAX_HZ = 80;

export class RateLimiter {
  /** @type {Map<string, number>} último timestamp aceptado por mando */
  #lastAccepted = new Map();
  #minIntervalMs;

  /** @param {{ maxHz?: number }} opts */
  constructor({ maxHz = DEFAULT_MAX_HZ } = {}) {
    this.#minIntervalMs = 1000 / maxHz;
  }

  /**
   * Decide si un mensaje de tipo `motion` de un mando dado puede pasar.
   * Los tipos que no son `motion` siempre pasan (son eventos discretos).
   *
   * @param {string} padId — identificador del mando (socket id).
   * @param {Record<string, unknown>} msg — mensaje ya parseado.
   * @returns {{ ok: true } | { ok: false, reason: string }}
   */
  check(padId, msg) {
    if (msg.t !== 'motion') return { ok: true };

    const now = Date.now();
    const last = this.#lastAccepted.get(padId) ?? 0;

    if (now - last < this.#minIntervalMs) {
      return { ok: false, reason: `motion throttled (>${DEFAULT_MAX_HZ} Hz)` };
    }

    this.#lastAccepted.set(padId, now);
    return { ok: true };
  }

  /** Limpia el registro de un mando que se desconectó. */
  forget(padId) {
    this.#lastAccepted.delete(padId);
  }
}
