# unity — proyecto del juego

Dueños: **Piero Mejía** (`Scripts/Net`, `Scripts/Gestures`, `Scripts/Games`),
**Santiago Callocondo** (`Assets/UI`), **Misael Marrón** (`Scripts/Metrics`,
`Scripts/Storage`).

Unity **2022 LTS**. No abrir con otra versión: Unity reescribe archivos del
proyecto y ensucia el diff para todos.

## Estructura prevista

```
unity/Assets/
├── Scripts/
│   ├── Net/          cliente WebSocket, estado de conexión, pausa por desconexión (RF-07)
│   ├── Gestures/     calibración y reconocedor de los 5 gestos (RF-05, RF-06)
│   ├── Games/        un módulo por minijuego sobre una base común (RF-11 a RF-14)
│   ├── Metrics/      aciertos, errores, omisiones, tiempo de reacción, puntaje (RF-15)
│   └── Storage/      mejores puntajes y niveles desbloqueados (RF-17)
├── UI/               menús, instrucciones, HUD, resultados (RF-08, RF-10, RF-16)
├── Scenes/           MainMenu, Game_AtrapaLoCorrecto, Game_SigueLaSecuencia,
│                     Game_CortaSinFallar, Game_Equilibrio
├── Audio/
└── Art/
```

## Red y diagnóstico (`Scripts/Net`, `Scripts/Diagnostics`, RF-01, RF-07)

- `ServerConnection`: WebSocket a `/unity` (NativeWebSocket 2.0.7), crea la
  sala al conectar y reintenta con 0,5 / 1 / 2 / 4 / 5 s. Si se reconecta, la
  sala es nueva y los mandos tienen que volver a escanear.
- `GameLink`: interpreta los mensajes del servidor (`room_created`, `pad_state`,
  `motion`, `button`, `calibrate`) y arma los salientes (`haptic`, `state`).
- `RoomQrLoader`: baja el QR de `GET /qr/<código>` y lo pone en un `RawImage`.
- `PadInputHub` (`Gestures/Unity`): un reconocedor por mando. De aquí leen
  menús y minijuegos. Usa `Assets/Settings/Gestures/GestureSettings.asset`,
  que se edita en el Inspector incluso durante el Play.

**Escena `Scenes/Diagnostico`**, para probar con el celular real y afinar
umbrales:

1. `cd server && npm run dev` (necesita el canal `/unity` de RF-04).
2. Abrir `Diagnostico` y dar Play. Aparecen el código y el QR.
3. Escanear con el celular, calibrar y mover: se ven los Hz que llegan, la
   inclinación en X/Y, si está estable y cada gesto detectado.

La URL del servidor se cambia en el componente `ServerConnection`
(`ws://localhost:3443/unity` por defecto; `wss://` si el servidor tiene
certificados de mkcert).

## Gestos (`Scripts/Gestures`, RF-05, RF-06)

C# puro (`noEngineReferences`): no depende de UnityEngine ni de la red, y se
prueba sin abrir escenas (`Gestures/Tests`, Test Runner → EditMode).

```csharp
var settings = new GestureSettings();              // umbrales; editables en vivo
var pad = new GestureRecognizer(slot: 1, settings);
pad.GestureDetected += e => Debug.Log(e);          // TiltLeft/Right/Forward/Back, Swing, Shake, Steady
pad.Process(sample);                               // por cada mensaje motion
pad.Calibrate();                                   // mensaje calibrate o botón recenter
float x = pad.Tilt.X;                              // -1..1, para mecánicas continuas
```

Supuesto de postura: celular en vertical, pantalla arriba e inclinado hacia el
jugador. Si un eje sale al revés en la prueba real, se corrige con `InvertX` /
`InvertY`, sin tocar código.

## Reglas

- Un minijuego **consume gestos ya reconocidos**; nunca lee sensores crudos (RNF-08).
- Agregar un minijuego = una carpeta nueva en `Games/` + registro en el menú.
  Si hace falta tocar `Net/` o `Gestures/`, el diseño está mal.
- Escenas y prefabs no se fusionan solos: avisar antes de editar una escena que
  otro esté tocando.
- Mantener 60 fps; 30 es el piso (RNF-02).
- Texto grande y legible desde el sillón: el juego se ve en TV, no en monitor.

Contrato de mensajes: [../docs/protocolo-ws.md](../docs/protocolo-ws.md)
