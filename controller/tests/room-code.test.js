// RF-02: el código que el jugador escribe cuando no puede escanear el QR
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { isValidRoomCode, keepValidChars, normalizeRoomCode } from '../public/js/room-code.js';

test('el código se limpia mientras el jugador escribe', () => {
  assert.equal(keepValidChars('tcxg'), 'TCXG');
  assert.equal(keepValidChars('tc-xg'), 'TCXG');
  assert.equal(keepValidChars('TCXGQQ'), 'TCXG');
  // 0, O, 1, I y L no existen en el alfabeto del servidor: se descartan
  assert.equal(keepValidChars('T0X1'), 'TX');
});

test('normaliza igual que el servidor', () => {
  assert.equal(normalizeRoomCode('  tcxg '), 'TCXG');
  assert.equal(normalizeRoomCode(null), '');
});

test('solo acepta códigos de cuatro caracteres válidos', () => {
  assert.equal(isValidRoomCode('TCXG'), true);
  assert.equal(isValidRoomCode('tcxg'), true);
  assert.equal(isValidRoomCode('TCX'), false);
  assert.equal(isValidRoomCode('TCXGQ'), false);
  assert.equal(isValidRoomCode('TCX0'), false);
  assert.equal(isValidRoomCode(null), false);
});
