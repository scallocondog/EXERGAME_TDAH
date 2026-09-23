// RF-01, RF-20: pruebas del registro de salas
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { ROOM_CODE_LENGTH } from './room-code.js';
import { RoomRegistry } from './room-registry.js';

test('crea salas con código de 4 caracteres legibles y sin repetir', () => {
  const registry = new RoomRegistry();
  const codes = new Set();
  for (let i = 0; i < 200; i++) codes.add(registry.createRoom(`game-${i}`).code);

  assert.equal(codes.size, 200);
  for (const code of codes) {
    assert.equal(code.length, ROOM_CODE_LENGTH);
    assert.doesNotMatch(code, /[01OIL]/);
  }
});

test('el código se acepta en minúsculas y con espacios', () => {
  const registry = new RoomRegistry();
  const { code } = registry.createRoom('game');
  assert.equal(registry.getRoom(` ${code.toLowerCase()} `)?.code, code);
});

test('asigna slot 1 y 2 y rechaza el tercer mando', () => {
  const registry = new RoomRegistry();
  const { code } = registry.createRoom('game');

  assert.equal(registry.joinPad(code, 'a').slot, 1);
  assert.equal(registry.joinPad(code, 'b').slot, 2);
  assert.deepEqual(registry.joinPad(code, 'c'), { ok: false, error: 'room_full' });
});

test('rechaza una sala que no existe', () => {
  const registry = new RoomRegistry();
  assert.deepEqual(registry.joinPad('ZZZZ', 'a'), { ok: false, error: 'room_not_found' });
});

test('un mando que vuelve recupera el slot desconectado', () => {
  const registry = new RoomRegistry();
  const { code } = registry.createRoom('game');
  registry.joinPad(code, 'a');
  registry.joinPad(code, 'b');
  registry.findPad('a').pad.connected = false;

  const result = registry.joinPad(code, 'a-nuevo-socket');
  assert.equal(result.slot, 1);
  assert.equal(result.rejoined, true);
  assert.equal(registry.findPad('a'), null);
});

test('cerrar la sala la elimina', () => {
  const registry = new RoomRegistry();
  const { code } = registry.createRoom('game');
  registry.closeRoom(code);
  assert.equal(registry.getRoom(code), null);
  assert.equal(registry.findRoomByGame('game'), null);
});
