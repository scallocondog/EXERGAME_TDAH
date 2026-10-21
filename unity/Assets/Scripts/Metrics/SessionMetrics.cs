// RF-15: métricas de desempeño de una partida, independientes de la interfaz
using MoviMente.Games;

namespace MoviMente.Metrics
{
    public sealed class SessionMetrics
    {
        public const int PointsPerHit = 10;
        public const float ThreeStarsRatio = 0.8f;
        public const float TwoStarsRatio = 0.5f;

        private float reactionSum;
        private int reactionCount;

        public int Hits { get; private set; }
        public int Errors { get; private set; }
        public int Omissions { get; private set; }
        public int Streak { get; private set; }
        public int Score => Hits * PointsPerHit;
        public int Misses => Errors + Omissions;
        public float? AverageReactionSeconds => reactionCount == 0 ? (float?)null : reactionSum / reactionCount;

        // La estrella mínima es refuerzo positivo; no se usa como evaluación clínica.
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

        public void Add(TrialResult trial)
        {
            switch (trial.Outcome)
            {
                case TrialOutcome.Hit:
                    Hits++;
                    Streak++;
                    if (trial.ReactionSeconds.HasValue && trial.ReactionSeconds.Value >= 0f &&
                        !float.IsNaN(trial.ReactionSeconds.Value) && !float.IsInfinity(trial.ReactionSeconds.Value))
                    {
                        reactionSum += trial.ReactionSeconds.Value;
                        reactionCount++;
                    }
                    break;
                case TrialOutcome.Error:
                    Errors++;
                    Streak = 0;
                    break;
                case TrialOutcome.Omission:
                    Omissions++;
                    Streak = 0;
                    break;
            }
        }

        public SessionResult Result(float durationSeconds) => new SessionResult(
            Hits, Errors, Omissions, AverageReactionSeconds, durationSeconds, Score, Stars);

        public void Reset()
        {
            Hits = 0;
            Errors = 0;
            Omissions = 0;
            Streak = 0;
            reactionSum = 0f;
            reactionCount = 0;
        }
    }
}
