# Sistema visual — pantallas del juego

Responsable: **Santiago Callocondo**. Requisitos: RF-08 (interfaz), RF-10,
RF-16; RNF-05, RNF-06.

Esto es lo que hay que construir en Unity UI. La maqueta navegable está en
[maqueta/](maqueta/): se abre con doble clic en `index.html`, no necesita
servidor ni Unity. `index.html#menu` abre directo en una pantalla (también
`#pausa` y `#sin-mando`) y `?tema=claro` elige el tema.

El estilo sale del diseño **"MoviMente TV"** hecho en Claude Design (frames de
1920 × 1080). Tiene dos direcciones con los mismos tokens: **B oscuro**, la
recomendada y la que se construye, y **A claro**, tipo Wii U, que se ve mejor con
proyector en una sala muy iluminada. En la maqueta se cambia con la tecla T. Los
íconos del diseño están en [iconos/](iconos/).

---

## 1. La pantalla es una TV, no un monitor

Todo se decide desde ahí: el jugador está a **2 o 3 metros**, de pie, con el
celular en la mano y mirando al frente.

| Regla | Valor |
| --- | --- |
| Resolución de diseño | 1920 × 1080 |
| Zona segura | 96 px a los lados, 54 px arriba y abajo (las TV recortan el borde) |
| Texto más chico permitido | 32 px |
| Contraste mínimo | 4.5:1; los textos de apoyo también |
| Elementos por pantalla | Los menos posibles; una sola decisión a la vez |

En Unity: `Canvas Scaler` → *Scale With Screen Size*, referencia 1920×1080,
*Match* 0.5. Los tamaños de este documento son en px de esa referencia.

---

## 2. Color

Parte de la misma base petróleo que el mando (`controller/public/styles.css`):
mando y TV tienen que sentirse una sola cosa. Los valores son los de B; la
columna A es la de las superficies que cambian en el tema claro.

| Token | B oscuro | A claro | Para qué |
| --- | --- | --- | --- |
| `text` | `#f3f8f9` | `#22343c` | Títulos y texto principal |
| `text-soft` | `#a9c3cc` | `#4d6470` | Texto de apoyo |
| `card` | `#1b4250` | `#ffffff` | Tarjetas, píldoras y fichas |
| `card-edge` | `#2a5b6c` | `#d6e2e8` | Borde de 4 px de las tarjetas |
| `card-drop` | `#0b222a` | `#cddbe2` | Sombra sólida de 10 px hacia abajo |
| `card-focus` | `#22566a` | `#f2fbff` | Fondo de la tarjeta con foco |
| `focus` | `#1eb0f5` | `#0b86c7` | Borde del foco |
| `focus-halo` | `#1eb0f5` al 35 % | `#1eb0f5` al 40 % | Halo del foco |
| `bar` | `#0e2730` | `#ffffff` | Barra inferior y caja de la demo |
| `panel` | `#163a47` | `#ffffff` | Panel de pausa y de mando desconectado |
| `dim` | `#081820` al 78 % | perla al 82 % | Capa que atenúa el juego detrás de la pausa |

Iguales en los dos temas:

| Token | Valor | Para qué |
| --- | --- | --- |
| `pad1` | `#1eb0f5` (texto `#06222e`) | Mando 1 y el "¡Jugar!" de la tarjeta con foco |
| `pad2` | `#f08cbf` (texto `#3a0f24`) | Mando 2 (RF-20) |
| `action` | `#6fdcb6` (texto `#12302a`) | Acción principal: empezar, jugar otra vez |
| `hit` | `#58cfa6` | Acierto, "+10", racha |
| `star` | `#efc05f` (texto `#3a2a06`) | Estrellas, récord, tiempo por acabarse |
| `miss` | `#e79a86` al 50 % | Borde del error |
| `time-fill` | `#7fd3f7` (A: `#1eb0f5`) | Barra de tiempo del HUD |

**El foco es celeste, no verde.** El celeste contrasta 5,4:1 sobre petróleo; en
A apenas llega a 2,3:1 sobre perla, por eso ahí el borde se oscurece a
`#0b86c7` y el celeste queda como halo. El verde queda para la acción y el
acierto.

