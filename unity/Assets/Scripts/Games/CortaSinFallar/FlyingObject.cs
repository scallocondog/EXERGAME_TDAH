// RF-13: una fruta lanzada o un objeto prohibido; la escena lo dibuja, las reglas lo mueven
namespace MoviMente.Games.CortaSinFallar
{
    public enum FlyingKind
    {
        // Fruta en tiro parabólico: hay que cortarla.
        Valid,
        // Recorre una ruta de waypoints: no hay que cortarlo.
        Forbidden,
    }

    public sealed class FlyingObject
    {
        internal FlyingObject(int id, int category, float spawnedAt, Projectile projectile, float windowStart, float windowEnd)
        {
            Id = id;
            Kind = FlyingKind.Valid;
            Category = category;
            SpawnedAt = spawnedAt;
            Projectile = projectile;
            WindowStart = windowStart;
            WindowEnd = windowEnd;
            Position = projectile.Start;
        }

        internal FlyingObject(int id, float spawnedAt, WaypointFollower route)
        {
            Id = id;
            Kind = FlyingKind.Forbidden;
            Category = -1;
            SpawnedAt = spawnedAt;
            Route = route;
            Position = route.Position;
        }

        public int Id { get; }
        public FlyingKind Kind { get; }
        // Fruta: 0 a CategoryCount - 1, solo para elegir el modelo. Prohibido: -1.
        public int Category { get; }
        public float SpawnedAt { get; }
        public FieldPoint Position { get; internal set; }

        // Solo frutas: la parábola y la ventana predicha en que se puede cortar
        // (en tiempo de partida, sin pausas).
        public Projectile Projectile { get; }
        public float WindowStart { get; }
        public float WindowEnd { get; }

        // Solo prohibidos.
        public WaypointFollower Route { get; }
    }
}
