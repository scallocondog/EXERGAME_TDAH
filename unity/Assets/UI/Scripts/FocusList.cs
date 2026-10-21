// RF-08: el foco de un menú que se maneja inclinando el celular (docs/ux/sistema-visual.md §4)
using System;
using MoviMente.Gestures;

namespace MoviMente.UI
{
    public sealed class FocusList
    {
        // Sin enfriamiento, una inclinación larga cruzaría el menú entero de un golpe.
        public const float CooldownSeconds = 0.35f;

        private float lastMoveAt = float.NegativeInfinity;

        public FocusList(int count)
        {
            if (count < 1) throw new ArgumentOutOfRangeException(nameof(count), "Un menú necesita al menos una opción");
            Count = count;
        }

        public int Count { get; }
        public int Index { get; private set; }

        // Al entrar a una pantalla siempre hay un foco puesto.
        public void Reset(int index)
        {
            Index = Math.Clamp(index, 0, Count - 1);
            lastMoveAt = float.NegativeInfinity;
        }

        // En los extremos el foco se queda quieto: no da la vuelta.
        public bool Move(int step, float now)
        {
            if (step == 0 || now - lastMoveAt < CooldownSeconds) return false;
            int next = Math.Clamp(Index + step, 0, Count - 1);
            if (next == Index) return false;
            Index = next;
            lastMoveAt = now;
            return true;
        }

        // Adelante o a la izquierda va a la opción anterior; atrás o a la derecha, a la siguiente.
        public static int StepFor(GestureType gesture) => gesture switch
        {
            GestureType.TiltLeft or GestureType.TiltForward => -1,
            GestureType.TiltRight or GestureType.TiltBack => 1,
            _ => 0,
        };
    }
}
