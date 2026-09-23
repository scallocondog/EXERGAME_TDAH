// RF-04: forma mínima que el relay exige antes de retransmitir a Unity
// Los rangos y la frecuencia (protocolo-ws.md §4) son de server/src/validation/.
const BUTTON_ACTIONS = new Set(['confirm', 'recenter', 'pause']);

export function checkShape(msg) {
  switch (msg.t) {
    case 'motion':
      return checkMotion(msg);
    case 'button':
      return BUTTON_ACTIONS.has(msg.action) ? OK : fail(`button con action "${msg.action}"`);
    default:
      return OK;
  }
}

const OK = { ok: true };

function checkMotion(msg) {
  if (!Number.isInteger(msg.seq) || msg.seq < 0) return fail('motion con seq inválido');
  if (!Number.isInteger(msg.ts)) return fail('motion con ts inválido');
  if (!allFinite(msg.ori, ['alpha', 'beta', 'gamma'])) return fail('motion con ori no numérico');
  if (!allFinite(msg.acc, ['x', 'y', 'z'])) return fail('motion con acc no numérico');
  return OK;
}

// JSON no transporta NaN ni Infinity: llegan como null, que aquí también se rechaza.
function allFinite(obj, keys) {
  return typeof obj === 'object' && obj !== null && keys.every((key) => Number.isFinite(obj[key]));
}

function fail(reason) {
  return { ok: false, reason };
}
