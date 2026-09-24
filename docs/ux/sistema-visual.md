# Sistema visual — pantallas del juego

Responsable: **Santiago Callocondo**. Requisitos: RF-08 (interfaz), RF-10,
RF-16; RNF-05, RNF-06.

Esto es lo que hay que construir en Unity UI. La maqueta navegable está en
[maqueta/](maqueta/): se abre con doble clic en `index.html`, no necesita
servidor ni Unity.

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

Los mismos tokens que ya usa el mando (`controller/public/styles.css`): mando y
TV tienen que sentirse una sola cosa.

| Token | Valor | Para qué |
| --- | --- | --- |
| `bg` | `#12303b` | Fondo de todas las pantallas |
| `bg-soft` | `#1b4150` | Tarjetas y paneles |
| `bg-raised` | `#24596b` | La tarjeta que tiene el foco |
| `text` | `#f3f8f9` | Títulos y texto principal |
| `text-soft` | `#b6cbd2` | Texto de apoyo |
| `accent` | `#58cfa6` | Foco, acción, acierto |
| `warn` | `#efc05f` | Estrellas, tiempo por acabarse |
| `alert` | `#e79a86` | Error y desconexión |

**El error no es rojo.** Un rojo de alarma en pantalla completa es exactamente
la penalización humillante que prohíbe RNF-05. El error usa el coral apagado, a
la mitad de intensidad que el acierto, y dura menos.

Fondo siempre plano: sin degradados vivos, sin texturas, sin nada que se mueva
detrás del contenido.

---

## 3. Tipografía

Una sola familia, sans redondeada y pesada (en Unity, TextMeshPro con una fuente
tipo Nunito o Baloo). Nada de pesos finos: a 3 metros desaparecen.

| Uso | Tamaño | Peso |
| --- | --- | --- |
| Código de sala | 160 px | 800 |
| Número de la cuenta regresiva | 240 px | 800 |
| Título de pantalla | 72 px | 800 |
| Instrucción principal | 56 px | 700 |
| Nombre de tarjeta | 44 px | 700 |
| Puntaje del HUD | 64 px | 800 |
| Texto de apoyo | 36 px | 600 |
| Mínimo absoluto | 32 px | 600 |

Frases cortas, en segunda persona y en positivo: "Inclina para mover la
canasta", no "El jugador deberá inclinar el dispositivo".

---

## 4. El foco (RF-08)

No hay cursor ni hover: el jugador mueve el foco inclinando el celular y
confirma con el botón.

El foco se marca con **tres señales a la vez**, nunca solo con color:

1. Borde de 6 px en `accent`.
2. Fondo `bg-raised`.
3. Escala 1.06 respecto de las demás.

Reglas de navegación, para quien lo implemente en Unity:

- Inclinar mueve **una opción por vez**, con un enfriamiento de **350 ms**: si no,
  el foco cruza el menú entero de un golpe.
- Umbral de entrada 20°, de salida 10° (los mismos de `GestureSettings`), para
  que el foco no vibre entre dos opciones.
- **Confirmar es siempre el botón del mando.** Nunca por permanencia: un niño
  con TDAH deja el foco encima de cosas que no quiere elegir.
- El foco nunca se pierde: al entrar a una pantalla hay uno puesto, y en los
  extremos se queda quieto en vez de dar la vuelta.
- Al mover el foco, el mando vibra `tap`. **Lo manda Unity**, con
  `{ "t": "haptic", "slot": n, "pattern": "tap" }`: el mando no sabe cuándo
  cambió el foco, solo manda ángulos. El patrón ya existe en `haptics.js`.

---

## 5. Movimiento

| Transición | Duración |
| --- | --- |
| Cambio de foco | 120 ms |
| Entrar o salir de una pantalla | 220 ms, fundido y desplazamiento corto |
| Retroalimentación de acierto o error | 200 ms |
| Aparición de cada estrella | 300 ms, una tras otra |

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

### Pantalla 1 — Sala y QR

QR de 512 px a la izquierda, código de 4 caracteres a 160 px a la derecha, y una
sola frase: "Escanea el código con la cámara". Abajo, el estado del mando:
"Esperando mando…" → "Mando 1 conectado". Nada más en pantalla.

### Pantalla 4 — Instrucciones (RF-10)

La más importante y la más fácil de arruinar. Lleva exactamente cuatro cosas:

1. Una frase de una línea con el gesto.
2. La demostración del gesto, en bucle y sin texto encima.
3. El ícono de audio, porque la frase se narra (RNF-06).
4. Un botón: "Toca el botón para empezar".

Sin párrafos, sin lista de reglas, sin puntajes. Si no se entiende sin leer,
está mal hecha.

### Pantalla 6 — HUD

Solo dos datos: **tiempo** arriba a la izquierda (barra más número) y **puntaje**
arriba a la derecha. Nada de contadores de errores ni de vidas en pantalla.

Retroalimentación:

- **Acierto:** `+10` que sube y se desvanece sobre el objeto, borde de pantalla
  en `accent` 200 ms, vibración `hit`. La racha aparece en chico y en positivo
  ("3 seguidos").
- **Error:** el objeto se desvanece, borde en `alert` a la mitad de intensidad y
  la mitad de tiempo, vibración `miss`. **Sin texto**, sin "fallaste", sin sonido
  de bocina, sin sacudir la pantalla.

### Pantalla 8 — Resultados (RF-16)

Estrellas grandes que aparecen de a una, el puntaje, y una frase de refuerzo
("¡Muy bien!", "¡Lo lograste!"). Debajo, en chico y en tono neutro: aciertos y
tiempo de reacción. Si superó su récord: "¡Nuevo récord!".

**Decisión que conviene discutir:** RF-15 manda calcular errores y omisiones,
pero esta pantalla no los muestra. Calcularlos es una cosa y ponerle a un niño
"12 errores" en la TV es otra; RNF-05 prohíbe la penalización humillante. Las
métricas completas quedan guardadas para el equipo (`Metrics/`), no para el
jugador. Si el curso exige mostrarlas, se ponen fuera de la pantalla del niño.

### Pantalla 9 — Mando desconectado

"Esperando tu mando…" y una línea de qué hacer. Sin alarma, sin cuenta atrás,
sin amenaza de perder la partida: RNF-07 garantiza que no se pierde.

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
- Elegir la fuente definitiva y meterla en Unity como asset de TextMeshPro.
- Los íconos de los cuatro minijuegos: en la maqueta son emojis de relleno.
- Pasar estas pantallas a Unity UI.
