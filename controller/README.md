# controller — página web del mando

Dueño: **Santiago Callocondo**.
Requisitos: RF-02, RF-03, RF-05 (lado mando), RF-08 (interfaz), RF-10, RF-16,
RF-18; RNF-05, RNF-06.

Página que el celular abre al escanear el QR. HTML + CSS + JS vanilla, sin
framework: tiene que cargar rápido en cualquier celular.

## Estructura prevista

```
controller/
├── public/
│   ├── index.html        pantalla única del mando
│   ├── styles.css        botones grandes, alto contraste, sin distractores
│   └── assets/           iconos y audio
└── src/
    ├── connection.js     Socket.io: join, reconexión con backoff (RF-02, RF-07)
    ├── sensors.js        permiso iOS + DeviceOrientation/DeviceMotion (RF-03, RF-04)
    ├── calibration.js    posición neutra y recentrar (RF-05)
    ├── haptics.js        vibración en acierto y error (RF-18)
    └── ui.js             estados: sin permiso, conectando, conectado, desconectado
```

## Reglas

- El mando **no decide nada del juego**: reporta sensores y botones, nada más.
- Botones grandes, pensados para una mano de niño, sin texto pequeño (RNF-06).
- Todo estado tiene una pantalla clara: qué pasa y qué hacer.
- iOS exige un gesto del usuario para pedir permiso de sensores: el botón
  "Activar sensores" es obligatorio (RF-03).
- Se sirve desde `server/`; no se abre como `file://` porque los sensores exigen
  HTTPS.

Contrato de mensajes: [../docs/protocolo-ws.md](../docs/protocolo-ws.md)
