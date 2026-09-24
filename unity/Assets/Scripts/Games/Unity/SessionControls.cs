// RF-10, RF-19: los botones del mando que la partida entiende por sí sola
using MoviMente.Net;

namespace MoviMente.Games
{
    // Las opciones del menú de pausa (reanudar, reintentar, salir) las elige la
    // UI con inclinar + confirmar (RF-08) y llama a GameSession directamente.
    public static class SessionControls
    {
        public static void OnButton(GameSession session, string action)
        {
            switch (action)
            {
                case PadButton.Pause when session.State == GameState.Countdown || session.State == GameState.Playing:
                    session.PauseByPlayer();
                    break;
                // Pausar otra vez en la pausa la cierra; si falta un mando, sigue esperándolo.
                case PadButton.Pause when session.State == GameState.Paused:
                    session.Resume();
                    break;
                // "Toca el botón para empezar" de la pantalla de instrucciones.
                case PadButton.Confirm when session.State == GameState.Instructions:
                    session.BeginCountdown();
                    break;
            }
        }
    }
}
