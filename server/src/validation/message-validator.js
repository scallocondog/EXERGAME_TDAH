// RF-04, RNF-04: validación de rangos de los mensajes del mando
// Rangos según docs/protocolo-ws.md §4.
// Este módulo corre DESPUÉS de checkShape (relay/message-shape.js), que ya
// garantiza que los campos existen y son numéricos finitos.

/** @typedef {{ ok: true } | { ok: false, reason: string }} Verdict */

// ── Rangos válidos (protocolo-ws.md §1, tabla de motion) ──────────────
const ORI_RANGES = {
  alpha: { min: 0, max: 360 },
  beta:  { min: -180, max: 180 },
  gamma: { min: -90, max: 90 },
};

const ACC_RANGE = { min: -60, max: 60 }; // m/s², los tres ejes iguales

const OK = /** @type {const} */ ({ ok: true });

/**
 * Valida que los valores numéricos de un mensaje `motion` estén dentro de los
 * rangos definidos en el protocolo. Para tipos distintos de `motion` acepta
 * directamente (la forma ya fue verificada por checkShape).
 *
 * @param {Record<string, unknown>} msg — mensaje ya parseado y con forma válida.
 * @returns {Verdict}
 */
export function validateRanges(msg) {
  if (msg.t !== 'motion') return OK;

  for (const [key, { min, max }] of Object.entries(ORI_RANGES)) {
    const v = /** @type {number} */ (msg.ori[key]);
    if (v < min || v > max) {
      return { ok: false, reason: `ori.${key} fuera de rango: ${v} (${min}..${max})` };
    }
  }

  for (const axis of ['x', 'y', 'z']) {
    const v = /** @type {number} */ (msg.acc[axis]);
    if (v < ACC_RANGE.min || v > ACC_RANGE.max) {
      return { ok: false, reason: `acc.${axis} fuera de rango: ${v} (${ACC_RANGE.min}..${ACC_RANGE.max})` };
    }
  }

  return OK;
}