**Texto sobre color.** Sobre el verde de acción el texto es `#12302a`, porque el
blanco no alcanza contraste. Los nombres de juego nunca van en blanco sobre el
color del juego.

**El error no es rojo.** Un rojo de alarma en pantalla completa es exactamente
la penalización humillante que prohíbe RNF-05. El error usa el coral apagado, a
la mitad de intensidad que el acierto, y dura menos.

Fondo quieto: en B, un degradado radial suave de `#1a4452` a `#12303b` en los
menús y uno vertical de `#173d4a` a `#12303b` en la partida; en A, franjas
horizontales tenues de 6 px, como el menú de Wii. Sin texturas y sin nada que se
mueva detrás del contenido.

---

## 3. Tipografía

Una sola familia: **Nunito** en 700 y 800 (en Unity, TextMeshPro). Nada de pesos
finos: a 3 metros desaparecen.

| Uso | Tamaño | Peso |
| --- | --- | --- |
| Número de la cuenta regresiva | 240 px | 800 |
| Código de sala (una ficha por letra) | 160 px | 800 |
| Puntaje final | 144 px | 800 |
| Título de pantalla | 72 px | 800 |
| Puntaje del HUD | 64 px | 800 |
| Instrucción principal | 56 px | 800 |
| Nombre de dificultad | 52 px | 800 |
| Nombre de juego, opciones y botones | 44 px | 800 |
| Texto de apoyo | 36–44 px | 700 |
| Píldoras de la barra superior | 32 px | 800 |
| Mínimo absoluto | 32 px | 700 |

Frases cortas, en segunda persona y en positivo: "Inclina para mover la
canasta", no "El jugador deberá inclinar el dispositivo".

---

## 4. El foco (RF-08)

No hay cursor ni hover: el jugador mueve el foco inclinando el celular y
confirma con el botón.

El foco se marca con **varias señales a la vez**, nunca solo con color:

1. Borde de 8 px en `focus` (las demás tarjetas tienen 4 px en `card-edge`).
2. Halo de 14 px en `focus-halo` alrededor.
3. Fondo `card-focus`.
4. Escala 1.07 en las tarjetas y 1.06 en botones y opciones.
5. En las tarjetas de juego y de dificultad aparece la píldora "¡Jugar!" con el
   ícono del botón, como el "Start" de Wii U.

Reglas de navegación, para quien lo implemente en Unity:

- Inclinar mueve **una opción por vez**, con un enfriamiento de **350 ms**: si no,
  el foco cruza el menú entero de un golpe.
- Umbral de entrada 20°, de salida 10° (los mismos de `GestureSettings`), para
  que el foco no vibre entre dos opciones.
- **Confirmar es siempre el botón del mando.** Nunca por permanencia: un niño
  con TDAH deja el foco encima de cosas que no quiere elegir.
- El foco nunca se pierde: al entrar a una pantalla hay uno puesto, y en los
  extremos se queda quieto en vez de dar la vuelta.
- Dónde empieza: en Dificultad, en la última elegida; en Instrucciones, en
  "empezar" y no en el audio; en la pausa, en "Seguir jugando".
- Los menús los maneja el Mando 1.
- Al mover el foco, el mando vibra `tap`. **Lo manda Unity**, con
  `{ "t": "haptic", "slot": n, "pattern": "tap" }`: el mando no sabe cuándo
  cambió el foco, solo manda ángulos. El patrón ya existe en `haptics.js`.

---

## 5. Movimiento

| Transición | Duración |
| --- | --- |
| Cambio de foco | 120 ms |
| Entrar o salir de una pantalla, abrir un panel | 220 ms, fundido y 24 px hacia arriba |
| Cada número de la cuenta | 220 ms, escala de 0,9 a 1 y fundido; sin "¡YA!" con destello |
| Acierto: borde verde de 16 px | 200 ms, entra y sale |
| Acierto: "+10" | 600 ms, sube 60 px y se desvanece |
| Acierto: "3 seguidos" | 900 ms; aparece desde 3 aciertos seguidos |
| Acierto: puntaje | 200 ms contando hacia arriba |
| Error: borde coral de 10 px al 50 % | 150 ms |
| Error: el objeto | 150 ms, se desvanece sin caer ni rebotar |
| Aparición de cada estrella | 300 ms, una tras otra |
| Demostración del gesto | 2,4 s en bucle: el celular ±18° y la canasta ±170 px |

