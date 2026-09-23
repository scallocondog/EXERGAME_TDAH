// RF-04, RF-06: una lectura de sensores del mando, ya validada por el servidor
namespace MoviMente.Gestures
{
    public readonly struct MotionSample
    {
        // Milisegundos según el reloj del celular (campo ts del protocolo).
        public readonly long Timestamp;
        public readonly float Alpha;
        public readonly float Beta;
        public readonly float Gamma;
        // Aceleración incluyendo gravedad, en m/s².
        public readonly Vec3 Acceleration;

        public MotionSample(long timestamp, float alpha, float beta, float gamma, Vec3 acceleration)
        {
            Timestamp = timestamp;
            Alpha = alpha;
            Beta = beta;
            Gamma = gamma;
            Acceleration = acceleration;
        }
    }
}
