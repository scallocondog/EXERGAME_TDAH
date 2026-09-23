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

## Base de minijuegos (`Scripts/Games/Core`, RF-09, RF-19, RNF-08)

C# puro, igual que Gestures. Un minijuego nuevo es una clase de reglas y un
registro en el catálogo:

```csharp
class AtrapaRules : MinigameRules
{
    public override void OnGesture(GestureEvent g) { /* ... */ Context.Report(TrialOutcome.Hit, g.Slot, 0.6f); }
    public override void OnTick(float dt) { float x = Context.GetTilt(1).X; /* mover la canasta */ }
}

catalog.Register(new MinigameDefinition("atrapa", "Game_AtrapaLoCorrecto", () => new AtrapaRules(),
    easy:   new DifficultyProfile(180f, GestureType.TiltLeft, GestureType.TiltRight),
    medium: new DifficultyProfile(240f, GestureType.TiltLeft, GestureType.TiltRight),
    hard:   new DifficultyProfile(300f, GestureType.TiltLeft, GestureType.TiltRight)));
```

El catálogo rechaza partidas de menos de 3 o más de 5 minutos (RNF-05) y
minijuegos que mezclen swing y sacudir.

Ejemplo real: `Games/AtrapaLoCorrecto` (RF-11). La canasta sigue a
`Tilt.X` por posición (inclinar del todo la lleva al borde); caen objetos de 3
categorías y solo cuentan los de la pedida. Atrapar uno pedido es acierto,
atrapar un distractor es error, dejar caer uno pedido es omisión y dejar pasar
un distractor no se reporta. El tiempo de reacción va desde que aparece el
objeto hasta que la canasta queda debajo. La escena recibe las reglas de cada
partida por `onRulesCreated` y solo dibuja `BasketX` e `Items`.

`GameSession` maneja lo común a todos: instrucciones → cuenta regresiva de 3 s →
juego → fin. Además:

- **Pausa (RF-19):** reanudar vuelve con cuenta regresiva; reintentar empieza
  de cero; salir deja la partida en `Abandoned` y `ShouldSaveScore` en `false`.
- **Desconexión (RF-07, CU-08):** `SetPadConnected(slot, false)` pausa y al
  reconectar retoma con el tiempo intacto. Una pausa del jugador no se reanuda
  sola al reconectar.
- **Filtro:** las reglas solo reciben gestos mientras se juega, de los mandos
  de la partida y del tipo que permite la dificultad.
- **Salidas:** `StateChanged` para la UI (Santiago) y `TrialReported`
  (acierto, error u omisión, con tiempo de reacción) para métricas (Misael) y
  vibración.

## Reglas

- Un minijuego **consume gestos ya reconocidos**; nunca lee sensores crudos (RNF-08).
- Agregar un minijuego = una carpeta nueva en `Games/` + registro en el menú.
  Si hace falta tocar `Net/` o `Gestures/`, el diseño está mal.
- Escenas y prefabs no se fusionan solos: avisar antes de editar una escena que
  otro esté tocando.
- Mantener 60 fps; 30 es el piso (RNF-02).
- Texto grande y legible desde el sillón: el juego se ve en TV, no en monitor.

Contrato de mensajes: [../docs/protocolo-ws.md](../docs/protocolo-ws.md)
