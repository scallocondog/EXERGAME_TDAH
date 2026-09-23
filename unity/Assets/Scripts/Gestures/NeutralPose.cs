// RF-05: posición neutra del mando; calibrar y recentrar son la misma operación
using System.Collections.Generic;

namespace MoviMente.Gestures
{
    internal sealed class NeutralPose
    {
        private readonly GestureSettings settings;
        private readonly Queue<MotionSample> recent = new Queue<MotionSample>();
        private long lastTimestamp;
        private bool hasPose;
        private float beta;
        private float gamma;

        public NeutralPose(GestureSettings settings)
        {
            this.settings = settings;
        }

        public bool IsCalibrated { get; private set; }

        public void Observe(in MotionSample sample)
        {
            if (recent.Count > 0 && sample.Timestamp < lastTimestamp) recent.Clear();
            lastTimestamp = sample.Timestamp;
            recent.Enqueue(sample);
            while (sample.Timestamp - recent.Peek().Timestamp > settings.CalibrationWindowMs)
            {
                recent.Dequeue();
            }

            // Hasta que llegue la calibración real, la primera lectura hace de
            // neutra para que no salten gestos por la postura inicial.
            if (!hasPose) SetPose(sample.Beta, sample.Gamma);
        }

        // Promedia la ventana reciente para que un temblor al pulsar no quede
        // grabado como posición neutra.
        public bool Calibrate()
        {
            if (recent.Count == 0) return false;

            float reference = recent.Peek().Beta;
            float betaOffset = 0f;
            float gammaSum = 0f;
            foreach (MotionSample sample in recent)
            {
                betaOffset += Angles.Wrap180(sample.Beta - reference);
                gammaSum += sample.Gamma;
            }

            SetPose(Angles.Wrap180(reference + betaOffset / recent.Count), gammaSum / recent.Count);
            IsCalibrated = true;
            return true;
        }

        public void Relative(in MotionSample sample, out float deltaBeta, out float deltaGamma)
        {
            deltaBeta = Angles.Wrap180(sample.Beta - beta);
            deltaGamma = sample.Gamma - gamma;
        }

        private void SetPose(float newBeta, float newGamma)
        {
            beta = newBeta;
            gamma = newGamma;
            hasPose = true;
        }
    }
}
