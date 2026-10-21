// RF-10, RF-16, RF-19, CU-08: qué pantalla muestra la TV en cada momento de la partida
using MoviMente.Games;

namespace MoviMente.UI
{
    public enum SessionScreen
    {
        None,
        // Todavía falta un mando: el código y el QR para unirse (RF-01).
        Room,
        Instructions,
        Countdown,
        Playing,
        Pause,
        WaitingForPad,
        Results,
    }

    public static class SessionScreens
    {
        public static SessionScreen For(GameState state, bool waitingForPad, bool padsReady) => state switch
        {
            GameState.Instructions => padsReady ? SessionScreen.Instructions : SessionScreen.Room,
            GameState.Countdown => SessionScreen.Countdown,
            GameState.Playing => SessionScreen.Playing,
            GameState.Paused => waitingForPad ? SessionScreen.WaitingForPad : SessionScreen.Pause,
            GameState.Finished => SessionScreen.Results,
            _ => SessionScreen.None,
        };

        // La pausa y el mando desconectado son capas: el HUD sigue detrás para no perder el contexto.
        public static bool ShowsHud(SessionScreen screen) =>
            screen is SessionScreen.Playing or SessionScreen.Pause or SessionScreen.WaitingForPad;
    }
}