Lo único que se anima en bucle es la **demostración del gesto** en la pantalla de
instrucciones. Todo lo demás se queda quieto cuando termina de entrar: RNF-05
pide pantallas sin distractores y una animación en bucle es un distractor.

---

## 6. Las pantallas

| # | Pantalla | Requisito | Qué decide el jugador |
| --- | --- | --- | --- |
| 1 | Sala y QR | RF-01 | Nada: conecta el celular |
| 2 | Elegir juego | RF-08, RF-09 | Cuál de los cuatro |
| 3 | Elegir dificultad | RF-09 | Fácil, medio o difícil |
| 4 | Instrucciones | RF-10 | Cuándo empezar |
| 5 | Cuenta regresiva | CU-06 | Nada |
| 6 | Partida y HUD | RF-15 | Jugar |
| 7 | Pausa | RF-19 | Seguir, reiniciar o salir |
| 8 | Resultados | RF-16, RF-17 | Repetir o cambiar de juego |
| 9 | Mando desconectado | RF-07, CU-08 | Nada: el juego espera |

Pausa y mando desconectado son **capas encima de la partida**, no pantallas
aparte: el juego se ve atenuado detrás para no perder el contexto.

### Barras de los menús (pantallas 2 y 3)

Arriba, a la izquierda, el logo o el juego elegido (ícono y nombre); a la
derecha, una píldora por mando y el indicador de música. La música y la pausa se
controlan desde el celular, así que la píldora de música es solo un indicador y
no una opción más. El Mando 2 se une en cualquier momento con el mismo código:
mientras no está, su píldora va punteada y muestra el código ("Mando 2 · TCXG").

Abajo, una barra de 150 px con cómo se usa el mando: "Inclina para elegir ·
Botón para entrar", cada frase con su ícono.

### Pantalla 1 — Sala y QR

QR de 512 px en una tarjeta blanca a la izquierda. A la derecha, el título
"Conecta tu celular" con el ícono de audio, el código en cuatro fichas de 160 px
y una sola frase: "Escanea el código con la cámara". Debajo, el estado del mando:
"Esperando mando…" en punteado → "Mando 1 conectado" en celeste con un ✓. Nada
más en pantalla.

### Pantalla 2 — Elegir juego

Cuatro tarjetas de 720 × 280 en dos filas, cada una con el ícono del juego a
224 px y su nombre.

### Pantalla 3 — Dificultad

Tres tarjetas de 480 × 540. La velocidad se lee sin texto: 1, 2 o 3 flechas y el
color (verde, amarillo, violeta).

### Pantalla 4 — Instrucciones (RF-10)

La más importante y la más fácil de arruinar. Lleva exactamente esto:

1. Una frase de una línea con el gesto.
2. Al lado, el botón de audio, que repite la locución (RNF-06).
3. La demostración del gesto, en bucle y sin texto encima: el celular del
   Mando 1 se inclina y la canasta lo sigue.
4. Un botón: "Toca el botón para empezar".

Hay dos opciones con foco, audio y empezar, y el foco empieza en empezar. Sin
párrafos, sin lista de reglas, sin puntajes. Si no se entiende sin leer, está
mal hecha.

### Pantalla 5 — Cuenta regresiva

El número de 240 px en un círculo, sobre el juego atenuado, y debajo la frase
del gesto con su ícono. La canasta ya se puede mover para practicar.

### Pantalla 6 — HUD

Solo dos datos: **tiempo** arriba a la izquierda (reloj, barra y número) y
**puntaje** arriba a la derecha (estrella y número), en píldoras semitransparentes
sobre el juego. Nada de contadores de errores ni de vidas en pantalla.

En Atrapa lo correcto hay además un panel a la izquierda con qué atrapar: la
palabra "Atrapa", el dibujo del objeto, su nombre y el ícono de audio. No es un
dato de desempeño: es la consigna, dicha sin depender de leer. Si juegan dos, el
panel lleva el color del mando.

Retroalimentación:

- **Acierto:** `+10` que sube y se desvanece sobre el objeto, borde de pantalla
  en `accent` 200 ms, vibración `hit`. La racha aparece en chico y en positivo
  ("3 seguidos").
