// RF-05, CU-03: sostener el celular quieto dos segundos fija la posición neutra
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { NeutralHold } from '../public/js/calibration.js';

function hold(samples) {
  let progress = 0;
  let done = false;
  const neutral = new NeutralHold({
    onProgress: (value) => {
      progress = value;
    },
    onComplete: () => {
      done = true;
    },
  });
  for (const sample of samples) neutral.observe(sample);
  return { progress, done, neutral };
}

// 45 lecturas cada 50 ms son 2.2 s: el mando manda entre 30 y 60 Hz.
function still(count, beta = 40, step = 50) {
  return Array.from({ length: count }, (_, i) => ({
    ts: 1000 + i * step,
    ori: { alpha: 0, beta, gamma: 2 },
  }));
}

test('quieto dos segundos fija la posición neutra', () => {
  const { done, progress } = hold(still(45));
  assert.equal(done, true);
  assert.equal(progress, 1);
});

test('no se completa antes de los dos segundos', () => {
  const { done, progress } = hold(still(20));
  assert.equal(done, false);
  assert.ok(progress > 0 && progress < 1, `progreso ${progress}`);
});

test('moverse de verdad reinicia la cuenta', () => {
  const { done, progress } = hold([...still(20), ...still(10, 70)]);
  assert.equal(done, false);
  assert.ok(progress < 0.35, `progreso ${progress}`);
});

// Un niño no sostiene el celular perfectamente quieto (RNF-05).
test('un temblor pequeño no reinicia la cuenta', () => {
  const samples = Array.from({ length: 45 }, (_, i) => ({
    ts: 1000 + i * 50,
    ori: { alpha: 0, beta: 40 + (i % 2 === 0 ? 2 : -2), gamma: 2 },
  }));
  assert.equal(hold(samples).done, true);
});

test('179 y -179 grados no se toman como un movimiento enorme', () => {
  const samples = Array.from({ length: 45 }, (_, i) => ({
    ts: 1000 + i * 50,
    ori: { alpha: 0, beta: i % 2 === 0 ? 179 : -179, gamma: 0 },
  }));
  assert.equal(hold(samples).done, true);
});

test('reset vuelve a empezar', () => {
  const { neutral } = hold(still(20));
  let progress = -1;
  neutral.reset();
  const fresh = new NeutralHold({
    onProgress: (value) => {
      progress = value;
    },
    onComplete: () => {},
  });
  fresh.reset();
  assert.equal(progress, 0);
});
