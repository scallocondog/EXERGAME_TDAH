# CLAUDE.md — MoviMente (Equipo Kinesis)

Guía operativa del proyecto. Todo el equipo y cualquier agente de IA que trabaje
en este repositorio debe seguir lo que está aquí. Si algo de este archivo choca
con una decisión nueva, primero se actualiza este archivo y luego se programa.

---

## 1. Qué es MoviMente

Colección de minijuegos tipo Wii Play para **niños de 8 a 12 años con TDAH**.
El **celular es el mando de movimiento** desde el navegador, sin instalar nada.
El juego corre en **Unity sobre una PC** conectada a TV/monitor/proyector.

Flujo: el celular abre una página web → lee acelerómetro y giroscopio → envía
por **WebSocket** a un **servidor Node.js** → el servidor retransmite al **juego
Unity** → el juego responde en la pantalla grande.

**Uso libre:** no hay login, no hay adulto operando la sesión. El jugador abre el
juego, escanea un QR y juega.

**Fuera de alcance (no se implementa, no se agrega, no se insinúa):** diagnóstico
o evaluación clínica del TDAH, cuentas de usuario, almacenamiento de datos
personales.

Fuente de verdad de requisitos: [docs/requisitos-y-casos-de-uso.md](docs/requisitos-y-casos-de-uso.md)

---

## 2. Equipo y propiedad del código

| Integrante | Rol | Carpetas que le pertenecen | Requisitos a cargo |
| --- | --- | --- | --- |
| **Santiago Callocondo** | Front e interfaces | `controller/`, `unity/Assets/UI/`, `docs/ux/` | RF-02, RF-03, RF-05 (lado mando), RF-08 (interfaz), RF-10, RF-16, RF-18; RNF-05, RNF-06 |
| **Piero Mejía** | Unity y back | `server/src/rooms/`, `server/src/relay/`, `unity/Assets/Scripts/` (red, gestos, minijuegos) | RF-01, RF-04, RF-06, RF-07, RF-09, RF-11 a RF-14, RF-19, RF-20; RNF-02, RNF-08 |
| **Misael Marrón** | Back y QA | `server/src/validation/`, `server/certs/`, `unity/Assets/Scripts/Metrics/`, `tests/` | RF-15, RF-17; RNF-01, RNF-03, RNF-04, RNF-07, RNF-09 |

**Regla de propiedad:** nadie edita directamente la carpeta de otro. Si necesitas
un cambio fuera de tu zona, abres un issue o lo pides en el PR y lo hace el
dueño. Excepción: `docs/` y los archivos de configuración de la raíz son
compartidos, pero el cambio se avisa en el PR.

Detalle ampliado: [docs/equipo-y-responsabilidades.md](docs/equipo-y-responsabilidades.md)

---

## 3. Arquitectura y estructura del repo

```
MoviMente/
├── controller/          Página web del mando (HTML/CSS/JS vanilla, sin framework)
│   ├── public/          index.html, estilos, iconos, audio
│   └── src/             sensores, conexión WS, calibración, vibración, UI del mando
├── server/              Node.js + Socket.io (salas, emparejamiento, relay, validación)
│   ├── src/             rooms/, relay/, validation/
│   └── certs/           certificados locales HTTPS (NO se commitean)
├── unity/               Proyecto Unity (cliente WS, gestos, minijuegos, UI, métricas)
├── docs/                Requisitos, arquitectura, protocolo, UX, plan de pruebas
├── tests/               Pruebas de latencia, reconexión, compatibilidad
└── .github/             Plantillas de PR e issues
```

**Regla de independencia (RNF-08):** agregar un minijuego nuevo **no puede**
obligar a tocar el módulo de conexión ni el de reconocimiento de gestos. Un
minijuego consume gestos ya reconocidos; nunca lee sensores crudos.

Capas y diagrama: [docs/arquitectura.md](docs/arquitectura.md)

Postura del mando, calibración y gestos por minijuego:
[docs/gestos.md](docs/gestos.md). Swing y sacudir nunca van en el mismo
minijuego.

---

## 4. Stack y versiones

