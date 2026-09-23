// RF-06: mantener estable (Equilibrio, RF-14)
using System;

namespace MoviMente.Gestures
{
    internal sealed class SteadyDetector
    {
        private readonly GestureSettings settings;
        private long? steadySince;
        private float anchorX;
        private float anchorY;

        public SteadyDetector(GestureSettings settings)
        {
            this.settings = settings;
        }

        public bool IsSteady { get; private set; }

        // Estable = poca aceleración y el ángulo sin alejarse del punto donde
        // empezó la quietud. Se avisa una vez; se vuelve a avisar tras moverse.
        public void Update(float linearMagnitude, float xDegrees, float yDegrees, long timestamp,
            Action<GestureType, float> emit)
        {
            bool calm = steadySince.HasValue
                && linearMagnitude <= settings.SteadyMaxLinearAcceleration
                && Math.Abs(xDegrees - anchorX) <= settings.SteadyMaxAngleDelta
                && Math.Abs(yDegrees - anchorY) <= settings.SteadyMaxAngleDelta;

            if (!calm)
            {
                steadySince = timestamp;
                anchorX = xDegrees;
                anchorY = yDegrees;
                IsSteady = false;
                return;
            }

            if (IsSteady || timestamp - steadySince.Value < settings.SteadyHoldMs) return;
            IsSteady = true;
            emit(GestureType.Steady, 1f);
        }

        public void Reset()
        {
            steadySince = null;
            IsSteady = false;
        }
    }
}
