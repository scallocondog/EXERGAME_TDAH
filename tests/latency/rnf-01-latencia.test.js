// RNF-01: medición de latencia del relay en loopback
// Se miden 100 mensajes motion ida y vuelta (mando → servidor → Unity) y se
// verifica que la mediana sea < 100 ms. Esta prueba corre sin red Wi-Fi real:
// simula el camino completo en memoria para establecer un piso de rendimiento.
import { describe, it, before, after } from 'node:test';
import assert from 'node:assert/strict';
import { Relay } from '../../server/src/relay/relay.js';
import { RoomRegistry } from '../../server/src/rooms/room-registry.js';
import { createValidator, RateLimiter } from '../../server/src/validation/index.js';

const SAMPLE_COUNT = 100;
const MAX_MEDIAN_MS = 100;

describe('RNF-01 — latencia del relay < 100 ms', () => {
  let relay;
  let registry;
  const received = [];

  /** @type {{ send: (msg: any) => void }} */
  let gameConn;
  /** @type {{ send: (msg: any) => void }} */
  let padConn;
  let roomCode;

  before(() => {
    registry = new RoomRegistry();
    const rateLimiter = new RateLimiter({ maxHz: Infinity }); // sin throttling para la prueba
    const validate = createValidator({ rateLimiter });

    relay = new Relay({
      registry,
      joinUrl: (code) => `https://localhost:3443/?room=${code}`,
      validate,
      now: () => Date.now(),
      log: () => {}, // silenciar logs durante la prueba
    });

    // Simular conexión del juego (Unity)
    gameConn = {
      id: 'game-1',
      send: (msg) => {
        if (msg.t === 'room_created') {
          roomCode = msg.room;
        }
        if (msg.t === 'motion') {
          received.push({ arrivedAt: performance.now(), msg });
        }
      },
    };
    relay.connect(gameConn);
    relay.receive('game-1', { t: 'create_room' });

    // Simular conexión del mando
    padConn = {
      id: 'pad-1',
      send: () => {},
    };
    relay.connect(padConn);
    relay.receive('pad-1', { t: 'join', room: roomCode, v: 1 });
  });

  after(() => {
    relay.disconnect('pad-1');
    relay.disconnect('game-1');
  });

  it(`mediana de ${SAMPLE_COUNT} mensajes motion < ${MAX_MEDIAN_MS} ms`, () => {
    const sentTimestamps = [];

    for (let i = 0; i < SAMPLE_COUNT; i++) {
      const sentAt = performance.now();
      sentTimestamps.push(sentAt);

      relay.receive('pad-1', {
        t: 'motion',
        room: roomCode,
        seq: i,
        ts: Date.now(),
        ori: { alpha: 180, beta: 0, gamma: 0 },
        acc: { x: 0, y: 9.8, z: 0 },
      });
    }

    assert.equal(received.length, SAMPLE_COUNT, `esperaba ${SAMPLE_COUNT} mensajes, llegaron ${received.length}`);

    const latencies = received.map((r, i) => r.arrivedAt - sentTimestamps[i]);
    latencies.sort((a, b) => a - b);

    const median = latencies[Math.floor(latencies.length / 2)];
    const max = latencies[latencies.length - 1];
    const min = latencies[0];

    console.log(`  Latencia relay (loopback): min=${min.toFixed(3)} ms, mediana=${median.toFixed(3)} ms, max=${max.toFixed(3)} ms`);

    assert.ok(median < MAX_MEDIAN_MS, `mediana ${median.toFixed(3)} ms supera el límite de ${MAX_MEDIAN_MS} ms`);
  });
});
