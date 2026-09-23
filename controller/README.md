# controller — página web del mando

Dueño: **Santiago Callocondo**.
Requisitos: RF-02, RF-03, RF-05 (lado mando), RF-08 (interfaz), RF-18; RNF-05, RNF-06.

Página que el celular abre al escanear el QR. HTML + CSS + JS vanilla, sin
framework: tiene que cargar rápido en cualquier celular.

## Estructura

```
controller/
├── public/               lo que el servidor sirve tal cual
│   ├── index.html        una sección por pantalla, solo una visible a la vez
│   ├── styles.css        botones grandes, alto contraste, sin distractores
│   └── js/
│       ├── main.js         orquesta el flujo: sala → permiso → calibrar → jugar
│       ├── connection.js   Socket.io: join, reconexión con backoff (RF-02, RF-07)
│       ├── sensors.js      permiso iOS + orientación y aceleración a 60 Hz (RF-03, RF-04)
│       ├── calibration.js  reposo de 2 s para la posición neutra (RF-05, CU-03)
│       ├── haptics.js      vibración de acierto y error (RF-18)
│       ├── room-code.js    código de sala, mismo alfabeto que el servidor
│       ├── ui.js           pantallas, barra de estado y avisos
│       └── wake-lock.js    la pantalla no se apaga a mitad de partida
└── tests/                lógica pura, sin navegador (`npm test`)
```

Todo vive bajo `public/` porque `server/src/index.js` sirve esa carpeta
completa: lo que esté fuera no llega al celular.

## Cómo se prueba

La página **no se abre sola**: la sirve el servidor.

```bash
cd server && npm install && npm run dev   # imprime la URL y el QR
cd controller && npm test                 # lógica de código de sala y calibración
```

Desde el celular se escanea el QR; desde la PC se puede abrir
`https://<ip>:<puerto>/?room=<código>` para revisar el diseño, aunque sin
sensores no pasa de la pantalla de calibración.

## Pantallas

| Pantalla | Cuándo aparece |
| --- | --- |
| `insecure` | La página se sirvió por HTTP: el navegador no entrega sensores |
| `code` | Se abrió sin `?room=`, o la sala no existe o está llena |
| `permission` | Hay sala; falta el toque que activa los sensores (obligatorio en iOS) |
| `denied` | El permiso fue denegado; explica cómo habilitarlo |
| `no-sensors` | Permiso concedido pero no llegan lecturas (p. ej. una laptop) |
| `calibrate` | Reposo de 2 s con anillo de avance (CU-03) |
| `pad` | Confirmar, recentrar y pausa (RF-08, RF-19, CU-07) |

Encima de todas, una barra de estado siempre visible y un aviso a pantalla
completa para pausa y desconexión (RF-07).

## Reglas

- El mando **no decide nada del juego**: reporta sensores y botones, nada más.
- Los botones responden en `pointerdown`, no en `click`: el gesto sale en cuanto
  el dedo toca (RNF-01).
- Botones grandes, pensados para usarse **sin mirar el celular**: el jugador
  mira la TV (RNF-06).
- Todo estado tiene una pantalla que dice qué pasa y qué hacer.
- iOS exige un gesto del usuario para pedir permiso de sensores: el botón
  "Activar sensores" es obligatorio (RF-03).
- Los valores de los sensores se mandan tal cual los da el navegador. El
  reconocedor de Unity usa la magnitud de la aceleración, así que el signo
  invertido de iOS no lo afecta.

Contrato de mensajes: [../docs/protocolo-ws.md](../docs/protocolo-ws.md)
