// RF-04: pruebas de validación de rangos (protocolo-ws.md §4)
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { validateRanges } from '../../server/src/validation/message-validator.js';

// ── Helpers ──────────────────────────────────────────────────────────────

function motion(overrides = {}) {
  return {
    t: 'motion',
    room: 'A1B2',
    seq: 0,
    ts: Date.now(),
    ori: { alpha: 180, beta: 0, gamma: 0 },
    acc: { x: 0, y: 9.8, z: 0 },
    ...overrides,
  };
}

// ── Mensajes válidos ─────────────────────────────────────────────────────

describe('validateRanges — mensajes válidos', () => {
  it('acepta motion con valores en el centro de los rangos', () => {
    const result = validateRanges(motion());
    assert.equal(result.ok, true);
  });

  it('acepta motion en los límites exactos (alpha=0, beta=-180, gamma=-90)', () => {
    const msg = motion({ ori: { alpha: 0, beta: -180, gamma: -90 } });
    assert.equal(validateRanges(msg).ok, true);
  });

  it('acepta motion en los límites superiores (alpha=360, beta=180, gamma=90)', () => {
    const msg = motion({ ori: { alpha: 360, beta: 180, gamma: 90 } });
    assert.equal(validateRanges(msg).ok, true);
  });

  it('acepta motion con acc en los extremos (-60 a 60)', () => {
    const msg = motion({ acc: { x: -60, y: 60, z: 0 } });
    assert.equal(validateRanges(msg).ok, true);
  });

  it('acepta mensajes que no son motion (button, calibrate)', () => {
    assert.equal(validateRanges({ t: 'button', room: 'A1B2', action: 'confirm' }).ok, true);
    assert.equal(validateRanges({ t: 'calibrate', room: 'A1B2' }).ok, true);
  });
});

// ── Orientación fuera de rango ───────────────────────────────────────────

describe('validateRanges — ori fuera de rango', () => {
  it('rechaza alpha negativo', () => {
    const msg = motion({ ori: { alpha: -1, beta: 0, gamma: 0 } });
    const r = validateRanges(msg);
    assert.equal(r.ok, false);
    assert.match(r.reason, /ori\.alpha/);
  });

  it('rechaza alpha > 360', () => {
    const msg = motion({ ori: { alpha: 361, beta: 0, gamma: 0 } });
    assert.equal(validateRanges(msg).ok, false);
  });

  it('rechaza beta < -180', () => {
    const msg = motion({ ori: { alpha: 0, beta: -181, gamma: 0 } });
    assert.equal(validateRanges(msg).ok, false);
  });

  it('rechaza beta > 180', () => {
    const msg = motion({ ori: { alpha: 0, beta: 181, gamma: 0 } });
    assert.equal(validateRanges(msg).ok, false);
  });

  it('rechaza gamma < -90', () => {
    const msg = motion({ ori: { alpha: 0, beta: 0, gamma: -91 } });
    assert.equal(validateRanges(msg).ok, false);
  });

  it('rechaza gamma > 90', () => {
    const msg = motion({ ori: { alpha: 0, beta: 0, gamma: 91 } });
    assert.equal(validateRanges(msg).ok, false);
  });

  it('rechaza beta = 999 (P-24 del plan de pruebas)', () => {
    const msg = motion({ ori: { alpha: 0, beta: 999, gamma: 0 } });
    const r = validateRanges(msg);
    assert.equal(r.ok, false);
    assert.match(r.reason, /ori\.beta/);
  });
});

// ── Aceleración fuera de rango ───────────────────────────────────────────

describe('validateRanges — acc fuera de rango', () => {
  it('rechaza acc.x < -60', () => {
    const msg = motion({ acc: { x: -61, y: 0, z: 0 } });
    assert.equal(validateRanges(msg).ok, false);
  });

  it('rechaza acc.y > 60', () => {
    const msg = motion({ acc: { x: 0, y: 61, z: 0 } });
    assert.equal(validateRanges(msg).ok, false);
  });

  it('rechaza acc.z fuera de rango', () => {
    const msg = motion({ acc: { x: 0, y: 0, z: 100 } });
    assert.equal(validateRanges(msg).ok, false);
  });
});
