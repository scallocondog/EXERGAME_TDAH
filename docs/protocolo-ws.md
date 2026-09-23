# Protocolo WebSocket — MoviMente

Contrato entre mando, servidor y Unity. **Cambiarlo requiere acuerdo de los tres
integrantes**, y el cambio se documenta aquí en el mismo PR que lo implementa.

- Transporte (mismo servidor y puerto, dos canales):
  - **Mando:** Socket.io 4.x sobre HTTPS/WSS. Todo mensaje va por el evento
    estándar `message`: `socket.send(obj)` para enviar y `socket.on('message')`
    para recibir. `motion` se envía con `socket.volatile.send(obj)` para que un
    dato atrasado se descarte en vez de acumularse.
  - **Unity:** WebSocket puro (NativeWebSocket) en `wss://<ip-lan>:<puerto>/unity`.
    Un mensaje por frame de texto, con el JSON serializado.
  - Los dos canales llevan **el mismo JSON** y pasan por **el mismo relay y la
    misma validación**. El rol lo fija el primer mensaje: `create_room` → juego,
    `join` → mando.
- Codificación: JSON UTF-8.
- Campo `t` (type) obligatorio en todo mensaje.
- Campo `room` obligatorio en todo mensaje que envía el mando.
- Versión del protocolo: `v1`.

---

## 1. Mando → Servidor

### `join` — entrar a la sala (RF-02, CU-02)

```json
{ "t": "join", "room": "A7K2", "v": 1 }
```

Respuesta del servidor: `joined` o `error`.

### `motion` — flujo continuo de sensores (RF-04)

```json
{
  "t": "motion",
  "room": "A7K2",
  "seq": 128,
  "ts": 1737590000123,
  "ori": { "alpha": 12.4, "beta": -3.1, "gamma": 45.0 },
  "acc": { "x": 0.12, "y": 9.71, "z": 0.03 }
}
```

| Campo | Tipo | Rango válido | Nota |
| --- | --- | --- | --- |
| `seq` | entero | ≥ 0, creciente | Detecta pérdidas y desorden |
| `ts` | entero (ms) | epoch del celular | Base de la medición de RNF-01 |
| `ori.alpha` | número | 0 a 360 | Giro sobre el eje Z |
| `ori.beta` | número | −180 a 180 | Inclinación adelante/atrás |
| `ori.gamma` | número | −90 a 90 | Inclinación izquierda/derecha |
| `acc.x/y/z` | número | −60 a 60 (m/s²) | Aceleración incluyendo gravedad |

**Frecuencia:** 30–60 Hz. El mando limita el envío a unos 60 Hz; el servidor
descarta lo que supere 80 Hz por mando.

### `button` — eventos discretos (RF-08, RF-19)

```json
{ "t": "button", "room": "A7K2", "action": "confirm" }
```

`action`: `confirm` | `recenter` | `pause`.

### `calibrate` — fijar posición neutra (RF-05, CU-03)

```json
{ "t": "calibrate", "room": "A7K2" }
```

---

## 2. Servidor → Mando

| `t` | Contenido | Cuándo |
| --- | --- | --- |
| `joined` | `{ "t": "joined", "room": "A7K2", "slot": 1 }` | Emparejamiento correcto (`slot` 1 o 2, RF-20) |
| `error` | `{ "t": "error", "code": "room_not_found" }` | `room_not_found`, `room_full`, `bad_message` |
| `haptic` | `{ "t": "haptic", "pattern": "hit" }` | Acierto o error en el juego (RF-18); `hit` \| `miss` |
| `state` | `{ "t": "state", "value": "paused" }` | `playing` \| `paused` \| `disconnected` (RF-07) |

---

## 3. Unity ↔ Servidor

| `t` | Dirección | Contenido |
| --- | --- | --- |
| `create_room` | Unity → Servidor | `{ "t": "create_room" }` → responde `{ "t": "room_created", "room": "A7K2", "url": "https://192.168.1.10:3443/?room=A7K2" }` (RF-01) |
| `motion` | Servidor → Unity | El mismo mensaje del mando, ya validado, con `slot` agregado |
| `button` | Servidor → Unity | Igual, con `slot` |
| `calibrate` | Servidor → Unity | Igual, con `slot` |
| `pad_state` | Servidor → Unity | `{ "t": "pad_state", "slot": 1, "value": "connected" }` — `connected` \| `disconnected` (RF-07, CU-08) |
| `haptic` | Unity → Servidor | `{ "t": "haptic", "slot": 1, "pattern": "hit" }` |
| `state` | Unity → Servidor | `{ "t": "state", "slot": 1, "value": "paused" }` |

---

## 4. Validación en el servidor (RF-04, responsabilidad de Misael)

Primero el relay exige una forma mínima (`server/src/relay/message-shape.js`):
en `motion`, `seq` entero ≥ 0, `ts` entero y los seis valores de `ori` y `acc`
numéricos y finitos (`NaN` e `Infinity` viajan como `null` en JSON y se
rechazan); en `button`, una `action` conocida. Después corre la validación de
`server/src/validation/`.

Un mensaje se **descarta y se registra** si:

- No tiene `t`, o `t` no está en la lista de tipos conocidos.
- Le falta `room` cuando viene del mando, o la sala no existe.
- Algún valor numérico está fuera del rango de la tabla, o es `NaN` / `null`.
- Llega a más de 80 Hz desde el mismo mando (se aplica throttling).
- El JSON está malformado.

Regla: **un mensaje inválido nunca llega a Unity**. El servidor responde
`{ "t": "error", "code": "bad_message" }` como máximo una vez por segundo y por
mando, para no inundar la red.

---

## 5. Reconexión (RF-07, RNF-07, CU-08)

1. El servidor considera **desconectado** un mando tras **2 s sin mensajes**.
2. Emite `pad_state: disconnected` a Unity, que pausa la partida y muestra el aviso.
3. El mando reintenta con backoff: 0.5 s, 1 s, 2 s, 4 s, tope de 5 s.
4. Al reconectar con el mismo `room` y `slot`, el servidor emite
   `pad_state: connected` y Unity retoma la partida donde quedó. **No se pierde
   el puntaje.**

---

## 6. Historial de cambios

| Fecha | Versión | Cambio | Acordado por |
| --- | --- | --- | --- |
| 2026-09-22 | v1 | Versión inicial del contrato | Santiago, Piero, Misael |
| 2026-09-22 | v1.1 | Evento `message` y `volatile` en el mando, tope de ~60 Hz en el mando, canal WebSocket puro `/unity` para Unity con el mismo JSON y la misma validación, forma mínima en el relay. Los mensajes no cambian: `join` sigue con `"v": 1` | Santiago, Piero, Misael |
