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
(`ws://127.0.0.1:3444/unity` por defecto: el puerto local del servidor, que no
usa certificado). Unity nunca desactiva la validación de certificados.

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

## Partida en Unity (`Scripts/Games/Unity`, RF-07, RF-18, RF-19)

- **`Prefabs/CoreSystems`**: conexión, gestos, QR y `GameRunner` en un objeto
  que sobrevive a los cambios de escena, para que la sala no se cierre al pasar
  del menú al minijuego. Cada escena lleva una copia para poder abrirse sola;
  si ya hay una viva, la copia se apaga antes de conectarse. Se accede con
  `CoreSystems.Instance`.
- **`GameRunner.Begin(definición, dificultad, slots)`** crea la `GameSession`
  y la hace avanzar cada cuadro. Además:
  - le pasa los gestos;
  - si llega `pad_state: disconnected`, pausa; al volver el mando, retoma;
  - manda `hit` o `miss` al mando del acierto o error (la omisión no vibra);
  - avisa al mando `playing` o `paused`.
- **Botones del mando:** `confirm` en instrucciones empieza la cuenta
  regresiva; `pause` pausa y, en la pausa, reanuda. Reintentar y salir los
  elige la UI de pausa (Santiago) llamando a `Session.Retry()` / `Quit()`.

**Escena `Scenes/Game_AtrapaLoCorrecto`** (RF-11), jugable con el celular:

1. `cd server && npm run dev`.
2. Abrir la escena y dar Play. Si falta el mando, el HUD muestra la sala y el QR.
3. Escanear, calibrar y tocar el botón del mando para empezar.

Las categorías son forma + color (esfera, cubo, cápsula) hasta que lleguen los
íconos definitivos. La categoría pedida se ve en el panel de arriba a la
izquierda desde la pantalla de instrucciones. El HUD (`Diagnostics/SessionDebugHud`)
es de prueba: lo reemplaza la UI de `Assets/UI/`.

## Reglas

- Un minijuego **consume gestos ya reconocidos**; nunca lee sensores crudos (RNF-08).
- Agregar un minijuego = una carpeta nueva en `Games/` + registro en el menú.
  Si hace falta tocar `Net/` o `Gestures/`, el diseño está mal.
- Escenas y prefabs no se fusionan solos: avisar antes de editar una escena que
  otro esté tocando.
- Mantener 60 fps; 30 es el piso (RNF-02).
- Texto grande y legible desde el sillón: el juego se ve en TV, no en monitor.

Contrato de mensajes: [../docs/protocolo-ws.md](../docs/protocolo-ws.md)
