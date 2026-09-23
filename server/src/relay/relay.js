// RF-04, RF-07, RF-20: retransmisión mando ↔ Unity y detección de desconexión
import { normalizeRoomCode } from '../rooms/room-code.js';
import { checkShape } from './message-shape.js';
import { PadWatchdog } from './pad-watchdog.js';

export const PAD_TIMEOUT_MS = 2000;
const ERROR_INTERVAL_MS = 1000;

const PAD_STREAM_TYPES = new Set(['motion', 'button', 'calibrate']);
const HAPTIC_PATTERNS = new Set(['hit', 'miss']);
const PAD_STATES = new Set(['playing', 'paused', 'disconnected']);

// validate() lo aporta server/src/validation/ (Misael): rangos y frecuencia de
// protocolo-ws.md §4. Corre después de checkShape, sea cual sea el canal.
const acceptAll = () => ({ ok: true });

// El relay no conoce el transporte: recibe conexiones { id, send(msg) } y
// mensajes ya parseados. El servidor nunca interpreta gestos.
export class Relay {
  #registry;
  #joinUrl;
  #validate;
  #now;
  #log;
  #onRoomCreated;
  #watchdog;
  #connections = new Map();

  constructor({
    registry,
    joinUrl,
    validate = acceptAll,
    now = Date.now,
    log = console.log,
    onRoomCreated = () => {},
    timeoutMs = PAD_TIMEOUT_MS,
  }) {
    this.#registry = registry;
    this.#joinUrl = joinUrl;
    this.#validate = validate;
    this.#now = now;
    this.#log = log;
    this.#onRoomCreated = onRoomCreated;
    this.#watchdog = new PadWatchdog({ timeoutMs, onTimeout: (padId) => this.#padTimedOut(padId) });
  }

  connect(conn) {
    this.#connections.set(conn.id, { conn, role: null, lastErrorAt: -Infinity });
  }

  disconnect(connId) {
    const entry = this.#connections.get(connId);
    if (!entry) return;
    this.#connections.delete(connId);

    if (entry.role === 'game') this.#closeRoomOfGame(connId);
    if (entry.role === 'pad') {
      this.#watchdog.forget(connId);
      this.#markPadDisconnected(connId);
    }
  }

  receive(connId, msg) {
    const entry = this.#connections.get(connId);
    if (!entry) return;

    if (!isMessage(msg)) return this.#rejectPadMessage(entry, 'mensaje sin campo t o malformado');

    if (!entry.role && msg.t === 'create_room') entry.role = 'game';
    if (!entry.role && msg.t === 'join') entry.role = 'pad';

    if (entry.role === 'game') return this.#fromGame(entry, msg);
    if (entry.role === 'pad') return this.#fromPad(entry, msg);
    this.#rejectPadMessage(entry, `"${msg.t}" antes de create_room o join`);
  }

  sweep() {
    this.#watchdog.sweep(this.#now());
  }

