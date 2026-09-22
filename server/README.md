# server — Node.js + Socket.io

Dueños: **Piero Mejía** (`rooms/`, `relay/`) y **Misael Marrón** (`validation/`,
`certs/`).
Requisitos: RF-01, RF-04, RF-07, RF-20; RNF-01, RNF-03, RNF-04, RNF-07.

Empareja el mando con el juego y retransmite los datos de movimiento. Sirve
también la página del mando por HTTPS.

## Estructura prevista

```
server/
├── src/
│   ├── index.js          arranque: HTTPS + Express + Socket.io, imprime URL y QR
│   ├── rooms/            códigos de sala, emparejamiento, slots 1 y 2 (RF-01, RF-20)
│   ├── relay/            retransmisión mando ↔ Unity, detección de desconexión (RF-04, RF-07)
│   └── validation/       rangos, frecuencia, mensajes malformados
└── certs/                certificados locales (NO se commitean)
```

## Reglas

- El servidor **no interpreta gestos** ni guarda estado de juego: valida,
  empareja y retransmite.
- Todo mensaje inválido se descarta y se registra; **nunca llega a Unity**.
- Throttling a 80 Hz por mando.
- Un mando sin mensajes por 2 s se considera desconectado (RF-07).
- Sin base de datos, sin login, sin datos personales (RNF-04).

## HTTPS local

Los navegadores solo entregan los sensores por HTTPS. Cada integrante genera su
certificado en `certs/` (ver [certs/README.md](certs/README.md)); no se
commitean. El celular mostrará una advertencia la primera vez: es esperable en
red local.

Contrato de mensajes: [../docs/protocolo-ws.md](../docs/protocolo-ws.md)
