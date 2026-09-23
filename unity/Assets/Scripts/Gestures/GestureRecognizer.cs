// RF-05, RF-06: traduce las lecturas de un mando en gestos
using System;

namespace MoviMente.Gestures
{
    // Uno por mando (slot). Los minijuegos se suscriben a GestureDetected o leen
    // Tilt/IsSteady; nunca ven los sensores crudos (RNF-08).
    public sealed class GestureRecognizer
    {
        private readonly GestureSettings settings;
        private readonly NeutralPose neutral;
        private readonly TiltTracker tilt;
        private readonly GravityFilter gravity;
        private readonly SwingDetector swing;
        private readonly ShakeDetector shake;
        private readonly SteadyDetector steady;
        private readonly Action<GestureType, float> emit;
        private long currentTimestamp;
        private bool hasSamples;

        public GestureRecognizer(int slot, GestureSettings settings)
        {
            Slot = slot;
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            neutral = new NeutralPose(settings);
            tilt = new TiltTracker(settings);
            gravity = new GravityFilter(settings);
            swing = new SwingDetector(settings);
            shake = new ShakeDetector(settings);
            steady = new SteadyDetector(settings);
            emit = Emit;
        }

        public event Action<GestureEvent> GestureDetected;

        public int Slot { get; }
        public TiltAxes Tilt => tilt.Axes;
        public bool IsSteady => steady.IsSteady;
        public bool IsCalibrated => neutral.IsCalibrated;

        public void Process(in MotionSample sample)
        {
            // El reloj del celular retrocede si se recargó la página del mando.
            if (hasSamples && sample.Timestamp < currentTimestamp) ResetMotion();
            hasSamples = true;
            currentTimestamp = sample.Timestamp;

            neutral.Observe(sample);
            neutral.Relative(sample, out float deltaBeta, out float deltaGamma);
            float x = settings.InvertX ? -deltaGamma : deltaGamma;
            // Inclinar la punta del celular hacia adelante baja beta.
            float y = settings.InvertY ? deltaBeta : -deltaBeta;

            Vec3 linear = gravity.Update(sample);

            tilt.Update(x, y, emit);
            swing.Update(linear, sample.Timestamp, emit);
            shake.Update(linear, sample.Timestamp, emit);
            steady.Update(linear.Magnitude, x, y, sample.Timestamp, emit);
        }

        // Calibrar (CU-03) y recentrar (CU-07): la pose actual pasa a ser la neutra.
        public bool Calibrate()
        {
            if (!neutral.Calibrate()) return false;
            tilt.Reset();
            steady.Reset();
            return true;
        }

        private void ResetMotion()
        {
            gravity.Reset();
            swing.Reset();
            shake.Reset();
            steady.Reset();
        }

        private void Emit(GestureType type, float intensity)
        {
            GestureDetected?.Invoke(new GestureEvent(Slot, type, intensity, currentTimestamp));
        }
    }
}
