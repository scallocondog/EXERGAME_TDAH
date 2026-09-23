# Trazabilidad — requisito ↔ responsable ↔ código ↔ prueba

Tabla viva: se actualiza en el PR que implementa cada requisito.
Estado: ⬜ pendiente · 🟨 en progreso · ✅ hecho y probado.

## Requisitos funcionales

| ID | Requisito | Responsable | Dónde vive | Prueba | Estado |
| --- | --- | --- | --- | --- | --- |
| RF-01 | Sala con código y QR | Piero | `server/src/rooms/`, `server/src/index.js` | CU-01 (`relay.test.js`) | 🟨 servidor listo; falta mostrarlo en Unity |
| RF-02 | Conexión por QR sin instalar | Santiago | `controller/` | CU-02 | ⬜ |
| RF-03 | Permiso de sensores | Santiago | `controller/src/sensors.js` | CU-03 | ⬜ |
| RF-04 | Envío de orientación y aceleración | Piero | `controller/src/`, `server/src/relay/` | CU-02 (`relay.test.js`, `socketio-transport.test.js`) | 🟨 relay listo; faltan mando y validación |
| RF-05 | Calibración y recentrado | Santiago (mando) / Piero (Unity) | `controller/src/`, `unity/.../Gestures/` | CU-03, CU-07 (`GestureRecognizerTests`) | 🟨 lado Unity listo; falta mando |
| RF-06 | Reconocimiento de 5 gestos | Piero | `unity/.../Gestures/` | CU-06 (`GestureRecognizerTests`) | 🟨 probado con datos simulados; falta ajustar umbrales con celular real |
| RF-07 | Estado de conexión y reconexión | Piero | `unity/.../Net/`, `server/src/relay/` | CU-08 (`relay.test.js`) | 🟨 servidor listo; falta pausa en Unity |
| RF-08 | Menús navegados desde el mando | Santiago | `unity/Assets/UI/` | CU-04 | ⬜ |
| RF-09 | Elegir minijuego y dificultad | Piero | `unity/.../Games/` | CU-05 | ⬜ |
| RF-10 | Instrucciones visuales y de audio | Santiago | `unity/Assets/UI/` | CU-06 | ⬜ |
| RF-11 | Atrapa lo correcto | Piero | `unity/.../Games/AtrapaLoCorrecto/` | CU-06 | ⬜ |
| RF-12 | Sigue la secuencia | Piero | `unity/.../Games/SigueLaSecuencia/` | CU-06 | ⬜ |
| RF-13 | Corta sin fallar | Piero | `unity/.../Games/CortaSinFallar/` | CU-06 | ⬜ |
| RF-14 | Equilibrio (opcional) | Piero | `unity/.../Games/Equilibrio/` | CU-06 | ⬜ |
| RF-15 | Métricas por partida | Misael | `unity/.../Metrics/` | CU-06 | ⬜ |
| RF-16 | Resumen con puntaje y estrellas | Santiago | `unity/Assets/UI/` | CU-09 | ⬜ |
| RF-17 | Guardado local de puntajes | Misael | `unity/.../Storage/` | CU-09 | ⬜ |
| RF-18 | Vibración en aciertos y errores | Santiago | `controller/src/haptics.js` | CU-06 | ⬜ |
| RF-19 | Pausar, reanudar, reintentar, salir | Piero | `unity/.../Games/`, `controller/` | CU-06 | ⬜ |
| RF-20 | Dos mandos (deseable) | Piero | `server/src/rooms/`, `unity/.../Net/` | CU-02 (`room-registry.test.js`) | 🟨 slots 1 y 2 en servidor; falta Unity |

## Requisitos no funcionales

| ID | Requisito | Responsable | Cómo se verifica | Estado |
| --- | --- | --- | --- | --- |
| RNF-01 | < 100 ms de latencia | Misael | `npm run test:latency` + medición en LAN | ⬜ |
| RNF-02 | 60 fps (30 mínimo) | Piero | Unity Profiler en la escena más pesada | ⬜ |
| RNF-03 | Chrome Android y Safari iOS | Misael | Prueba manual en ambos dispositivos | ⬜ |
| RNF-04 | HTTPS, sin datos personales | Misael | Revisión de PR + inspección de red | ⬜ |
| RNF-05 | Usabilidad TDAH | Santiago | Checklist de la sección 10 de CLAUDE.md | ⬜ |
| RNF-06 | Accesibilidad sin leer | Santiago | Prueba con audio y sin texto | ⬜ |
| RNF-07 | Desconexión no pierde la partida | Misael | Apagar Wi-Fi a mitad de partida | ⬜ |
| RNF-08 | Minijuegos independientes | Piero | Agregar un minijuego de prueba sin tocar Net ni Gestures | ⬜ |
| RNF-09 | Alcance ético | Misael | Revisión de todo texto visible al jugador | ⬜ |
