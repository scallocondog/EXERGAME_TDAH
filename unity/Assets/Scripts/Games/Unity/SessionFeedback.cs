// RF-07, RF-18: qué se le avisa al mando según lo que pasa en la partida
using MoviMente.Net;

namespace MoviMente.Games
{
    public static class SessionFeedback
    {
        // La omisión no vibra: el jugador no hizo nada que sentir.
        public static string HapticFor(TrialOutcome outcome) => outcome switch
        {
            TrialOutcome.Hit => HapticPattern.Hit,
            TrialOutcome.Error => HapticPattern.Miss,
            _ => null,
        };

        // Solo los estados que el mando muestra; el resto no cambia su pantalla.
        public static string PadStateFor(GameState state) => state switch
        {
            GameState.Countdown or GameState.Playing => PadScreenState.Playing,
            GameState.Paused => PadScreenState.Paused,
            _ => null,
        };
    }
}
