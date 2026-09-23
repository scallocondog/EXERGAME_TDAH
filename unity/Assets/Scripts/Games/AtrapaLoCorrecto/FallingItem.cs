// RF-11: un objeto que cae; la escena lo dibuja, las reglas lo mueven
namespace MoviMente.Games.AtrapaLoCorrecto
{
    public sealed class FallingItem
    {
        internal FallingItem(int id, int category, bool isTarget, float x, float spawnedAt)
        {
            Id = id;
            Category = category;
            IsTarget = isTarget;
            X = x;
            Y = 1f;
            SpawnedAt = spawnedAt;
        }

        public int Id { get; }
        // 0 a CategoryCount - 1; la escena decide qué ícono es cada categoría.
        public int Category { get; }
        public bool IsTarget { get; }
        // Campo normalizado: X de -1 a 1, Y de 1 (arriba) a 0 (línea de la canasta).
        public float X { get; }
        public float Y { get; internal set; }
        public float SpawnedAt { get; }
        // Primera vez que la canasta quedó debajo; base del tiempo de reacción.
        public float? AlignedAt { get; internal set; }
    }
}
