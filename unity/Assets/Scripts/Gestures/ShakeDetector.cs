// RF-06: sacudir, varios picos de aceleración alternando de sentido
using System;

namespace MoviMente.Gestures
{
    internal sealed class ShakeDetector
    {
        private readonly GestureSettings settings;
        private int peaks;
        private int lastAxis;
        private int lastSign;
        private long firstPeakAt;
        private float strongest;
        private long? lastShakeAt;

        public ShakeDetector(GestureSettings settings)
        {
            this.settings = settings;
        }

        public void Update(Vec3 linear, long timestamp, Action<GestureType, float> emit)
        {
            linear.Dominant(out int axis, out float value);
            if (Math.Abs(value) < settings.ShakeThreshold) return;

            int sign = Math.Sign(value);
            bool alternates = peaks > 0 && axis == lastAxis && sign != lastSign
                && timestamp - firstPeakAt <= settings.ShakeWindowMs;

            if (alternates)
            {
                peaks++;
                strongest = Math.Max(strongest, Math.Abs(value));
            }
            else if (peaks == 0 || axis != lastAxis || timestamp - firstPeakAt > settings.ShakeWindowMs)
            {
                peaks = 1;
                firstPeakAt = timestamp;
                strongest = Math.Abs(value);
            }
            lastAxis = axis;
            lastSign = sign;

            if (peaks < settings.ShakeMinPeaks) return;
            peaks = 0;
            if (lastShakeAt.HasValue && timestamp - lastShakeAt.Value < settings.ShakeCooldownMs) return;

            lastShakeAt = timestamp;
            emit(GestureType.Shake, Angles.Clamp01(strongest / settings.ShakeMaxAcceleration));
        }

        public void Reset()
        {
            peaks = 0;
            lastShakeAt = null;
        }
    }
}
