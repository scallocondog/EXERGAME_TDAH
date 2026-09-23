# unity — proyecto del juego

Dueños: **Piero Mejía** (`Scripts/Net`, `Scripts/Gestures`, `Scripts/Games`),
**Santiago Callocondo** (`Assets/UI`), **Misael Marrón** (`Scripts/Metrics`,
`Scripts/Storage`).

Unity **6000.3.24f1** (Unity 6.3 LTS, soporte hasta diciembre de 2027). No
abrir con otra versión, ni siquiera otro parche de 6000.3: Unity reescribe
archivos del proyecto y ensucia el diff para todos.

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

Postura acordada: celular en vertical como un Wiimote, pantalla arriba e
inclinada unos 30° hacia el jugador. Qué gestos usa cada minijuego y cómo se
calibra: [docs/gestos.md](../docs/gestos.md). Si un eje sale al revés en la
prueba real, se corrige con `InvertX` / `InvertY`, sin tocar código.

## Reglas

- Un minijuego **consume gestos ya reconocidos**; nunca lee sensores crudos (RNF-08).
- Agregar un minijuego = una carpeta nueva en `Games/` + registro en el menú.
  Si hace falta tocar `Net/` o `Gestures/`, el diseño está mal.
- Escenas y prefabs no se fusionan solos: avisar antes de editar una escena que
  otro esté tocando.
- Mantener 60 fps; 30 es el piso (RNF-02).
- Texto grande y legible desde el sillón: el juego se ve en TV, no en monitor.

Contrato de mensajes: [../docs/protocolo-ws.md](../docs/protocolo-ws.md)
