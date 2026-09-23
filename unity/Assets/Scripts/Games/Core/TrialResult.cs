// RF-15, RF-18: resultado de cada estímulo, insumo de métricas y vibración
namespace MoviMente.Games
{
    public enum TrialOutcome
    {
        Hit,
        Error,
        Omission,
    }

    public readonly struct TrialResult
    {
        public readonly int Slot;
        public readonly TrialOutcome Outcome;
        // Segundos entre el estímulo y el gesto, en tiempo de partida (sin pausas).
        // Null en las omisiones.
        public readonly float? ReactionSeconds;
        public readonly float ElapsedSeconds;

        public TrialResult(int slot, TrialOutcome outcome, float? reactionSeconds, float elapsedSeconds)
        {
            Slot = slot;
            Outcome = outcome;
            ReactionSeconds = reactionSeconds;
            ElapsedSeconds = elapsedSeconds;
        }
    }
}
