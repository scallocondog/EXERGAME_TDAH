# Gestos — postura, calibración y uso por minijuego

Acuerdo del equipo del 2026-09-22. Si cambia, se actualiza aquí y en
`GestureSettings` en el mismo PR. Código: `unity/Assets/Scripts/Gestures/`
(RF-05, RF-06).

## Postura del mando

- Celular **en vertical**, sostenido con una mano **como un Wiimote**.
- Pantalla hacia arriba, **inclinada unos 30° hacia el jugador** (beta ≈ 30°).
- Inclinar a izquierda o derecha usa **gamma**; hacia adelante o atrás, **beta**.
- **No** sostenerlo con la pantalla mirando a la cara (beta ≈ 90°): en esa
  postura gamma se vuelve inestable y la inclinación lateral falla.

La rotación no se puede bloquear de forma confiable (`screen.orientation.lock`
solo funciona en Android a pantalla completa y no existe en Safari iOS). La
página del mando se diseña en vertical y, si detecta horizontal, muestra "gira
tu celular".

## Calibración (CU-03) y recentrado (CU-07)

| Paso | Quién | Qué hace |
| --- | --- | --- |
| 1 | Mando | Pide sostener quieto en la postura de arriba y cuenta **2 s**. |
| 2 | Mando | Si en esos 2 s la aceleración varía por encima de un umbral, **reinicia la cuenta** en vez de calibrar. |
| 3 | Mando | Al completar los 2 s quieto, envía `calibrate`. |
| 4 | Unity | Toma como posición neutra el **promedio de los últimos 300 ms**. |

Recentrar (`button` con `recenter`) hace lo mismo en Unity, a partir de la
postura del momento.

## Gestos por minijuego

| Minijuego | Gestos | Nota |
| --- | --- | --- |
| Menús (RF-08) | Inclinar izquierda/derecha + botón en pantalla | Nada más. |
| Atrapa lo correcto (RF-11) | Inclinación continua izquierda/derecha (`Tilt.X`) | Mueve la canasta. |
| Sigue la secuencia (RF-12) | Fácil y medio: las 4 direcciones. Difícil: 4 direcciones + swing | |
| Corta sin fallar (RF-13) | Swing | |
| Equilibrio (RF-14) | Inclinación continua + mantener estable | |

**Regla:** swing y sacudir **nunca** van en el mismo minijuego. Se confunden
entre sí, tanto para el reconocedor (una sacudida empieza con un pico que
también es swing) como para un niño.

Hoy ningún minijuego usa sacudir; el gesto queda disponible para uno futuro.
