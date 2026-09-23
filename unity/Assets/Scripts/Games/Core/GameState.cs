// RF-10, RF-19, CU-06: etapas de una partida
namespace MoviMente.Games
{
    public enum GameState
    {
        Instructions,
        Countdown,
        Playing,
        Paused,
        Finished,
        // Salió desde la pausa: no se guarda puntaje (RF-19).
        Abandoned,
    }
}
