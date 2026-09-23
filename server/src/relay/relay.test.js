// RF-04, RF-07, RF-20: pruebas del relay con reloj simulado
import assert from 'node:assert/strict';
import { beforeEach, test } from 'node:test';
import { RoomRegistry } from '../rooms/room-registry.js';
import { PAD_TIMEOUT_MS, Relay } from './relay.js';

const MOTION = {
  t: 'motion',
  seq: 1,
  ts: 1737590000123,
  ori: { alpha: 12.4, beta: -3.1, gamma: 45 },
  acc: { x: 0.12, y: 9.71, z: 0.03 },
};

let clock;
let relay;

function fakeConn(id) {
  const conn = { id, inbox: [], send: (msg) => conn.inbox.push(msg) };
  relay.connect(conn);
  return conn;
}

function last(conn) {
  return conn.inbox.at(-1);
}

function openRoom() {
  const game = fakeConn('game');
  relay.receive('game', { t: 'create_room' });
  return { game, room: last(game).room };
}

function joinPad(id, room) {
  const pad = fakeConn(id);
  relay.receive(id, { t: 'join', room, v: 1 });
  return pad;
}

beforeEach(() => {
  clock = 0;
  relay = new Relay({
    registry: new RoomRegistry(),
    joinUrl: (room) => `https://192.168.1.10:3443/?room=${room}`,
    now: () => clock,
    log: () => {},
  });
});

test('CU-01: el juego crea una sala y recibe código y URL', () => {
  const game = fakeConn('game');
  relay.receive('game', { t: 'create_room' });

  const reply = last(game);
  assert.equal(reply.t, 'room_created');
  assert.match(reply.room, /^[A-Z2-9]{4}$/);
  assert.equal(reply.url, `https://192.168.1.10:3443/?room=${reply.room}`);
});

test('CU-02: el mando se une y el juego recibe pad_state connected', () => {
  const { game, room } = openRoom();
  const pad = joinPad('pad', room);

  assert.deepEqual(last(pad), { t: 'joined', room, slot: 1 });
  assert.deepEqual(last(game), { t: 'pad_state', slot: 1, value: 'connected' });
});

test('CU-02: sala inexistente y sala llena devuelven error', () => {
  const { room } = openRoom();
  assert.deepEqual(last(joinPad('x', 'ZZZZ')), { t: 'error', code: 'room_not_found' });

  joinPad('a', room);
  joinPad('b', room);
  assert.deepEqual(last(joinPad('c', room)), { t: 'error', code: 'room_full' });
});

test('RF-04: motion, button y calibrate llegan al juego con el slot', () => {
  const { game, room } = openRoom();
  joinPad('pad', room);

  relay.receive('pad', { ...MOTION, room });
  assert.deepEqual(last(game), { ...MOTION, room, slot: 1 });

  relay.receive('pad', { t: 'button', room, action: 'confirm' });
  assert.deepEqual(last(game), { t: 'button', room, action: 'confirm', slot: 1 });

  relay.receive('pad', { t: 'calibrate', room });
  assert.deepEqual(last(game), { t: 'calibrate', room, slot: 1 });
});

test('RF-04: nada llega al juego si el mando no se unió, cambia de sala o manda basura', () => {
  const { game, room } = openRoom();
  const intruder = fakeConn('intruder');
  relay.receive('intruder', { ...MOTION, room });
  assert.equal(last(intruder).t, 'error');

  joinPad('pad', room);
  const before = game.inbox.length;
  relay.receive('pad', { ...MOTION, room: 'ZZZZ' });
  relay.receive('pad', { t: 'teleport', room });
  relay.receive('pad', null);
  assert.equal(game.inbox.length, before);
});

test('RF-04: un mensaje rechazado por la validación no llega al juego', () => {
  relay = new Relay({
    registry: new RoomRegistry(),
    joinUrl: () => '',
    now: () => clock,
    log: () => {},
    validate: (msg) => (msg.ori?.gamma > 90 ? { ok: false, reason: 'gamma fuera de rango' } : { ok: true }),
  });
  const { game, room } = openRoom();
  const pad = joinPad('pad', room);
  const before = game.inbox.length;

  relay.receive('pad', { ...MOTION, room, ori: { ...MOTION.ori, gamma: 400 } });
  assert.equal(game.inbox.length, before);
  assert.deepEqual(last(pad), { t: 'error', code: 'bad_message' });
});

