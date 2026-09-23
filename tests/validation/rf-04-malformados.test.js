// RF-04: pruebas de mensajes malformados y tipos desconocidos
import { describe, it } from 'node:test';
import assert from 'node:assert/strict';
import { checkShape } from '../../server/src/relay/message-shape.js';
import { validateRanges } from '../../server/src/validation/message-validator.js';

// ── Helpers ──────────────────────────────────────────────────────────────

function fullPipeline(msg) {
  const shape = checkShape(msg);
  if (!shape.ok) return shape;
  return validateRanges(msg);
}

// ── JSON malformado (P-23 del plan de pruebas) ───────────────────────────
// El JSON malformado se atrapa en parse-message.js → devuelve null.
// relay.js comprueba isMessage(null) → false → rechazado.
// Aquí verificamos que el pipeline completo rechaza entradas inválidas.

describe('mensajes malformados', () => {
  it('rechaza motion sin campo seq', () => {
    const msg = { t: 'motion', room: 'A1B2', ts: 123, ori: { alpha: 0, beta: 0, gamma: 0 }, acc: { x: 0, y: 0, z: 0 } };
    assert.equal(fullPipeline(msg).ok, false);
  });

  it('rechaza motion con seq negativo', () => {
    const msg = { t: 'motion', room: 'A1B2', seq: -1, ts: 123, ori: { alpha: 0, beta: 0, gamma: 0 }, acc: { x: 0, y: 0, z: 0 } };
    assert.equal(fullPipeline(msg).ok, false);
  });

  it('rechaza motion con seq fraccionario', () => {
    const msg = { t: 'motion', room: 'A1B2', seq: 1.5, ts: 123, ori: { alpha: 0, beta: 0, gamma: 0 }, acc: { x: 0, y: 0, z: 0 } };
    assert.equal(fullPipeline(msg).ok, false);
  });

  it('rechaza motion sin ts', () => {
    const msg = { t: 'motion', room: 'A1B2', seq: 0, ori: { alpha: 0, beta: 0, gamma: 0 }, acc: { x: 0, y: 0, z: 0 } };
    assert.equal(fullPipeline(msg).ok, false);
  });

  it('rechaza motion con ori null', () => {
    const msg = { t: 'motion', room: 'A1B2', seq: 0, ts: 123, ori: null, acc: { x: 0, y: 0, z: 0 } };
    assert.equal(fullPipeline(msg).ok, false);
  });

  it('rechaza motion con ori.alpha como string', () => {
    const msg = { t: 'motion', room: 'A1B2', seq: 0, ts: 123, ori: { alpha: 'abc', beta: 0, gamma: 0 }, acc: { x: 0, y: 0, z: 0 } };
    assert.equal(fullPipeline(msg).ok, false);
  });

  it('rechaza motion con acc faltante', () => {
    const msg = { t: 'motion', room: 'A1B2', seq: 0, ts: 123, ori: { alpha: 0, beta: 0, gamma: 0 } };
    assert.equal(fullPipeline(msg).ok, false);
  });

  it('rechaza motion con acc.x = NaN (llega como null en JSON)', () => {
    const msg = { t: 'motion', room: 'A1B2', seq: 0, ts: 123, ori: { alpha: 0, beta: 0, gamma: 0 }, acc: { x: null, y: 0, z: 0 } };
    assert.equal(fullPipeline(msg).ok, false);
  });

  it('rechaza button con action desconocida', () => {
    const msg = { t: 'button', room: 'A1B2', action: 'jump' };
    assert.equal(fullPipeline(msg).ok, false);
  });

  it('acepta button con action válida', () => {
    assert.equal(fullPipeline({ t: 'button', room: 'A1B2', action: 'confirm' }).ok, true);
    assert.equal(fullPipeline({ t: 'button', room: 'A1B2', action: 'recenter' }).ok, true);
    assert.equal(fullPipeline({ t: 'button', room: 'A1B2', action: 'pause' }).ok, true);
  });

  it('acepta calibrate sin campos extra', () => {
    assert.equal(fullPipeline({ t: 'calibrate', room: 'A1B2' }).ok, true);
  });
});
