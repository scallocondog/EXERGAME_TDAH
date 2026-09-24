# Trazabilidad — requisito ↔ responsable ↔ código ↔ prueba

Tabla viva: se actualiza en el PR que implementa cada requisito.
Estado: ⬜ pendiente · 🟨 en progreso · ✅ hecho y probado.

## Requisitos funcionales

| ID | Requisito | Responsable | Dónde vive | Prueba | Estado |
| --- | --- | --- | --- | --- | --- |
| RF-01 | Sala con código y QR | Piero | `server/src/rooms/`, `server/src/index.js`, `unity/.../Net/` | CU-01 (`relay.test.js`, `GameLinkTests`) | 🟨 servidor y Unity listos (QR en `RoomQrLoader`); falta la pantalla de sala de UI |
| RF-02 | Conexión por QR sin instalar | Santiago | `controller/public/` | CU-02 (`room-code.test.js`) | 🟨 mando listo; falta probarlo desde un celular |
| RF-03 | Permiso de sensores | Santiago | `controller/public/js/sensors.js` | CU-03 | 🟨 implementado; falta probar el permiso en un iPhone real |
| RF-04 | Envío de orientación y aceleración | Piero | `controller/public/js/`, `server/src/relay/`, `server/src/validation/` | CU-02 (`relay.test.js`, `socketio-transport.test.js`, `tests/validation/rf-04-*.test.js`) | 🟨 mando, relay y validación listos; falta probarlo desde un celular |
| RF-05 | Calibración y recentrado | Santiago (mando) / Piero (Unity) | `controller/public/js/calibration.js`, `unity/.../Gestures/` | CU-03, CU-07 (`calibration.test.js`, `GestureRecognizerTests`) | 🟨 las dos mitades listas; falta probarlas juntas |
| RF-06 | Reconocimiento de 5 gestos | Piero | `unity/.../Gestures/` | CU-06 (`GestureRecognizerTests`) | 🟨 probado con datos simulados; falta ajustar umbrales con celular real |
| RF-07 | Estado de conexión y reconexión | Piero | `unity/.../Net/`, `server/src/relay/`, `controller/public/js/connection.js` | CU-08 (`relay.test.js`, `GameLinkTests`) | 🟨 servidor, cliente Unity y mando listos; `GameRunner` pausa y retoma con `pad_state` (`SessionBridgeTests`); falta probarlo con celular |
| RF-08 | Menús navegados desde el mando | Santiago | `controller/public/js/main.js`, `docs/ux/`, `unity/Assets/UI/` | CU-04 | 🟨 mando y diseño listos; falta construirlos en Unity UI |
| RF-09 | Elegir minijuego y dificultad | Piero | `unity/.../Games/Core/` | CU-05 (`MinigameCatalogTests`) | 🟨 catálogo y perfiles listos; falta el menú en Unity |
| RF-10 | Instrucciones visuales y de audio | Santiago | `docs/ux/`, `unity/Assets/UI/` | CU-06 | 🟨 pantalla diseñada; faltan las locuciones y Unity UI |
| RF-11 | Atrapa lo correcto | Piero | `unity/.../Games/AtrapaLoCorrecto/` | CU-06 (`AtrapaLoCorrectoTests`, `AtrapaSceneTests`, `WanderingTests`) | 🟨 reglas, escena 3D y Wandering en difícil ([técnicas de movimiento](tecnicas-de-movimiento.md)); probada con celular; faltan los íconos definitivos |
| RF-12 | Sigue la secuencia | Piero | `unity/.../Games/SigueLaSecuencia/` | CU-06 | ⬜ |
| RF-13 | Corta sin fallar | Piero | `unity/.../Games/CortaSinFallar/` | CU-06 | ⬜ |
| RF-14 | Equilibrio (opcional) | Piero | `unity/.../Games/Equilibrio/` | CU-06 | ⬜ |
| RF-15 | Métricas por partida | Misael | `unity/.../Metrics/` | CU-06 | ⬜ |
| RF-16 | Resumen con puntaje y estrellas | Santiago | `docs/ux/`, `unity/Assets/UI/` | CU-09 | 🟨 pantalla diseñada; falta construirla en Unity UI |
| RF-17 | Guardado local de puntajes | Misael | `unity/.../Storage/` | CU-09 | ⬜ |
| RF-18 | Vibración en aciertos y errores | Santiago | `controller/public/js/haptics.js` | CU-06 | 🟨 el mando vibra al recibir `haptic`; `GameRunner` manda `hit`/`miss` en cada acierto o error; falta probarlo con celular |
| RF-19 | Pausar, reanudar, reintentar, salir | Piero | `unity/.../Games/Core/`, `controller/public/js/main.js` | CU-06 (`GameSessionTests`) | 🟨 lógica en `GameSession`, botón `pause` conectado en `GameRunner`; falta la UI de pausa (reintentar / salir) |
| RF-20 | Dos mandos (deseable) | Piero | `server/src/rooms/`, `unity/.../Net/` | CU-02 (`room-registry.test.js`) | 🟨 slots 1 y 2 en servidor y mando; falta Unity |

## Requisitos no funcionales

| ID | Requisito | Responsable | Cómo se verifica | Estado |
| --- | --- | --- | --- | --- |
| RNF-01 | < 100 ms de latencia | Misael | `npm run test:latency` + medición en LAN | ⬜ |
| RNF-02 | 60 fps (30 mínimo) | Piero | Unity Profiler en la escena más pesada | ⬜ |
| RNF-03 | Chrome Android y Safari iOS | Misael | Prueba manual en ambos dispositivos | ⬜ |
| RNF-04 | HTTPS, sin datos personales | Misael | Revisión de PR + inspección de red | ⬜ |
| RNF-05 | Usabilidad TDAH | Santiago | Checklist de la sección 10 de CLAUDE.md | 🟨 aplicado en el mando y en el diseño del juego |
| RNF-06 | Accesibilidad sin leer | Santiago | Prueba con audio y sin texto | 🟨 tamaños e íconos definidos; voces temporales y efectos en `unity/Assets/Audio/`, enganchados con `SessionAudio` (`SessionSoundsTests`); falta grabar las voces y oírlas en la partida |
| RNF-07 | Desconexión no pierde la partida | Misael | Apagar Wi-Fi a mitad de partida | ⬜ |
| RNF-08 | Minijuegos independientes | Piero | Agregar un minijuego de prueba sin tocar Net ni Gestures (`FakeRules` en `Games/Core/Tests`) | 🟨 base lista; se confirma con el primer minijuego real |
| RNF-09 | Alcance ético | Misael | Revisión de todo texto visible al jugador | ⬜ |
