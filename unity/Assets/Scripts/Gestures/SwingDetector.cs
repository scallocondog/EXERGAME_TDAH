// RF-06: swing, un golpe brusco del celular (Corta sin fallar, RF-13)
using System;

namespace MoviMente.Gestures
{
    internal sealed class SwingDetector
    {
        private readonly GestureSettings settings;
        private long? lastSwingAt;
        private bool armed = true;

        public SwingDetector(GestureSettings settings)
        {
            this.settings = settings;
        }

        // Se emite al cruzar el umbral, sin esperar al pico, para no sumar
        // latencia (RNF-01). Un swing no se repite hasta que la mano se calme.
        public void Update(Vec3 linear, long timestamp, Action<GestureType, float> emit)
        {
            float magnitude = linear.Magnitude;
            if (!armed)
            {
                if (magnitude < settings.SwingThreshold * 0.5f) armed = true;
                return;
            }

            if (magnitude < settings.SwingThreshold) return;
            if (lastSwingAt.HasValue && timestamp - lastSwingAt.Value < settings.SwingCooldownMs) return;

            armed = false;
            lastSwingAt = timestamp;
            emit(GestureType.Swing, Angles.Clamp01(magnitude / settings.SwingMaxAcceleration));
        }

        public void Reset()
        {
            lastSwingAt = null;
            armed = true;
        }
    }
}
