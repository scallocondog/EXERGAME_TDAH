// RF-15: resumen inmutable de una partida completada
namespace MoviMente.Metrics
{
    public readonly struct SessionResult
    {
        public readonly int Hits;
        public readonly int Errors;
        public readonly int Omissions;
        public readonly float? AverageReactionSeconds;
        public readonly float DurationSeconds;
        public readonly int Score;
        public readonly int Stars;

        public SessionResult(int hits, int errors, int omissions, float? averageReactionSeconds,
            float durationSeconds, int score, int stars)
        {
            Hits = hits;
            Errors = errors;
            Omissions = omissions;
            AverageReactionSeconds = averageReactionSeconds;
            DurationSeconds = durationSeconds;
            Score = score;
            Stars = stars;
        }
    }
}
