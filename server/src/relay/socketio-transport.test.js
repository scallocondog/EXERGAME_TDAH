// RF-01, RF-04: prueba de punta a punta sobre Socket.io real
import assert from 'node:assert/strict';
import { createServer } from 'node:http';
import { after, before, test } from 'node:test';
import { Server } from 'socket.io';
import { io as connect } from 'socket.io-client';
import { RoomRegistry } from '../rooms/room-registry.js';
import { Relay } from './relay.js';
import { attachSocketIo } from './socketio-transport.js';

let httpServer;
let url;
const clients = [];

before(async () => {
  httpServer = createServer();
  const relay = new Relay({ registry: new RoomRegistry(), joinUrl: (room) => room, log: () => {} });
  attachSocketIo(new Server(httpServer), relay);
  await new Promise((resolve) => httpServer.listen(0, '127.0.0.1', resolve));
  url = `http://127.0.0.1:${httpServer.address().port}`;
});

after(() => {
  for (const client of clients) client.close();
  httpServer.closeAllConnections();
  httpServer.close();
});

function client() {
  const socket = connect(url, { transports: ['websocket'] });
  clients.push(socket);
  return socket;
}

function nextMessage(socket, type) {
  return new Promise((resolve) => {
    const onMessage = (msg) => {
      if (msg.t !== type) return;
      socket.off('message', onMessage);
      resolve(msg);
    };
    socket.on('message', onMessage);
  });
}

test('juego y mando se emparejan y el movimiento llega al juego', async () => {
  const game = client();
  const created = nextMessage(game, 'room_created');
  game.send({ t: 'create_room' });
  const { room } = await created;

  const pad = client();
  const joined = nextMessage(pad, 'joined');
  const connected = nextMessage(game, 'pad_state');
  pad.send({ t: 'join', room, v: 1 });
  assert.equal((await joined).slot, 1);
  assert.equal((await connected).value, 'connected');

  const motion = nextMessage(game, 'motion');
  // Unity puede mandar el JSON como texto: el transporte lo acepta igual.
  pad.send(JSON.stringify({ t: 'motion', room, seq: 1, ts: 1, ori: {}, acc: {} }));
  assert.equal((await motion).slot, 1);

  const haptic = nextMessage(pad, 'haptic');
  game.send({ t: 'haptic', slot: 1, pattern: 'hit' });
  assert.equal((await haptic).pattern, 'hit');

  const disconnected = nextMessage(game, 'pad_state');
  pad.close();
  assert.equal((await disconnected).value, 'disconnected');
});
