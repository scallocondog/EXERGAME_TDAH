// RF-06: inclinación continua para mecánicas como mover la canasta (RF-11)
namespace MoviMente.Gestures
{
    public readonly struct TiltAxes
    {
        public static readonly TiltAxes Neutral = new TiltAxes(0f, 0f);

        // -1 izquierda, +1 derecha.
        public readonly float X;
        // -1 atrás, +1 adelante.
        public readonly float Y;

        public TiltAxes(float x, float y)
        {
            X = x;
            Y = y;
        }
    }
}
