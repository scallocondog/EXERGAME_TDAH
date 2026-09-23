// RF-09, RF-11: lo que cambia entre fácil, medio y difícil
namespace MoviMente.Games.AtrapaLoCorrecto
{
    public sealed class AtrapaTuning
    {
        public static readonly AtrapaTuning Easy = new AtrapaTuning(2.2f, 4.0f, 0.25f, 0.6f);
        public static readonly AtrapaTuning Medium = new AtrapaTuning(1.6f, 3.2f, 0.20f, 0.5f);
        public static readonly AtrapaTuning Hard = new AtrapaTuning(1.1f, 2.4f, 0.16f, 0.4f);

        private AtrapaTuning(float spawnIntervalSeconds, float fallSeconds, float basketHalfWidth, float targetRatio)
        {
            SpawnIntervalSeconds = spawnIntervalSeconds;
            FallSeconds = fallSeconds;
            BasketHalfWidth = basketHalfWidth;
            TargetRatio = targetRatio;
        }

        public float SpawnIntervalSeconds { get; }
        // Tiempo que tarda un objeto en caer de arriba a la canasta.
        public float FallSeconds { get; }
        public float BasketHalfWidth { get; }
        // Proporción de objetos de la categoría pedida; el resto son distractores.
        public float TargetRatio { get; }

        public static AtrapaTuning For(Difficulty difficulty) => difficulty switch
        {
            Difficulty.Easy => Easy,
            Difficulty.Medium => Medium,
            _ => Hard,
        };
    }
}
