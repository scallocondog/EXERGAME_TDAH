// RF-04: adaptador Socket.io del relay
// Todo mensaje viaja por el evento estándar "message" (socket.send en ambos lados).
import { parseMessage } from './parse-message.js';

export function attachSocketIo(io, relay) {
  io.on('connection', (socket) => {
    relay.connect({ id: socket.id, send: (msg) => socket.send(msg) });
    socket.on('message', (raw) => relay.receive(socket.id, parseMessage(raw)));
    socket.on('disconnect', () => relay.disconnect(socket.id));
  });
}
