// RF-13: técnica Física Predictiva (lanzamiento de proyectil) para las frutas válidas
using System;

namespace MoviMente.Games.CortaSinFallar
{
    // Tiro parabólico sin rozamiento:
    //   x(t) = x0 + vx·t
    //   y(t) = y0 + vy·t − ½·g·t²
    //
    // "Predictiva" porque se resuelve al revés y antes de lanzar: dado desde dónde
    // sale, dónde tiene que quedar la cima y la gravedad, se despejan vx y vy.
    // Con eso el juego sabe de antemano por dónde y cuándo va a pasar la fruta,
    // sin simular paso a paso: la posición es una fórmula cerrada del tiempo.
    public sealed class Projectile
    {
        private Projectile(FieldPoint start, float velocityX, float velocityY, float gravity)
        {
            Start = start;
            VelocityX = velocityX;
            VelocityY = velocityY;
            Gravity = gravity;
        }

        public FieldPoint Start { get; }
        public float VelocityX { get; }
        public float VelocityY { get; }
        public float Gravity { get; }

        // En la cima vy(t) = vy − g·t = 0.
        public float ApexTime => VelocityY / Gravity;
        // Vuelve a la altura de salida en el doble del tiempo de subida.
        public float FlightTime => 2f * ApexTime;
        public FieldPoint Apex => PositionAt(ApexTime);

        // Problema inverso: altura de la cima h = vy² / (2g)  →  vy = √(2·g·h),
        // tiempo de subida = vy / g, y en ese tiempo tiene que recorrer el ancho:
        // vx = (xCima − x0) / tSubida.
        public static Projectile Aim(FieldPoint start, FieldPoint apex, float gravity)
        {
            if (gravity <= 0f) throw new ArgumentOutOfRangeException(nameof(gravity), "La gravedad tiene que ser positiva");
            float height = apex.Y - start.Y;
            if (height <= 0f) throw new ArgumentException("La cima tiene que estar más arriba que la salida", nameof(apex));

            float velocityY = (float)Math.Sqrt(2.0 * gravity * height);
            float apexTime = velocityY / gravity;
            float velocityX = (apex.X - start.X) / apexTime;
            return new Projectile(start, velocityX, velocityY, gravity);
        }

        public FieldPoint PositionAt(float t) => new FieldPoint(
            Start.X + VelocityX * t,
            Start.Y + VelocityY * t - 0.5f * Gravity * t * t);

        // Los dos instantes en que pasa por una altura: subiendo y bajando.
        // Sale de despejar t en y(t) = altura (ecuación de segundo grado).
        public bool TryTimesAtHeight(float height, out float rising, out float falling)
        {
            float discriminant = VelocityY * VelocityY - 2f * Gravity * (height - Start.Y);
            if (discriminant < 0f)
            {
                rising = falling = 0f;
                return false;
            }
            float root = (float)Math.Sqrt(discriminant);
            rising = (VelocityY - root) / Gravity;
            falling = (VelocityY + root) / Gravity;
            return true;
        }
    }
}
