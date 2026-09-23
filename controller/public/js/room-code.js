// RF-02: el código de sala, con el mismo alfabeto que server/src/rooms/room-code.js
export const ROOM_CODE_LENGTH = 4;

// Sin 0/O ni 1/I/L: se confunden al leerlos de lejos en la TV.
const ALPHABET = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';

export function normalizeRoomCode(value) {
  return typeof value === 'string' ? value.trim().toUpperCase() : '';
}

export function isValidRoomCode(value) {
  const code = normalizeRoomCode(value);
  return code.length === ROOM_CODE_LENGTH && [...code].every((char) => ALPHABET.includes(char));
}

// Descarta lo que el jugador teclee y no exista en el alfabeto, para que no
// llegue a escribir un código imposible.
export function keepValidChars(value) {
  return [...normalizeRoomCode(value)]
    .filter((char) => ALPHABET.includes(char))
    .slice(0, ROOM_CODE_LENGTH)
    .join('');
}

// El QR abre la página con ?room=CODIGO (RF-01).
export function roomFromUrl(search = window.location.search) {
  const code = normalizeRoomCode(new URLSearchParams(search).get('room'));
  return isValidRoomCode(code) ? code : '';
}
