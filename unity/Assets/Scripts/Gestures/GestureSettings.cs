// RF-06: umbrales del reconocedor, ajustables sin recompilar
using System;

namespace MoviMente.Gestures
{
    // Campos públicos para que Unity los serialice y se editen desde el
    // Inspector; el reconocedor lee siempre la instancia viva.
    [Serializable]
    public sealed class GestureSettings
    {
        // Supuesto: celular en vertical, pantalla hacia arriba e inclinado hacia
        // el jugador. Si en las pruebas un eje sale al revés, se invierte aquí.
        public bool InvertX;
        public bool InvertY;

        public float TiltDeadzoneDegrees = 4f;
        public float TiltEnterDegrees = 20f;
        public float TiltExitDegrees = 10f;
        public float TiltMaxDegrees = 40f;

        public int CalibrationWindowMs = 300;
        public float GravityTimeConstantSeconds = 0.25f;

        public float SwingThreshold = 14f;
        public float SwingMaxAcceleration = 35f;
        public int SwingCooldownMs = 350;

        public float ShakeThreshold = 8f;
        public float ShakeMaxAcceleration = 25f;
        public int ShakeMinPeaks = 3;
        public int ShakeWindowMs = 800;
        public int ShakeCooldownMs = 600;

        public float SteadyMaxLinearAcceleration = 1.2f;
        public float SteadyMaxAngleDelta = 5f;
        public int SteadyHoldMs = 1500;
    }
}
