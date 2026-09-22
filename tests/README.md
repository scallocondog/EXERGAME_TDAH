# tests

Dueño: **Misael Marrón**. Requisitos: RNF-01, RNF-03, RNF-07 y la validación de
mensajes del servidor.

Cada prueba lleva en el nombre el ID que verifica, para que la trazabilidad sea
directa:

```
tests/
├── validation/
│   ├── rf-04-rangos.test.js        valores fuera de rango, NaN, null
│   ├── rf-04-malformados.test.js   JSON roto, tipos desconocidos
│   └── rf-04-frecuencia.test.js    throttling a 80 Hz
├── latency/
│   └── rnf-01-latencia.test.js     mediana < 100 ms en LAN
└── manual/
    └── plan.md                     pruebas que se hacen con celular en mano
```

Las pruebas que requieren dispositivo real se documentan en
[../docs/plan-de-pruebas.md](../docs/plan-de-pruebas.md) con su resultado y la
fecha de ejecución.
