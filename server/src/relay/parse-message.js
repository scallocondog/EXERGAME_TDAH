// RF-04: los dos canales (Socket.io y WebSocket de Unity) entregan el mismo JSON
export function parseMessage(raw) {
  if (typeof raw !== 'string') return raw;
  try {
    return JSON.parse(raw);
  } catch {
    return null;
  }
}