  #fromGame(entry, msg) {
    const gameId = entry.conn.id;
    switch (msg.t) {
      case 'create_room':
        return this.#createRoom(entry);
      case 'haptic':
        if (!HAPTIC_PATTERNS.has(msg.pattern)) break;
        return this.#sendToPad(gameId, msg.slot, { t: 'haptic', pattern: msg.pattern });
      case 'state':
        if (!PAD_STATES.has(msg.value)) break;
        return this.#sendToPad(gameId, msg.slot, { t: 'state', value: msg.value });
    }
    this.#log(`[relay] descartado desde el juego: ${JSON.stringify(msg)}`);
  }

  #createRoom(entry) {
    this.#closeRoomOfGame(entry.conn.id);
    const room = this.#registry.createRoom(entry.conn.id);
    const url = this.#joinUrl(room.code);
    entry.conn.send({ t: 'room_created', room: room.code, url });
    this.#log(`[rooms] sala ${room.code} creada`);
    this.#onRoomCreated(room.code, url);
  }

  #fromPad(entry, msg) {
    if (msg.t === 'join') return this.#joinPad(entry, msg);
    if (!PAD_STREAM_TYPES.has(msg.t)) return this.#rejectPadMessage(entry, `tipo desconocido "${msg.t}"`);

    const padId = entry.conn.id;
    const found = this.#registry.findPad(padId);
    if (!found) return this.#sendPadError(entry, 'room_not_found', 'mensaje de un mando sin sala');
    if (normalizeRoomCode(msg.room) !== found.room.code) {
      return this.#rejectPadMessage(entry, `room "${msg.room}" no coincide con ${found.room.code}`);
    }

    const shape = checkShape(msg);
    const verdict = shape.ok ? this.#validate(msg) : shape;
    if (!verdict.ok) return this.#rejectPadMessage(entry, verdict.reason ?? 'no pasó la validación');

    this.#watchdog.touch(padId, this.#now());
    if (!found.pad.connected) {
      found.pad.connected = true;
      this.#notifyPadState(found.room, found.slot, 'connected');
    }
    this.#sendToGame(found.room, { ...msg, slot: found.slot });
  }

  #joinPad(entry, msg) {
    const padId = entry.conn.id;
    const current = this.#registry.findPad(padId);
    if (current?.room.code === normalizeRoomCode(msg.room)) {
      entry.conn.send({ t: 'joined', room: current.room.code, slot: current.slot });
      return;
    }
    if (current) {
      this.#registry.releasePad(padId);
      this.#notifyPadState(current.room, current.slot, 'disconnected');
    }

    const result = this.#registry.joinPad(msg.room, padId);
    if (!result.ok) {
      entry.conn.send({ t: 'error', code: result.error });
      this.#log(`[rooms] join rechazado a "${msg.room}": ${result.error}`);
      return;
    }

    this.#watchdog.touch(padId, this.#now());
    entry.conn.send({ t: 'joined', room: result.room.code, slot: result.slot });
    this.#notifyPadState(result.room, result.slot, 'connected');
    const how = result.rejoined ? 'reconectado' : 'conectado';
    this.#log(`[rooms] mando ${how} a ${result.room.code} en slot ${result.slot}`);
  }

  #padTimedOut(padId) {
    if (this.#markPadDisconnected(padId)) {
      this.#log(`[relay] mando sin señal por ${PAD_TIMEOUT_MS} ms`);
    }
  }

  #markPadDisconnected(padId) {
    const found = this.#registry.findPad(padId);
    if (!found?.pad.connected) return false;
    found.pad.connected = false;
    this.#notifyPadState(found.room, found.slot, 'disconnected');
    return true;
  }

  #closeRoomOfGame(gameId) {
    const room = this.#registry.findRoomByGame(gameId);
    if (!room) return;
    this.#registry.closeRoom(room.code);
    for (const { padId } of room.pads.values()) {
      this.#watchdog.forget(padId);
      this.#connections.get(padId)?.conn.send({ t: 'error', code: 'room_not_found' });
    }
    this.#log(`[rooms] sala ${room.code} cerrada`);
  }

  #notifyPadState(room, slot, value) {
    this.#sendToGame(room, { t: 'pad_state', slot, value });
  }

  #sendToGame(room, msg) {
    this.#connections.get(room.gameId)?.conn.send(msg);
  }

  #sendToPad(gameId, slot, msg) {
    const pad = this.#registry.findRoomByGame(gameId)?.pads.get(slot);
    if (!pad?.connected) return;
    this.#connections.get(pad.padId)?.conn.send(msg);
  }

  #rejectPadMessage(entry, reason) {
    this.#sendPadError(entry, 'bad_message', reason);
  }

  // Como máximo un error por segundo y por mando para no inundar la red.
  #sendPadError(entry, code, reason) {
    this.#log(`[relay] descartado: ${reason}`);
    const now = this.#now();
    if (now - entry.lastErrorAt < ERROR_INTERVAL_MS) return;
    entry.lastErrorAt = now;
    entry.conn.send({ t: 'error', code });
  }
}

function isMessage(msg) {
  return typeof msg === 'object' && msg !== null && typeof msg.t === 'string';
}
