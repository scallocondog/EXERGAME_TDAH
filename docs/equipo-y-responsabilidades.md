# Equipo y responsabilidades — Equipo Kinesis

Reparto acordado. Las carpetas indican quién es dueño del código: nadie edita la
zona de otro sin avisar (ver sección 2 de [CLAUDE.md](../CLAUDE.md)).

---

## Santiago Callocondo — Front e interfaces

**Responsabilidades**

- Página web del mando (HTML/CSS/JS): conexión por QR, permisos de sensores,
  calibración, botones de confirmar, recentrar y pausa, vibración.
- Diseño UI/UX en Figma de menús, instrucciones, HUD y resultados.
- Implementación de esas pantallas en Unity UI.

**Requisitos a cargo:** RF-02, RF-03, RF-05 (lado mando), RF-08 (interfaz),
RF-10, RF-16, RF-18; RNF-05, RNF-06.

**Carpetas:** `controller/`, `unity/Assets/UI/`, `docs/ux/`.

**Entregables**

- `controller/` funcionando en Android y iOS, con estados claros: sin permiso,
  conectando, conectado, desconectado.
- Figma con menús, pantalla de instrucciones (RF-10), HUD y resultados (RF-16).
- Pantallas de Unity armadas con esos diseños, legibles a distancia en TV.

---

## Piero Mejía — Unity y back

**Responsabilidades**

- Servidor Node.js + Socket.io: salas, emparejamiento, retransmisión.
- Cliente WebSocket en Unity.
- Reconocimiento de gestos.
- Escenas, mecánicas y lógica de los minijuegos.

**Requisitos a cargo:** RF-01, RF-04, RF-06, RF-07, RF-09, RF-11 a RF-14, RF-19,
RF-20; RNF-02, RNF-08.

**Carpetas:** `server/src/rooms/`, `server/src/relay/`,
`unity/Assets/Scripts/` (red, gestos, minijuegos).

**Entregables**

- Sala con código + QR al iniciar el juego (RF-01).
- Relay de sensores con latencia dentro de RNF-01.
- Reconocedor de los cinco gestos de RF-06, con umbrales ajustables sin recompilar.
- Los tres minijuegos obligatorios (RF-11, RF-12, RF-13) y el opcional (RF-14).

---

## Misael Marrón — Back y QA

**Responsabilidades**

- Validación de los datos del mando: rangos, frecuencia, mensajes malformados.
- Cálculo de métricas por partida y guardado local de puntajes.
- Configuración de HTTPS para la red local.
- Pruebas de latencia, reconexión y compatibilidad Android/iOS.
- Plan de pruebas por caso de uso.

**Requisitos a cargo:** RF-15, RF-17; RNF-01, RNF-03, RNF-04, RNF-07, RNF-09.

**Carpetas:** `server/src/validation/`, `server/certs/`,
`unity/Assets/Scripts/Metrics/`, `tests/`.

**Entregables**

- Capa de validación que descarta y registra todo mensaje inválido antes del relay.
- Métricas por partida de RF-15 y persistencia local de RF-17.
- HTTPS local documentado paso a paso para que cualquiera lo levante.
- [Plan de pruebas](plan-de-pruebas.md) con un caso por CU y las mediciones de RNF-01.

---

## Zonas compartidas

| Archivo o carpeta | Regla |
| --- | --- |
| `docs/protocolo-ws.md` | Cambios con acuerdo de los tres, en el mismo PR que el código |
| `docs/requisitos-y-casos-de-uso.md` | No se toca salvo cambio de alcance acordado |
| `CLAUDE.md`, `.gitignore`, configuración raíz | Se puede editar, pero se avisa en el PR |

## Ritmo de trabajo

- Integración en `dev`; `main` siempre debe poder demostrarse.
- Antes de cada entrega del curso: merge a `main`, tag y prueba completa en la
  PC de demostración con dos celulares (Android e iOS).
- Si alguien se bloquea más de un día en algo de otra zona, lo pasa al dueño en
  vez de arreglarlo por su cuenta.
