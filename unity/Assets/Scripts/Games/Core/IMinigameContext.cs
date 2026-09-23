// RF-11 a RF-14, RNF-08: lo único que las reglas de un minijuego ven de la partida
using MoviMente.Gestures;

namespace MoviMente.Games
{
    public interface IMinigameContext
    {
        Difficulty Difficulty { get; }
        // Tiempo jugado, sin contar pausas ni cuentas regresivas.
        float ElapsedSeconds { get; }
        float RemainingSeconds { get; }

        TiltAxes GetTilt(int slot);

        void Report(TrialOutcome outcome, int slot, float? reactionSeconds = null);

        // Para minijuegos por rondas que terminan antes del tope de tiempo.
        void Finish();
    }
}
