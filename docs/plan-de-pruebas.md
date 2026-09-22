# Plan de pruebas — MoviMente

Responsable: **Misael Marrón**. Cada integrante ejecuta las pruebas de sus
requisitos antes de abrir el PR; Misael corre el plan completo antes de cada
entrega.

## Entorno de prueba

- PC con el juego (Windows) y servidor en la misma red Wi-Fi.
- Celular Android con Chrome reciente.
- Celular iPhone con Safari reciente.
- Router local o hotspot (no la red de la universidad, que puede aislar clientes).

---

## Pruebas por caso de uso

| # | Caso | Pasos | Resultado esperado |
| --- | --- | --- | --- |
| P-01 | CU-01 | Abrir el juego en la PC | Se muestra sala con código y QR legible desde 2 m (RF-01) |
| P-02 | CU-02 | Escanear el QR con el celular | Se abre el mando y el juego muestra "Mando conectado" (RF-02) |
| P-03 | CU-02 alt | Escribir un código inválido | Mensaje de error y pedido de reescanear, sin caída |
| P-04 | CU-02 alt | Conectar un tercer mando | Se rechaza con `room_full` |
| P-05 | CU-03 | Pulsar "Activar sensores" y aceptar | Permiso concedido y calibración de 2 s (RF-03, RF-05) |
| P-06 | CU-03 alt | Denegar el permiso | El mando explica cómo habilitarlo y no deja continuar |
| P-07 | CU-04 | Inclinar el celular en el menú | El foco se mueve; el botón confirma (RF-08) |
| P-08 | CU-05 | Elegir minijuego y dificultad | Carga la partida seleccionada (RF-09) |
| P-09 | CU-06 | Jugar una partida completa de cada minijuego | Instrucciones, cuenta regresiva, gestos reconocidos, retroalimentación (RF-10 a RF-14) |
| P-10 | CU-06 | Provocar aciertos y errores | Vibración correcta en ambos casos (RF-18) |
| P-11 | CU-06 | Pausar y elegir salir | No se guarda puntaje (RF-19) |
| P-12 | CU-07 | Pulsar "Recentrar" con el celular girado | La posición actual pasa a ser la neutra (RF-05) |
| P-13 | CU-08 | Apagar el Wi-Fi del celular a mitad de partida | El juego pausa, avisa y retoma al reconectar sin perder el puntaje (RF-07, RNF-07) |
| P-14 | CU-09 | Terminar una partida superando el récord | Resumen con puntaje y estrellas; el récord se actualiza (RF-16, RF-17) |

---

## Pruebas no funcionales

| # | RNF | Método | Criterio |
| --- | --- | --- | --- |
| P-15 | RNF-01 | `ts` del mando contra el frame de reacción en Unity, 100 muestras | Mediana < 100 ms, sin picos sostenidos |
| P-16 | RNF-02 | Unity Profiler en la escena más pesada, 2 min | ≥ 60 fps; nunca por debajo de 30 |
| P-17 | RNF-03 | Ejecutar P-01 a P-14 en Android y en iOS | Mismo comportamiento en ambos |
| P-18 | RNF-04 | Inspeccionar tráfico y almacenamiento | Todo por HTTPS; ningún dato personal; solo puntajes locales |
| P-19 | RNF-05 | Cronometrar partidas y revisar pantallas | 3–5 min, una instrucción a la vez, sin distractores |
| P-20 | RNF-06 | Jugar con el texto tapado | Se puede jugar solo con íconos y audio |
| P-21 | RNF-08 | Agregar un minijuego de prueba | No requiere tocar `Net/` ni `Gestures/` |
| P-22 | RNF-09 | Revisar todo texto visible | Ninguna frase sugiere diagnóstico o evaluación clínica |

---

## Pruebas de robustez del servidor

| # | Caso | Esperado |
| --- | --- | --- |
| P-23 | Enviar JSON malformado | Se descarta y se registra; Unity no lo recibe |
| P-24 | Enviar `beta` = 999 | Se descarta por fuera de rango |
| P-25 | Enviar a 200 Hz | Se aplica throttling a ≤ 80 Hz |
| P-26 | Cerrar Unity con el mando conectado | El mando recibe `state: disconnected` y reintenta |

---

## Registro de ejecución

Cada corrida completa se anota aquí con fecha, versión y resultado.

| Fecha | Versión | Pruebas ejecutadas | Fallos | Notas |
| --- | --- | --- | --- | --- |
| — | — | — | — | Pendiente de la primera corrida |