| Parte | Tecnología | Nota |
| --- | --- | --- |
| Mando | HTML + CSS + JS vanilla (ES2022) | Sin framework. `DeviceOrientationEvent` / `DeviceMotionEvent`. |
| Servidor | Node.js 20+ LTS, Express, Socket.io 4.x | HTTPS obligatorio: sin él el navegador no entrega los sensores. |
| Juego | Unity 2022 LTS, C#, cliente WebSocket (`NativeWebSocket` o equivalente) | Build para Windows. |
| QR | Generado por el servidor | Apunta a `https://<ip-lan>:<puerto>/?room=<código>`. |

No se agregan dependencias sin acuerdo del equipo. Cada dependencia nueva se
justifica en el PR: qué resuelve y por qué no se hace a mano.

---

## 5. Contrato entre módulos (lo que nadie cambia por su cuenta)

El protocolo WebSocket es la frontera entre los tres. **Modificarlo exige acuerdo
de los tres integrantes y actualizar** [docs/protocolo-ws.md](docs/protocolo-ws.md)
**en el mismo PR.**

```jsonc
// Mando → Servidor → Unity, flujo continuo (30–60 Hz)
{ "t": "motion", "room": "A7K2", "seq": 128, "ts": 1737590000123,
  "ori": { "alpha": 12.4, "beta": -3.1, "gamma": 45.0 },
  "acc": { "x": 0.12, "y": 9.71, "z": 0.03 } }

// Mando → Servidor → Unity, eventos discretos
{ "t": "button", "room": "A7K2", "action": "confirm" }   // confirm | recenter | pause
{ "t": "calibrate", "room": "A7K2" }

// Unity → Servidor → Mando
{ "t": "haptic", "pattern": "hit" }                      // hit | miss
{ "t": "state", "value": "paused" }                      // playing | paused | disconnected
```

Reglas duras:

- El **mando no decide nada del juego**: solo reporta sensores y botones.
- El **servidor no interpreta gestos**: valida, empareja y retransmite (RF-04).
- El **reconocimiento de gestos vive solo en Unity** (RF-06).
- Los mensajes malformados o fuera de rango se descartan en el servidor y se
  registran; nunca llegan a Unity.

---

## 6. Flujo de trabajo Git

**Ramas**

- `main` — siempre funcional y demostrable. Nadie hace push directo.
- `dev` — integración; ahí entran los PR de features.
- `feat/RF-04-envio-sensores`, `fix/RF-07-reconexion`, `docs/protocolo-ws` — una
  rama por tarea, **siempre con el ID del requisito**.

**Commits (Conventional Commits + ID de requisito)**

```
feat(RF-04): enviar orientacion y aceleracion a 60 Hz
fix(RF-07): reconectar el mando tras perdida de senal
docs(CU-06): detallar flujo alterno de pausa
test(RNF-01): medir latencia gesto-pantalla en LAN
```

