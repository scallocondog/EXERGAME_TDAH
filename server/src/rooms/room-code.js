// RF-01: códigos de sala cortos y fáciles de leer en la TV
import { randomInt } from 'node:crypto';

// Sin 0/O ni 1/I/L: se confunden al leerlos de lejos.
const ALPHABET = 'ABCDEFGHJKMNPQRSTUVWXYZ23456789';
const MAX_ATTEMPTS = 1000;

export const ROOM_CODE_LENGTH = 4;

export function generateRoomCode(isTaken) {
  for (let attempt = 0; attempt < MAX_ATTEMPTS; attempt++) {
    let code = '';
    for (let i = 0; i < ROOM_CODE_LENGTH; i++) {
      code += ALPHABET[randomInt(ALPHABET.length)];
    }
    if (!isTaken(code)) return code;
  }
  throw new Error('No quedan códigos de sala libres');
}

export function normalizeRoomCode(value) {
  return typeof value === 'string' ? value.trim().toUpperCase() : '';
}