- **Error:** el objeto se desvanece, borde en `miss` más fino y más corto que el
  del acierto, vibración `miss`. **Sin texto**, sin "fallaste", sin sonido de
  bocina, sin sacudir la pantalla, y el puntaje no baja.

### Pantalla 7 — Pausa (RF-19)

Se abre con el botón de pausa del celular. Panel con "Juego en pausa" y tres
opciones, cada una con su ícono: "Seguir jugando", "Volver a empezar" y "Salir",
con la nota "no se guarda el puntaje" debajo de Salir.

### Pantalla 8 — Resultados (RF-16)

Tres estrellas grandes (la del medio más grande y más alta) que aparecen de a
una, "¡Muy bien!", el puntaje de 144 px y la palabra "puntos". Siempre hay al
menos una estrella. Si superó su récord, la píldora amarilla "¡Nuevo récord!".
Debajo, en chico y en tono neutro: aciertos y tiempo de reacción. Dos botones:
"Jugar otra vez" (verde, con el foco) y "Elegir otro juego".

**Decisión que conviene discutir:** RF-15 manda calcular errores y omisiones,
pero esta pantalla no los muestra. Calcularlos es una cosa y ponerle a un niño
"12 errores" en la TV es otra; RNF-05 prohíbe la penalización humillante. Las
métricas completas quedan guardadas para el equipo (`Metrics/`), no para el
jugador. Si el curso exige mostrarlas, se ponen fuera de la pantalla del niño.

### Pantalla 9 — Mando desconectado

El ícono del celular en un círculo punteado, con el número del mando que falta;
"Esperando tu mando…" y una línea de qué hacer. Sin alarma, sin cuenta atrás, sin
opciones y sin amenaza de perder la partida: RNF-07 garantiza que no se pierde.
El juego se congela; al reconectar, la capa se va y vuelve la pausa, así el niño
decide cuándo seguir.

---

## 7. Audio (RNF-06)

El jugador no necesita leer. Cada pantalla tiene su locución; la voz dice lo
mismo que el texto grande, ni más ni menos.

| Pantalla | Locución |
| --- | --- |
| Sala | "Escanea el código con la cámara de tu celular" |
| Elegir juego | "Elige un juego inclinando el celular" |
| Dificultad | "¿Qué tan rápido quieres jugar?" |
| Instrucciones | La frase del gesto, tal cual |
| Cuenta regresiva | "Tres, dos, uno, ¡ya!" |
| Acierto | Tono corto y agradable, sin palabras |
| Error | Tono neutro, más bajo que el de acierto |
| Resultados | "¡Muy bien! Conseguiste [n] estrellas" |
| Sin mando | "Esperando tu mando" |

Volumen parejo entre pantallas y ningún sonido que sobresalte (RNF-05).

---

## 8. Assets

### Íconos

[iconos/](iconos/), en SVG, del diseño "MoviMente TV".

| Archivo | Qué es |
| --- | --- |
| `juego-atrapa.svg`, `juego-secuencia.svg`, `juego-corta.svg`, `juego-equilibrio.svg` | Ícono de cada juego, con su color (naranja, azul, violeta, verde) |
| `dificultad-facil.svg`, `dificultad-medio.svg`, `dificultad-dificil.svg` | 1, 2 y 3 flechas sobre verde, amarillo y violeta |
| `estrella.svg` | Estrella de resultados y del puntaje del HUD |
| `ui-*.svg` | Íconos de interfaz de un color (audio, botón, celular, check, estrella, inclinar, jugar, música, música apagada, pausa, reiniciar, reloj, salir) |

Los `ui-*` son blancos para teñirlos con el color del tema: en Unity, con el
tinte de la imagen. La maqueta los lleva copiados en línea en `index.html` con
`currentColor`, porque las máscaras CSS con archivos no cargan al abrirla con
doble clic.

### Modelos de Atrapa lo correcto (RF-11)

