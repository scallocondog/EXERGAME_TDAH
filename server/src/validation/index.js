// RF-04, RNF-04: punto de entrada de la validación del mando
// Combina la validación de rangos y el limitador de frecuencia.
// El relay invoca validate(msg) después de checkShape.
import { validateRanges } from './message-validator.js';
import { RateLimiter } from './rate-limiter.js';

export { RateLimiter } from './rate-limiter.js';
export { validateRanges } from './message-validator.js';

/**
 * Crea la función validate(msg) que el Relay consume.
 * Se necesita una instancia de RateLimiter compartida porque depende del
 * estado (último timestamp por mando).
 *
 * @param {{ rateLimiter: RateLimiter }} deps
 * @returns {(msg: Record<string, unknown>, padId?: string) => { ok: true } | { ok: false, reason: string }}
 */
export function createValidator({ rateLimiter }) {
  return (msg, padId) => {
    // 1. Rangos (ori y acc dentro de protocolo-ws.md §4)
    const rangeResult = validateRanges(msg);
    if (!rangeResult.ok) return rangeResult;

    // 2. Frecuencia (≤ 80 Hz por mando)
    if (padId) {
      const rateResult = rateLimiter.check(padId, msg);
      if (!rateResult.ok) return rateResult;
    }

    return { ok: true };
  };
}
