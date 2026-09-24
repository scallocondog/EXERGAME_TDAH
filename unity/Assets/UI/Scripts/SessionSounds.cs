// RNF-06, CU-06: qué suena en cada momento de la partida (docs/ux/sistema-visual.md §7)
using MoviMente.Games;

namespace MoviMente.UI
{
    public enum SessionCue
    {
        None,
        Hit,
        Error,
        Countdown,
        Go,
        WaitingForPad,
        WellDone,
    }

    public static class SessionSounds
    {
        // La omisión no suena, igual que no vibra: el jugador no hizo nada.
        public static SessionCue For(TrialOutcome outcome) => outcome switch
        {
            TrialOutcome.Hit => SessionCue.Hit,
            TrialOutcome.Error => SessionCue.Error,
            _ => SessionCue.None,
        };

        // Toda reanudación pasa por la cuenta regresiva, así que "¡Ya!" cae
        // siempre justo cuando se vuelve a jugar. La pausa del jugador no habla:
        // la voz es solo para cuando falta el mando.
        public static SessionCue For(GameState state, bool waitingForPad) => state switch
        {
            GameState.Countdown => SessionCue.Countdown,
            GameState.Playing => SessionCue.Go,
            GameState.Paused when waitingForPad => SessionCue.WaitingForPad,
            GameState.Finished => SessionCue.WellDone,
            _ => SessionCue.None,
        };
    }
}
