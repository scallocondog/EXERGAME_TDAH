// RF-15, RF-16: los números que la TV escribe, en español y sin decimales de más
using System;
using System.Globalization;

namespace MoviMente.UI
{
    public static class HudText
    {
        // Redondea hacia arriba: el reloj no marca 0:00 mientras quede tiempo.
        public static string Clock(float seconds)
        {
            int total = (int)Math.Ceiling(Math.Max(0f, seconds));
            return $"{total / 60}:{total % 60:00}";
        }

        // 3, 2, 1: nunca muestra 0, porque en ese instante ya empieza el juego.
        public static string Countdown(float seconds) => Math.Max(1, (int)Math.Ceiling(seconds)).ToString(CultureInfo.InvariantCulture);

        // "18 aciertos · respondiste en 0,6 s", en tono neutro (docs/ux/sistema-visual.md §6).
        public static string Summary(int hits, float? reactionSeconds)
        {
            string count = hits == 1 ? "1 acierto" : $"{hits} aciertos";
            if (!reactionSeconds.HasValue) return count;
            string reaction = reactionSeconds.Value.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
            return $"{count} · respondiste en {reaction} s";
        }
    }
}
