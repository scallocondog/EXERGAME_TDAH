// RF-01, RF-20: salas, emparejamiento juego ↔ mando y slots 1 y 2
import { generateRoomCode, normalizeRoomCode } from './room-code.js';

export const MAX_PADS = 2;

const SLOTS = Array.from({ length: MAX_PADS }, (_, i) => i + 1);

export class RoomRegistry {
  #rooms = new Map();

  createRoom(gameId) {
    const code = generateRoomCode((candidate) => this.#rooms.has(candidate));
    const room = { code, gameId, pads: new Map() };
    this.#rooms.set(code, room);
    return room;
  }

  getRoom(code) {
    return this.#rooms.get(normalizeRoomCode(code)) ?? null;
  }

  findRoomByGame(gameId) {
    for (const room of this.#rooms.values()) {
      if (room.gameId === gameId) return room;
    }
    return null;
  }

  closeRoom(code) {
    const room = this.getRoom(code);
    if (room) this.#rooms.delete(room.code);
    return room;
  }

  joinPad(code, padId) {
    const room = this.getRoom(code);
    if (!room) return { ok: false, error: 'room_not_found' };

    const slot = pickSlot(room);
    if (slot === null) return { ok: false, error: 'room_full' };

    const rejoined = room.pads.has(slot);
    room.pads.set(slot, { padId, connected: true });
    return { ok: true, room, slot, rejoined };
  }

  findPad(padId) {
    for (const room of this.#rooms.values()) {
      for (const [slot, pad] of room.pads) {
        if (pad.padId === padId) return { room, slot, pad };
      }
    }
    return null;
  }

  releasePad(padId) {
    const found = this.findPad(padId);
    if (found) found.room.pads.delete(found.slot);
    return found;
  }
}

// Un slot desconectado queda reservado para que su mando vuelva sin perder la
// partida (RF-07, RNF-07); solo si no hay ninguno se ocupa un slot libre.
function pickSlot(room) {
  const disconnected = SLOTS.find((slot) => room.pads.get(slot)?.connected === false);
  if (disconnected !== undefined) return disconnected;
  return SLOTS.find((slot) => !room.pads.has(slot)) ?? null;
}
