// RF-13: punto del campo normalizado (X de -1 a 1, Y de 0 abajo a 1 arriba)
using System;

namespace MoviMente.Games.CortaSinFallar
{
    public readonly struct FieldPoint
    {
        public readonly float X;
        public readonly float Y;

        public FieldPoint(float x, float y)
        {
            X = x;
            Y = y;
        }

        public float DistanceTo(FieldPoint other)
        {
            float dx = other.X - X;
            float dy = other.Y - Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public override string ToString() => $"({X:0.000}, {Y:0.000})";
    }
}
