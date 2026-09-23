// RF-04: Unity por WebSocket puro y mando por Socket.io, sobre el mismo relay
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { after, before, test } from 'node:test';
import { Server } from 'socket.io';
import { io as connect } from 'socket.io-client';
import { WebSocket } from 'ws';
import { RoomRegistry } from '../rooms/room-registry.js';
import { Relay } from './relay.js';
import { attachSocketIo } from './socketio-transport.js';
import { attachUnityWebSocket, UNITY_WS_PATH } from './ws-transport.js';

let httpServer;
let port;
const closers = [];

before(async () => {
  httpServer = createServer();
  const relay = new Relay({ registry: new RoomRegistry(), joinUrl: (room) => room, log: () => {} });
  attachSocketIo(new Server(httpServer, { destroyUpgrade: false }), relay);
  attachUnityWebSocket(httpServer, relay);
  await new Promise((resolve) => httpServer.listen(0, '127.0.0.1', resolve));
  port = httpServer.address().port;
});

after(() => {
  for (const close of closers) close();
  httpServer.closeAllConnections();
  httpServer.close();
});

async function unityClient() {
  const ws = new WebSocket(`ws://127.0.0.1:${port}${UNITY_WS_PATH}`);
  closers.push(() => ws.close());
  await new Promise((resolve, reject) => {
    ws.once('open', resolve);
    ws.once('error', reject);
  });
  return ws;
}

function nextUnityMessage(ws, type) {
  return new Promise((resolve) => {
    const onMessage = (data) => {
      const msg = JSON.parse(data.toString());
      if (msg.t !== type) return;
      ws.off('message', onMessage);
      resolve(msg);
    };
    ws.on('message', onMessage);
  });
}

function padClient() {
  const socket = connect(`http://127.0.0.1:${port}`, { transports: ['websocket'] });
  closers.push(() => socket.close());
  return socket;
}

function nextPadMessage(socket, type) {
  return new Promise((resolve) => {
    const onMessage = (msg) => {
      if (msg.t !== type) return;
      socket.off('message', onMessage);
      resolve(msg);
    };
    socket.on('message', onMessage);
  });
}

test('Unity por /unity crea la sala y recibe el movimiento del mando', async () => {
  const unity = await unityClient();
  const created = nextUnityMessage(unity, 'room_created');
  unity.send(JSON.stringify({ t: 'create_room' }));
  const { room } = await created;

  const pad = padClient();
  const joined = nextPadMessage(pad, 'joined');
  const connected = nextUnityMessage(unity, 'pad_state');
  pad.send({ t: 'join', room, v: 1 });
  assert.equal((await joined).slot, 1);
  assert.equal((await connected).value, 'connected');

  const motion = nextUnityMessage(unity, 'motion');
  pad.volatile.send({
    t: 'motion',
    room,
    seq: 7,
    ts: 1737590000123,
    ori: { alpha: 0, beta: 30, gamma: -12.5 },
    acc: { x: 0.1, y: 9.8, z: 0 },
  });
  const received = await motion;
  assert.equal(received.slot, 1);
  assert.equal(received.ori.gamma, -12.5);

  const haptic = nextPadMessage(pad, 'haptic');
  unity.send(JSON.stringify({ t: 'haptic', slot: 1, pattern: 'miss' }));
  assert.equal((await haptic).pattern, 'miss');
});

test('JSON malformado por /unity no rompe el canal', async () => {
  const unity = await unityClient();
  unity.send('{no es json');

  const created = nextUnityMessage(unity, 'room_created');
  unity.send(JSON.stringify({ t: 'create_room' }));
  assert.match((await created).room, /^[A-Z2-9]{4}$/);
});

test('un upgrade a otra ruta se cierra de inmediato', async () => {
  const ws = new WebSocket(`ws://127.0.0.1:${port}/otra`);
  closers.push(() => ws.terminate());
  const outcome = await new Promise((resolve) => {
    ws.once('open', () => resolve('open'));
    ws.once('error', () => resolve('error'));
    ws.once('close', () => resolve('close'));
    setTimeout(() => resolve('timeout'), 1500);
  });
  assert.equal(outcome, 'error');
});
