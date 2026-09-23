// RF-18: vibración en aciertos y errores, si el celular la soporta
// (Safari en iOS no expone navigator.vibrate; el juego sigue igual sin ella).
const PATTERNS = {
  // El acierto se siente claro y corto; el error es más suave y distinto, no un
  // castigo: el refuerzo es positivo (RNF-05).
  hit: [45],
  miss: [20, 90, 20],
  tap: [15],
};

export function supportsVibration() {
  return typeof navigator.vibrate === 'function';
}

export function vibrate(pattern) {
  if (!supportsVibration()) return false;
  const shape = PATTERNS[pattern];
  if (!shape) return false;
  try {
    return navigator.vibrate(shape);
  } catch {
    return false;
  }
}
