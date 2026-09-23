// RF-05, RF-06: aritmética de ángulos en grados
using System;

namespace MoviMente.Gestures
{
    internal static class Angles
    {
        // Lleva cualquier ángulo a (-180, 180] para que 179° y -179° queden a 2°.
        public static float Wrap180(float degrees)
        {
            float wrapped = degrees % 360f;
            if (wrapped > 180f) wrapped -= 360f;
            if (wrapped <= -180f) wrapped += 360f;
            return wrapped;
        }

        public static float Clamp01(float value) => Math.Max(0f, Math.Min(1f, value));
    }
}
