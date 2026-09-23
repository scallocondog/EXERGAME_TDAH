// RF-06: inclinar izquierda/derecha y adelante/atrás, continuo y discreto
using System;

namespace MoviMente.Gestures
{
    internal sealed class TiltTracker
    {
        private readonly GestureSettings settings;
        private int stateX;
        private int stateY;

        public TiltTracker(GestureSettings settings)
        {
            this.settings = settings;
        }

        public TiltAxes Axes { get; private set; } = TiltAxes.Neutral;

        // xDegrees > 0 es derecha; yDegrees > 0 es adelante.
        public void Update(float xDegrees, float yDegrees, Action<GestureType, float> emit)
        {
            Axes = new TiltAxes(Normalize(xDegrees), Normalize(yDegrees));
            stateX = Step(stateX, xDegrees, GestureType.TiltRight, GestureType.TiltLeft, emit);
            stateY = Step(stateY, yDegrees, GestureType.TiltForward, GestureType.TiltBack, emit);
        }

        public void Reset()
        {
            stateX = 0;
            stateY = 0;
            Axes = TiltAxes.Neutral;
        }

        // Histéresis: se dispara al pasar el umbral de entrada y solo se rearma al
        // volver cerca del centro, así mantener la inclinación no repite el gesto.
        private int Step(int current, float degrees, GestureType positive, GestureType negative,
            Action<GestureType, float> emit)
        {
            int next = current;
            if (degrees >= settings.TiltEnterDegrees) next = 1;
            else if (degrees <= -settings.TiltEnterDegrees) next = -1;
            else if (Math.Abs(degrees) <= settings.TiltExitDegrees) next = 0;

            if (next != current && next != 0)
            {
                emit(next > 0 ? positive : negative, Angles.Clamp01(Math.Abs(degrees) / settings.TiltMaxDegrees));
            }
            return next;
        }

        private float Normalize(float degrees)
        {
            float beyondDeadzone = Math.Abs(degrees) - settings.TiltDeadzoneDegrees;
            if (beyondDeadzone <= 0f) return 0f;
            float range = settings.TiltMaxDegrees - settings.TiltDeadzoneDegrees;
            return Math.Sign(degrees) * Angles.Clamp01(beyondDeadzone / range);
        }
    }
}
