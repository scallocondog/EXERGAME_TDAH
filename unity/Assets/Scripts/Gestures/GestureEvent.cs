// RF-06, RNF-08: lo único que un minijuego recibe del reconocedor
namespace MoviMente.Gestures
{
    public readonly struct GestureEvent
    {
        public readonly int Slot;
        public readonly GestureType Type;
        // 0 a 1: qué tan marcado fue el gesto.
        public readonly float Intensity;
        // Reloj del celular, para medir tiempo de reacción (RF-15) y latencia (RNF-01).
        public readonly long Timestamp;

        public GestureEvent(int slot, GestureType type, float intensity, long timestamp)
        {
            Slot = slot;
            Type = type;
            Intensity = intensity;
            Timestamp = timestamp;
        }

        public override string ToString() => $"slot {Slot}: {Type} ({Intensity:0.00}) @ {Timestamp}";
    }
}
