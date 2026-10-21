// RF-16: lo que la TV muestra del desempeño en la partida: puntaje, racha, estrellas y resumen
using MoviMente.Games;
using MoviMente.Metrics;

namespace MoviMente.UI
{
    // Adaptador de las métricas de RF-15 para el HUD y los resultados.
    public sealed class ScoreBoard
    {
        public const int PointsPerHit = SessionMetrics.PointsPerHit;
        // "3 seguidos" aparece desde el tercer acierto seguido.
        public const int StreakShownFrom = 3;
        public const float ThreeStarsRatio = SessionMetrics.ThreeStarsRatio;
        public const float TwoStarsRatio = SessionMetrics.TwoStarsRatio;

        private readonly SessionMetrics metrics = new SessionMetrics();

        public int Score => metrics.Score;
        public int Hits => metrics.Hits;
        // Errores y omisiones juntos: la pantalla no los muestra, solo cuentan para las estrellas.
        public int Misses => metrics.Misses;
        public int Streak => metrics.Streak;
        public bool ShowsStreak => Streak >= StreakShownFrom;
        public float? AverageReactionSeconds => metrics.AverageReactionSeconds;

        // Siempre hay al menos una estrella: el resultado refuerza, no castiga (RNF-05).
        public int Stars => metrics.Stars;

        // El error y la omisión solo cortan la racha: el puntaje nunca baja.
        public void Add(TrialResult trial) => metrics.Add(trial);

        public SessionResult Result(float durationSeconds) => metrics.Result(durationSeconds);

        public void Reset() => metrics.Reset();
    }
}
