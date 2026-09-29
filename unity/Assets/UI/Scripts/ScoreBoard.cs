// RF-16: lo que la TV muestra del desempeño en la partida: puntaje, racha, estrellas y resumen
using MoviMente.Games;

namespace MoviMente.UI
{
    // Puntaje provisional de 10 por acierto, hasta que Metrics/ (RF-15) lo calcule.
    // Todo es desempeño en el juego, nunca una señal clínica (RNF-09).
    public sealed class ScoreBoard
    {
        public const int PointsPerHit = 10;
        // "3 seguidos" aparece desde el tercer acierto seguido.
        public const int StreakShownFrom = 3;
        public const float ThreeStarsRatio = 0.8f;
        public const float TwoStarsRatio = 0.5f;

        private float reactionSum;
        private int reactionCount;

        public int Score { get; private set; }
        public int Hits { get; private set; }
        // Errores y omisiones juntos: la pantalla no los muestra, solo cuentan para las estrellas.
        public int Misses { get; private set; }
        public int Streak { get; private set; }
        public bool ShowsStreak => Streak >= StreakShownFrom;
        public float? AverageReactionSeconds => reactionCount > 0 ? reactionSum / reactionCount : (float?)null;

        // Siempre hay al menos una estrella: el resultado refuerza, no castiga (RNF-05).
        public int Stars
        {
            get
            {
                int trials = Hits + Misses;
                if (trials == 0) return 1;
                float ratio = (float)Hits / trials;
                if (ratio >= ThreeStarsRatio) return 3;
                return ratio >= TwoStarsRatio ? 2 : 1;
            }
        }

        // El error y la omisión solo cortan la racha: el puntaje nunca baja.
        public void Add(TrialResult trial)
        {
            if (trial.Outcome != TrialOutcome.Hit)
            {
                Misses++;
                Streak = 0;
                return;
            }

            Hits++;
            Streak++;
            Score += PointsPerHit;
            if (!trial.ReactionSeconds.HasValue) return;
            reactionSum += trial.ReactionSeconds.Value;
            reactionCount++;
        }

        public void Reset()
        {
            Score = 0;
            Hits = 0;
            Misses = 0;
            Streak = 0;
            reactionSum = 0f;
            reactionCount = 0;
        }
    }
}
