// RF-01: elección de la IP que va en el QR
import assert from 'node:assert/strict';
import { test } from 'node:test';
import { findRoutedAddress, pickHostAddress } from './lan-address.js';

// Caso real: Ethernet desconectado que Windows sigue listando y Wi-Fi activo.
const candidates = [
  { name: 'Ethernet', address: '192.168.15.23' },
  { name: 'Wi-Fi', address: '10.54.211.53' },
];

test('se usa la IP de la ruta por defecto aunque otra tenga mejor prefijo', () => {
  assert.equal(pickHostAddress(candidates, '10.54.211.53'), '10.54.211.53');
});

test('sin ruta (red sin internet) se usa la primera candidata', () => {
  assert.equal(pickHostAddress(candidates, null), '192.168.15.23');
});

test('si la ruta sale por un adaptador descartado (VPN) se usa la primera candidata', () => {
  assert.equal(pickHostAddress(candidates, '10.10.0.5'), '192.168.15.23');
});

test('sin candidatas no hay IP', () => {
  assert.equal(pickHostAddress([], '10.54.211.53'), null);
});

test('la IP de la ruta es una IPv4 o null, nunca lanza', async () => {
  const address = await findRoutedAddress();
  assert.ok(address === null || /^\d+\.\d+\.\d+\.\d+$/.test(address));
});
