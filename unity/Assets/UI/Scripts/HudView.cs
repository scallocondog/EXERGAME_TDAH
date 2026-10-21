// RF-15, RNF-05: el HUD de la partida (tiempo y puntaje) y el aviso de acierto o error
using MoviMente.Games;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoviMente.UI
{
    internal sealed class HudView
    {
        // docs/ux/sistema-visual.md §5: el error es más corto y más suave que el acierto.
        private const float HitFlashSeconds = 0.2f;
        private const float MissFlashSeconds = 0.15f;
        private const float PopupSeconds = 0.6f;
        private const float PopupRise = 60f;
        private const float StreakSeconds = 0.9f;
        // Fracciones de StreakSeconds: entra en el primer 15 % y se va en el último 20 %.
        private const float StreakFadeIn = 0.15f;
        private const float StreakFadeOut = 0.2f;
        private const float ScoreCountSeconds = 0.2f;
        // Con menos de un cuarto del tiempo, la barra pasa a amarillo; nunca a rojo.
        private const float LowTimeFraction = 0.25f;

        private readonly Label clock;
        private readonly VisualElement bar;
        private readonly Label points;
        private readonly Label popup;
        private readonly Label streak;
        private readonly VisualElement flash;
        private readonly Pulse flashPulse = new Pulse();
        private readonly Pulse popupPulse = new Pulse();
        private readonly Pulse streakPulse = new Pulse();
        private readonly Pulse countPulse = new Pulse();

        private int shownSeconds = -1;
        private int shownPoints;
        private int countFrom;
        private int countTo;

        public HudView(VisualElement hud)
        {
            clock = hud.Q<Label>("reloj");
            bar = hud.Q("barra-relleno");
            points = hud.Q<Label>("puntaje");
            popup = hud.Q<Label>("mas-diez");
            streak = hud.Q<Label>("racha");
            flash = hud.Q("destello");
        }

        public void Reset()
        {
            shownSeconds = -1;
            shownPoints = 0;
            countFrom = 0;
            countTo = 0;
            points.text = "0";
            flashPulse.Stop();
            popupPulse.Stop();
            streakPulse.Stop();
            countPulse.Stop();
        }

        // La omisión no se marca: igual que no suena ni vibra, el jugador no hizo nada.
        public void Feedback(TrialOutcome outcome, ScoreBoard board, float now)
        {
            if (outcome == TrialOutcome.Omission) return;

            bool hit = outcome == TrialOutcome.Hit;
            flash.EnableInClassList("destello--acierto", hit);
            flash.EnableInClassList("destello--error", !hit);
            flashPulse.Start(now, hit ? HitFlashSeconds : MissFlashSeconds);
            if (!hit) return;

            countFrom = countTo;
            countTo = board.Score;
            countPulse.Start(now, ScoreCountSeconds);
            popupPulse.Start(now, PopupSeconds);
            if (!board.ShowsStreak) return;
            streak.text = $"{board.Streak} seguidos";
            streakPulse.Start(now, StreakSeconds);
        }

        public void Tick(GameSession session, float now)
        {
            PaintClock(session);

            // El borde entra y sale.
            float? f = flashPulse.Progress(now);
            flash.style.opacity = f.HasValue ? 1f - Mathf.Abs(f.Value * 2f - 1f) : 0f;

            // El puntaje sube contando, no salta.
            float count = countPulse.Progress(now) ?? 1f;
            int shown = Mathf.RoundToInt(Mathf.Lerp(countFrom, countTo, count));
            if (shown != shownPoints)
            {
                shownPoints = shown;
                points.text = shown.ToString();
            }

            // "+10" sube y se desvanece.
            float? p = popupPulse.Progress(now);
            popup.style.opacity = p.HasValue ? 1f - p.Value : 0f;
            popup.style.translate = new Translate(0f, -PopupRise * (p ?? 0f));

            // "3 seguidos" entra, se queda un momento y se va.
            float? s = streakPulse.Progress(now);
            streak.style.opacity = s.HasValue ? Mathf.Clamp01(Mathf.Min(s.Value / StreakFadeIn, (1f - s.Value) / StreakFadeOut)) : 0f;
        }

        private void PaintClock(GameSession session)
        {
            int seconds = Mathf.CeilToInt(session.RemainingSeconds);
            if (seconds == shownSeconds) return;
            shownSeconds = seconds;
            clock.text = HudText.Clock(session.RemainingSeconds);
            float duration = session.Definition.Profile(session.Difficulty).DurationSeconds;
            float left = duration > 0f ? session.RemainingSeconds / duration : 0f;
            bar.style.width = Length.Percent(left * 100f);
            bar.EnableInClassList("poco-tiempo", left < LowTimeFraction);
        }
    }
}
