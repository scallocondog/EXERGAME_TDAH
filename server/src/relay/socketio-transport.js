// RF-04: adaptador Socket.io del relay
// Todo mensaje viaja por el evento estándar "message" (socket.send en ambos lados).
export function attachSocketIo(io, relay) {
  io.on('connection', (socket) => {
    relay.connect({ id: socket.id, send: (msg) => socket.send(msg) });
    socket.on('message', (raw) => relay.receive(socket.id, parse(raw)));
    socket.on('disconnect', () => relay.disconnect(socket.id));
  });
}

// Algunos clientes (p. ej. Unity) mandan el JSON como texto.
function parse(raw) {
  if (typeof raw !== 'string') return raw;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}