test('RF-04: motion con NaN, null, texto o campos faltantes no llega al juego', () => {
  const { game, room } = openRoom();
  joinPad('pad', room);
  const before = game.inbox.length;

  const broken = [
    { ori: { ...MOTION.ori, gamma: Number.NaN } },
    { ori: { ...MOTION.ori, beta: null } },
    { acc: { ...MOTION.acc, x: '0.1' } },
    { acc: undefined },
    { seq: -1 },
    { seq: 1.5 },
    { ts: '1737590000123' },
  ];
  for (const patch of broken) relay.receive('pad', { ...MOTION, room, ...patch });

  assert.equal(game.inbox.length, before);
});

test('RF-04: button con una acción desconocida no llega al juego', () => {
  const { game, room } = openRoom();
  joinPad('pad', room);
  const before = game.inbox.length;

  relay.receive('pad', { t: 'button', room, action: 'jump' });
  relay.receive('pad', { t: 'button', room });
  assert.equal(game.inbox.length, before);
});

test('bad_message se envía como máximo una vez por segundo', () => {
  const { room } = openRoom();
  const pad = joinPad('pad', room);
  const before = pad.inbox.length;

  for (let i = 0; i < 10; i++) relay.receive('pad', { t: 'basura', room });
  assert.equal(pad.inbox.length, before + 1);

  clock += 1000;
  relay.receive('pad', { t: 'basura', room });
  assert.equal(pad.inbox.length, before + 2);
});

test('RF-18: la háptica y el estado del juego llegan al mando del slot indicado', () => {
  const { room } = openRoom();
  const pad1 = joinPad('p1', room);
  const pad2 = joinPad('p2', room);

  relay.receive('game', { t: 'haptic', slot: 2, pattern: 'hit' });
  assert.deepEqual(last(pad2), { t: 'haptic', pattern: 'hit' });
  assert.equal(last(pad1).t, 'joined');

  relay.receive('game', { t: 'state', slot: 1, value: 'paused' });
  assert.deepEqual(last(pad1), { t: 'state', value: 'paused' });
});

test('RF-08: tap llega al mando y un patrón desconocido no', () => {
  const { room } = openRoom();
  const pad = joinPad('pad', room);

  relay.receive('game', { t: 'haptic', slot: 1, pattern: 'tap' });
  assert.deepEqual(last(pad), { t: 'haptic', pattern: 'tap' });

  relay.receive('game', { t: 'haptic', slot: 1, pattern: 'buzz' });
  assert.deepEqual(last(pad), { t: 'haptic', pattern: 'tap' });
});

test('CU-08: 2 s sin mensajes marcan el mando como desconectado y un mensaje nuevo lo reconecta', () => {
  const { game, room } = openRoom();
  joinPad('pad', room);

  clock += PAD_TIMEOUT_MS - 1;
  relay.sweep();
  assert.equal(last(game).t, 'pad_state');
  assert.equal(last(game).value, 'connected');

  clock += 1;
  relay.sweep();
  assert.deepEqual(last(game), { t: 'pad_state', slot: 1, value: 'disconnected' });

  relay.sweep();
  const afterTimeout = game.inbox.length;

  relay.receive('pad', { ...MOTION, room });
  assert.deepEqual(game.inbox[afterTimeout], { t: 'pad_state', slot: 1, value: 'connected' });
  assert.equal(last(game).t, 'motion');
});

test('CU-08: el mando que se cae y vuelve con otro socket recupera su slot', () => {
  const { game, room } = openRoom();
  joinPad('p1', room);
  joinPad('p2', room);

  relay.disconnect('p1');
  assert.deepEqual(last(game), { t: 'pad_state', slot: 1, value: 'disconnected' });

  const back = joinPad('p1-bis', room);
  assert.deepEqual(last(back), { t: 'joined', room, slot: 1 });
  assert.deepEqual(last(game), { t: 'pad_state', slot: 1, value: 'connected' });
});

test('si el juego se cierra, los mandos reciben room_not_found', () => {
  const { room } = openRoom();
  const pad = joinPad('pad', room);

  relay.disconnect('game');
  assert.deepEqual(last(pad), { t: 'error', code: 'room_not_found' });
});

test('crear otra sala desde el mismo juego cierra la anterior', () => {
  const { game, room } = openRoom();
  const pad = joinPad('pad', room);

  relay.receive('game', { t: 'create_room' });
  assert.notEqual(last(game).room, room);
  assert.deepEqual(last(pad), { t: 'error', code: 'room_not_found' });
});
