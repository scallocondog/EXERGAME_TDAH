// RF-13: técnica Rutas (Waypoints) para el objeto prohibido
using System;
using System.Collections.Generic;

namespace MoviMente.Games.CortaSinFallar
{
    // Ruta = lista ordenada de puntos. El objeto va en línea recta hacia el
    // waypoint actual a velocidad constante; cuando lo alcanza pasa al
    // siguiente, y al llegar al último termina. Lo que le sobra de recorrido
    // en ese cuadro lo sigue usando hacia el próximo, así la velocidad no
    // tiene saltos al doblar.
    public sealed class WaypointFollower
    {
        private readonly FieldPoint[] waypoints;
        private readonly float speed;

        public WaypointFollower(IReadOnlyList<FieldPoint> waypoints, float speed)
        {
            if (waypoints == null || waypoints.Count < 2) throw new ArgumentException("Una ruta necesita al menos dos puntos", nameof(waypoints));
            if (speed <= 0f) throw new ArgumentOutOfRangeException(nameof(speed), "La velocidad tiene que ser positiva");

            this.waypoints = new FieldPoint[waypoints.Count];
            for (int i = 0; i < waypoints.Count; i++) this.waypoints[i] = waypoints[i];
            this.speed = speed;
            Position = this.waypoints[0];
            CurrentIndex = 1;
        }

        public IReadOnlyList<FieldPoint> Waypoints => waypoints;
        public FieldPoint Position { get; private set; }
        // Waypoint hacia el que va ahora.
        public int CurrentIndex { get; private set; }
        public bool Finished => CurrentIndex >= waypoints.Length;

        public void Advance(float deltaSeconds)
        {
            float remaining = speed * deltaSeconds;
            while (remaining > 0f && !Finished)
            {
                FieldPoint target = waypoints[CurrentIndex];
                float distance = Position.DistanceTo(target);
                if (distance <= remaining)
                {
                    // Llegó: se para en el waypoint y apunta al siguiente.
                    Position = target;
                    remaining -= distance;
                    CurrentIndex++;
                    continue;
                }
                float k = remaining / distance;
                Position = new FieldPoint(Position.X + (target.X - Position.X) * k, Position.Y + (target.Y - Position.Y) * k);
                remaining = 0f;
            }
        }
    }
}