Tipos: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`. Mensajes en español,
en infinitivo, sin punto final.

**Pull Requests**

- Un PR por requisito o caso de uso; nada de PR gigantes.
- Título con el ID: `feat(RF-11): minijuego Atrapa lo correcto`.
- El cuerpo dice qué requisito cierra, cómo se probó, y lleva captura o video si
  toca UI.
- **Mínimo una aprobación** de otro integrante antes de mergear.
- Si toca el protocolo WS: aprobación de los tres.
- Se mergea con squash para que el historial de `dev` quede legible.

**Nunca se commitea:** `node_modules/`, `Library/`, `Temp/`, builds de Unity,
certificados (`server/certs/*.pem`), `.env`, puntajes locales, IPs personales.

---

## 7. Convenciones de código

**JavaScript (mando y servidor)**

- `camelCase` para variables y funciones, `PascalCase` para clases,
  `UPPER_SNAKE_CASE` para constantes.
- Módulos ES (`import` / `export`), un módulo por responsabilidad.
- Nada de `var`. Nada de lógica de negocio dentro de los handlers del DOM.
- Vocabulario de dominio consistente dentro de cada archivo.

**C# (Unity)**

- `PascalCase` para clases y métodos públicos, `camelCase` para privados y
  locales.
- Un `MonoBehaviour` por comportamiento; nada de scripts de 500 líneas.
- Los minijuegos heredan de una base común y solo implementan sus reglas.
- Escenas: `MainMenu`, `Game_AtrapaLoCorrecto`, `Game_SigueLaSecuencia`,
  `Game_CortaSinFallar`, `Game_Equilibrio`.

**General**

- Comentarios solo donde el porqué no es evidente; el código explica el qué.
- Nada de código muerto ni comentado "por si acaso": para eso está git.
- Archivos y carpetas en `kebab-case`, salvo dentro de Unity, que usa
  `PascalCase`.

---

## 8. Trazabilidad: requisito ↔ código ↔ prueba

Cada pieza de código que implementa un requisito lleva el ID en un comentario de
cabecera:

```js
// RF-05: calibración de posición neutra y recentrado
```

Cada caso de uso tiene su prueba en `tests/` con el mismo ID. Antes de decir
"terminado" se verifica lo mismo tres veces: existe el código, existe la prueba,
y el documento de requisitos sigue diciendo lo que el código hace.

Tabla viva de avance: [docs/trazabilidad.md](docs/trazabilidad.md)

---

## 9. Criterios de aceptación no negociables

| ID | Criterio | Cómo se mide |
| --- | --- | --- |
| RNF-01 | < 100 ms entre el gesto y la respuesta en pantalla, misma red Wi-Fi | Timestamp del mando contra el frame de reacción en Unity |
| RNF-02 | 60 fps en laptop promedio, 30 fps como mínimo | Profiler de Unity en la escena más pesada |
| RNF-03 | Funciona en Chrome Android y Safari iOS recientes | Prueba manual en al menos un dispositivo de cada uno |
| RNF-04 | HTTPS, sin login, sin datos personales, solo puntajes locales | Revisión en el PR e inspección del tráfico |
| RNF-07 | Una desconexión no pierde la partida | Apagar el Wi-Fi del celular a mitad de partida y reconectar |

Un PR que rompa cualquiera de estos no se mergea.

---

## 10. Reglas de diseño TDAH (aplican a todo lo que se escriba)

No es "detalle de UI": es el motivo del proyecto (RNF-05, RNF-06).

- Partidas de **3 a 5 minutos**. Nada más largo.
- **Una sola instrucción a la vez** en pantalla.
- Pantallas **sin distractores**: nada animado que no sirva al juego.
- Colores y sonidos **no agresivos**; sin alarmas ni parpadeos fuertes.
- **Refuerzo positivo inmediato** en el acierto; el error se corrige, no se
  castiga.
- **Cero penalizaciones humillantes**: nada de "perdiste" ni "fallaste otra vez".
- El jugador **no necesita leer para jugar**: texto grande, íconos y audio.
- Toda métrica es **de desempeño en el juego**, nunca una señal clínica (RNF-09).

---

## 11. Comandos frecuentes

```bash
# Servidor
cd server && npm install
npm run dev                 # HTTPS local + Socket.io; imprime la URL y el QR de la LAN

# Mando (lo sirve el servidor; esto es solo para editar en caliente)
cd controller && npm run dev

# Pruebas
npm test                    # servidor y validación de mensajes
npm run test:latency        # RNF-01

# Unity
# Abrir unity/ desde Unity Hub con 2022 LTS. No abrir con otra versión.
```

Para probar en celular: PC y celular en la **misma red Wi-Fi**, y aceptar el
certificado autofirmado la primera vez.

---

## 12. Qué NO hacer

- No agregar login, registro ni nada que identifique al jugador.
- No enviar datos a servicios externos ni analítica de terceros.
- No presentar ninguna métrica como diagnóstico, evaluación o "nivel de TDAH".
- No meter lógica de juego en el mando ni en el servidor.
- No tocar el protocolo WS sin acuerdo de los tres.
- No subir `node_modules/`, `Library/`, builds, certificados ni `.env`.
- No hacer push directo a `main`.

---

## 13. Directrices para Claude Code en este repo

- **Responder en español.** Commits, comentarios y documentación, también.
- Antes de codificar, identificar el **ID de requisito** (RF-xx / RNF-xx / CU-xx)
  y nombrarlo en el commit y en el comentario de cabecera.
- Respetar la **propiedad de carpetas** de la sección 2: si el cambio cae en zona
  ajena, decirlo explícitamente en vez de hacerlo en silencio.
- No introducir dependencias, frameworks ni servicios nuevos sin preguntar.
- No modificar `docs/requisitos-y-casos-de-uso.md` salvo pedido explícito: es el
  contrato con el curso.
- Al terminar, decir **qué requisito quedó cubierto y qué falta**, sin dar por
  probado lo que no se probó.
- Si un cambio afecta el protocolo WS, marcarlo como cambio que necesita acuerdo
  del equipo antes de implementarlo.
- Mantener este archivo actualizado cuando cambie una regla del proyecto.
