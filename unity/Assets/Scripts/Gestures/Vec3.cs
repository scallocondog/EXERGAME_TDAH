// RF-06: vector mínimo para no depender de UnityEngine en el reconocedor
using System;

namespace MoviMente.Gestures
{
    public readonly struct Vec3
    {
        public static readonly Vec3 Zero = new Vec3(0f, 0f, 0f);

        public readonly float X;
        public readonly float Y;
        public readonly float Z;

        public Vec3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float Magnitude => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 v, float k) => new Vec3(v.X * k, v.Y * k, v.Z * k);

        // Eje con más aceleración (0 = X, 1 = Y, 2 = Z) y su valor con signo.
        public void Dominant(out int axis, out float value)
        {
            axis = 0;
            value = X;
            if (Math.Abs(Y) > Math.Abs(value)) { axis = 1; value = Y; }
            if (Math.Abs(Z) > Math.Abs(value)) { axis = 2; value = Z; }
        }
    }
}
