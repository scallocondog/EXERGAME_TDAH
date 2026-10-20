// RF-09, RF-13: lo que cambia entre fácil, medio y difícil
namespace MoviMente.Games.CortaSinFallar
{
    public sealed class CortaTuning
    {
        public static readonly CortaTuning Easy = new CortaTuning(2.4f, 0.9f, 7.0f, 0.35f);
        public static readonly CortaTuning Medium = new CortaTuning(1.8f, 1.3f, 5.0f, 0.45f);
        public static readonly CortaTuning Hard = new CortaTuning(1.3f, 1.8f, 3.5f, 0.6f);

        private CortaTuning(float launchIntervalSeconds, float gravity, float forbiddenIntervalSeconds, float forbiddenSpeed)
        {
            LaunchIntervalSeconds = launchIntervalSeconds;
            Gravity = gravity;
            ForbiddenIntervalSeconds = forbiddenIntervalSeconds;
            ForbiddenSpeed = forbiddenSpeed;
        }

        // Cada cuánto se lanza una fruta.
        public float LaunchIntervalSeconds { get; }
        // Más gravedad = vuelo más corto y menos tiempo dentro de la zona de corte.
        public float Gravity { get; }
        public float ForbiddenIntervalSeconds { get; }
        // Velocidad del prohibido sobre su ruta (el campo mide 2 de ancho).
        public float ForbiddenSpeed { get; }

        public static CortaTuning For(Difficulty difficulty) => difficulty switch
        {
            Difficulty.Easy => Easy,
            Difficulty.Medium => Medium,
            _ => Hard,
        };
    }
}
