// RF-04: pruebas de throttling de frecuencia (P-25 del plan de pruebas)
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { RateLimiter } from '../../server/src/validation/rate-limiter.js';

// ── Helpers ──────────────────────────────────────────────────────────────

function motion() {
  return {
    t: 'motion',
    room: 'A1B2',
    seq: 0,
    ts: Date.now(),
    ori: { alpha: 0, beta: 0, gamma: 0 },
    acc: { x: 0, y: 9.8, z: 0 },
  };
}

// ── Throttling ───────────────────────────────────────────────────────────

describe('RateLimiter — throttling a 80 Hz', () => {
  it('acepta el primer mensaje', () => {
    const limiter = new RateLimiter({ maxHz: 80 });
    assert.equal(limiter.check('pad-1', motion()).ok, true);
  });

  it('rechaza un segundo mensaje inmediato del mismo mando', () => {
    const limiter = new RateLimiter({ maxHz: 80 });
    limiter.check('pad-1', motion());
    const r = limiter.check('pad-1', motion());
    assert.equal(r.ok, false);
    assert.match(r.reason, /throttled/);
  });

  it('acepta mensajes de mandos distintos en rápida sucesión', () => {
    const limiter = new RateLimiter({ maxHz: 80 });
    assert.equal(limiter.check('pad-1', motion()).ok, true);
    assert.equal(limiter.check('pad-2', motion()).ok, true);
  });

  it('siempre acepta eventos discretos (button, calibrate)', () => {
    const limiter = new RateLimiter({ maxHz: 80 });
    const btn = { t: 'button', room: 'A1B2', action: 'confirm' };
    const cal = { t: 'calibrate', room: 'A1B2' };
    // Enviar muchos seguidos sin esperar
    for (let i = 0; i < 100; i++) {
      assert.equal(limiter.check('pad-1', btn).ok, true);
      assert.equal(limiter.check('pad-1', cal).ok, true);
    }
  });

  it('acepta motion después de esperar el intervalo mínimo', async () => {
    const limiter = new RateLimiter({ maxHz: 50 }); // 20 ms de intervalo
    limiter.check('pad-1', motion());
    await new Promise((r) => setTimeout(r, 25)); // esperar > 20 ms
    assert.equal(limiter.check('pad-1', motion()).ok, true);
  });

  it('forget() limpia el registro de un mando', () => {
    const limiter = new RateLimiter({ maxHz: 80 });
    limiter.check('pad-1', motion());
    limiter.forget('pad-1');
    // Ahora debería aceptar inmediatamente como si fuera nuevo
    assert.equal(limiter.check('pad-1', motion()).ok, true);
  });
});
