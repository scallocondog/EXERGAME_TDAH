// RF-01, RF-04: arranque del servidor (HTTPS + Express + Socket.io)
import { existsSync, readFileSync } from 'node:fs';
import { createServer as createHttpServer } from 'node:http';
import { createServer as createHttpsServer } from 'node:https';
import { fileURLToPath } from 'node:url';
import express from 'express';
import QRCode from 'qrcode';
import { Server } from 'socket.io';
import { findLanAddresses } from './lan-address.js';
import { Relay } from './relay/relay.js';
import { attachSocketIo } from './relay/socketio-transport.js';
import { RoomRegistry } from './rooms/room-registry.js';

const PORT = Number(process.env.PORT ?? 3443);
const SWEEP_INTERVAL_MS = 250;
const CERT_DIR = fileURLToPath(new URL('../certs/', import.meta.url));
const CONTROLLER_DIR = fileURLToPath(new URL('../../controller/public/', import.meta.url));

const lanAddresses = findLanAddresses();
const hostIp = process.env.HOST_IP ?? lanAddresses[0]?.address ?? '127.0.0.1';
const credentials = loadCredentials();
const origin = `${credentials ? 'https' : 'http'}://${hostIp}:${PORT}`;
const joinUrl = (room) => `${origin}/?room=${room}`;

const registry = new RoomRegistry();
const relay = new Relay({ registry, joinUrl, onRoomCreated: printRoomQr });

const app = express();
app.use(express.static(CONTROLLER_DIR));

// Unity descarga aquí la imagen del QR para mostrarla en pantalla (RF-01).
app.get('/qr/:room', async (req, res) => {
  const room = registry.getRoom(req.params.room);
  if (!room) return res.sendStatus(404);
  const png = await QRCode.toBuffer(joinUrl(room.code), { width: 512, margin: 1 });
  res.type('png').send(png);
});

const server = credentials ? createHttpsServer(credentials, app) : createHttpServer(app);
attachSocketIo(new Server(server), relay);
setInterval(() => relay.sweep(), SWEEP_INTERVAL_MS);

server.listen(PORT, '0.0.0.0', () => {
  console.log(`MoviMente escuchando en ${origin}`);
  if (!credentials) {
    console.warn(
      'AVISO: no hay key.pem y cert.pem en server/certs/, se sirve por HTTP.\n' +
        '       El celular NO entregará los sensores sin HTTPS (ver server/certs/README.md).',
    );
  }
  if (lanAddresses.length > 1 && !process.env.HOST_IP) {
    const list = lanAddresses.map((a) => `${a.address} (${a.name})`).join(', ');
    console.log(`Varias IP detectadas: ${list}. Si el celular no conecta, fija HOST_IP.`);
  }
  console.log('Esperando que el juego cree una sala...');
});

function loadCredentials() {
  const keyPath = `${CERT_DIR}key.pem`;
  const certPath = `${CERT_DIR}cert.pem`;
  if (!existsSync(keyPath) || !existsSync(certPath)) return null;
  return { key: readFileSync(keyPath), cert: readFileSync(certPath) };
}

async function printRoomQr(room, url) {
  const qr = await QRCode.toString(url, { type: 'terminal', small: true });
  console.log(`\nSala ${room} → ${url}\n${qr}`);
}