`unity/Assets/Art/AtrapaLoCorrecto/Modelos/`, del [Food Kit 2.0 de
Kenney](https://kenney.nl/assets/food-kit), licencia CC0 (`License.txt` al lado).

| Categoría | Modelo | Color | Silueta |
| --- | --- | --- | --- |
| 0 | `Naranja.fbx` | naranja | redonda |
| 1 | `Pez.fbx` | azul grisáceo | alargada |
| 2 | `Uvas.fbx` | lila | racimo |
| Canasta | `Canasta.fbx` | blanca | cuenco |

Cada categoría se distingue por la silueta **y** por el color, nunca solo por el
color (RNF-06). Los cuatro comparten la textura `Textures/colormap.png`.

La escena no usa los FBX directo sino los prefabs de `AtrapaLoCorrecto/Prefabs/`,
que les ponen la orientación: el pez viene de fábrica con el largo hacia la
cámara y va girado 90° para verse de costado. Si un modelo se ve mal desde la
TV, se corrige en su prefab, sin tocar código. `AtrapaView` los centra y los
lleva al mismo tamaño que las formas primitivas, que siguen de respaldo si falta
algún modelo.

### Audio (RNF-06)

`unity/Assets/Audio/`. Todo en WAV mono de 44,1 kHz.

| Archivo | Qué es | Nivel |
| --- | --- | --- |
| `Efectos/Acierto.wav` | Dos notas que suben (Do6 → Mi6), 0,34 s | −22 dB RMS |
| `Efectos/Error.wav` | Una nota grave y quieta (Re4), 0,17 s: la mitad que el acierto | −28 dB RMS |
| `Voces/*.wav` | Las locuciones de la tabla de §7 | −18 dB RMS, todas iguales |
| `Voces/CuentaRegresiva.wav` | "Tres", "dos", "uno", una palabra por segundo | igual que las voces |

**Las voces son temporales:** salen de la voz sintética de Windows (Helena,
español de España) para poder probar ya. Se reemplazan por una grabación con los
mismos nombres de archivo y el mismo nivel.

Qué suena y cuándo lo decide `SessionSounds` (`unity/Assets/UI/Scripts/`), y lo
reproduce `SessionAudio`, que va dentro del prefab `CoreSystems`: así todo
minijuego tiene sonido sin agregar nada a su escena.

| Momento de la partida | Suena |
| --- | --- |
| Acierto / error | `Acierto` / `Error`. La omisión no suena, igual que no vibra |
| Empieza o se retoma la cuenta regresiva | `CuentaRegresiva` |
| Arranca el juego | `Ya` |
| Se cae el mando | `EsperandoTuMando` |
| Pausa del jugador | Nada |
| Termina la partida | `MuyBien` |

Una voz nueva corta a la anterior: nunca hablan dos a la vez (RNF-05).

---

## 9. Qué falta

- Grabar las locuciones y reemplazar las temporales de `Audio/Voces/`.
- Meter Nunito en Unity como asset de TextMeshPro.
- Importar los íconos en Unity tal cual, en SVG: Unity 6.3 trae el importador
  como módulo (`com.unity.modules.vectorgraphics`, ya está en el manifest).
  Revisar `ui-musica-off.svg`, el único que usa una máscara.
- Pasar estas pantallas a Unity UI.
- El diseño anuncia secciones de componentes, tokens y movimiento, Unity y mando
  que no vinieron en la exportación: pedirlas a Claude Design antes de tocar el
  mando.

---

## 10. Neon Saber: concepto, no aprobado

La maqueta trae además [maqueta/neon-saber.html](maqueta/neon-saber.html), un
concepto del mismo proyecto de Claude Design para un juego de cortar con sables.
Está copiado **tal cual, como referencia visual**, y **no se construye así**:

- Fondo animado (láseres que pulsan, rejilla y arcos que avanzan) y viñeta:
  distractores (RNF-05).
- Neón saturado rojo, rosa y cian con mucho brillo: no son colores calmados.
- Fallar reinicia el combo a 0 y baja el multiplicador a la mitad: castiga el
  error, cuando el error se corrige y no se castiga.
- "COMBO", "SCORE" y "AUTOPLAY" en inglés y a 12–13 px, bajo el mínimo de 32 px.
- Dos sables y cubos de un color por sable, cuando el mando es un solo celular;
  y no hay objetos prohibidos que evitar, que es lo que pide Corta sin fallar
  (RF-13).

Si se quiere usar su idea (pista en perspectiva, objetos que se acercan), hay
que rediseñarlo contra estas reglas y acordarlo con Piero, que tiene el
minijuego.
