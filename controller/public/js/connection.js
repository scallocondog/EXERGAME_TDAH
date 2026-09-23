// RF-02, RF-04, RF-07: conexión del mando con el servidor
// Todo viaja por el evento "message" de Socket.io, como espera server/src/relay/.
import { normalizeRoomCode } from './room-code.js';

// Los tiempos de reintento de protocolo-ws.md §5: 0.5 s, 1 s, 2 s, 4 s, tope 5 s.
const RECONNECT_DELAY_MS = 500;
const RECONNECT_DELAY_MAX_MS = 5000;

export class Connection {
  #socket = null;
  #room = '';
  #seq = 0;
  #onState;
  #onMessage;

  // onState recibe 'connecting' | 'joined' | 'disconnected'.
  constructor({ onState = () => {}, onMessage = () => {} } = {}) {
    this.#onState = onState;
    this.#onMessage = onMessage;
  }

  get room() {
    return this.#room;
  }

  start() {
    this.#socket = window.io({
      reconnectionDelay: RECONNECT_DELAY_MS,
      reconnectionDelayMax: RECONNECT_DELAY_MAX_MS,
      randomizationFactor: 0,
    });

    // El servidor identifica al mando por su conexión, así que hay que volver a
    // entrar en cada reconexión; él reconoce el slot reservado y responde
    // "joined" sin perder la partida (RNF-07).
    this.#socket.on('connect', () => {
      this.#onState('connecting');
      if (this.#room) this.#join();
    });

    this.#socket.on('disconnect', () => this.#onState('disconnected'));
    this.#socket.on('message', (msg) => this.#receive(msg));
  }

  join(room) {
    this.#room = normalizeRoomCode(room);
    this.#join();
  }

  // RF-04: una lectura de sensores ya combinada por sensors.js
  sendMotion({ ts, ori, acc }) {
    this.#send({ t: 'motion', room: this.#room, seq: this.#seq++, ts, ori, acc });
  }

  // RF-08, RF-19, CU-07: confirmar, recentrar o pausar
  sendButton(action) {
    this.#send({ t: 'button', room: this.#room, action });
  }

  // CU-03: la pose actual pasa a ser la neutra
  sendCalibrate() {
    this.#send({ t: 'calibrate', room: this.#room });
  }

  #join() {
    this.#send({ t: 'join', room: this.#room, v: 1 });
  }

  #send(msg) {
    if (this.#socket?.connected) this.#socket.send(msg);
  }

  #receive(msg) {
    if (!msg || typeof msg.t !== 'string') return;
    if (msg.t === 'joined') {
      this.#room = msg.room;
      this.#onState('joined', msg);
      return;
    }
    this.#onMessage(msg);
  }
}
