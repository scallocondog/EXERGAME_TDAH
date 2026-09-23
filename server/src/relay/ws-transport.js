// RF-04: canal WebSocket puro para Unity (NativeWebSocket), sobre el mismo relay
import { randomUUID } from 'node:crypto';
import { WebSocket, WebSocketServer } from 'ws';
import { parseMessage } from './parse-message.js';

export const UNITY_WS_PATH = '/unity';
// Socket.io atiende sus propios upgrades en esta ruta; el resto se cierra.
const SOCKET_IO_PATH = '/socket.io/';

// Si Unity se atrasa, el motion viejo se descarta en vez de acumularse:
// mejor un dato perdido que un gesto que llega tarde (RNF-01).
const MAX_BUFFERED_BYTES = 64 * 1024;

export function attachUnityWebSocket(httpServer, relay) {
  const wss = new WebSocketServer({ noServer: true });

  httpServer.on('upgrade', (req, socket, head) => {
    const { pathname } = new URL(req.url, 'http://localhost');
    if (pathname === UNITY_WS_PATH) {
      wss.handleUpgrade(req, socket, head, (ws) => wss.emit('connection', ws));
    } else if (!pathname.startsWith(SOCKET_IO_PATH)) {
      socket.destroy();
    }
  });

  wss.on('connection', (ws) => {
    const id = `ws:${randomUUID()}`;
    relay.connect({ id, send: (msg) => send(ws, msg) });
    ws.on('message', (data, isBinary) => relay.receive(id, isBinary ? null : parseMessage(data.toString())));
    ws.on('close', () => relay.disconnect(id));
  });

  return wss;
}

function send(ws, msg) {
  if (ws.readyState !== WebSocket.OPEN) return;
  if (msg.t === 'motion' && ws.bufferedAmount > MAX_BUFFERED_BYTES) return;
  ws.send(JSON.stringify(msg));
}
