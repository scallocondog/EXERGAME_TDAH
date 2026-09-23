// RF-06: separa la gravedad para quedarse solo con el movimiento de la mano
namespace MoviMente.Gestures
{
    internal sealed class GravityFilter
    {
        private readonly GestureSettings settings;
        private Vec3 gravity;
        private long lastTimestamp;
        private bool initialized;

        public GravityFilter(GestureSettings settings)
        {
            this.settings = settings;
        }

        // Paso bajo por tiempo (no por muestra) porque el mando manda entre 30 y 60 Hz.
        public Vec3 Update(in MotionSample sample)
        {
            if (!initialized)
            {
                gravity = sample.Acceleration;
                lastTimestamp = sample.Timestamp;
                initialized = true;
                return Vec3.Zero;
            }

            float dt = (sample.Timestamp - lastTimestamp) / 1000f;
            lastTimestamp = sample.Timestamp;
            float k = dt / (settings.GravityTimeConstantSeconds + dt);
            gravity += (sample.Acceleration - gravity) * k;
            return sample.Acceleration - gravity;
        }

        public void Reset() => initialized = false;
    }
}
