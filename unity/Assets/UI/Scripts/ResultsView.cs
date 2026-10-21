// RF-16: estrellas de a una, "¡Muy bien!", puntaje y resumen en tono neutro
using UnityEngine;
using UnityEngine.UIElements;

namespace MoviMente.UI
{
    internal sealed class ResultsView
    {
        private const float FirstStarDelay = 0.25f;
        private const float StarSeconds = 0.3f;
        private const float StarStartScale = 0.4f;

        // Izquierda, centro (la grande) y derecha.
        private readonly VisualElement[] stars;
        private readonly Pulse[] pulses = { new Pulse(), new Pulse(), new Pulse() };
        private readonly Label points;
        private readonly Label summary;
        private readonly Label record;
        private readonly Label best;

        public ResultsView(VisualElement results)
        {
            stars = new[] { results.Q("estrella-izquierda"), results.Q("estrella-centro"), results.Q("estrella-derecha") };
            points = results.Q<Label>("res-puntos");
            summary = results.Q<Label>("res-resumen");
            record = results.Q<Label>("res-record");
            best = results.Q<Label>("res-mejor");
        }

        public void Show(ScoreBoard board, bool newRecord, int bestScore, float now)
        {
            points.text = board.Score.ToString();
            summary.text = HudText.Summary(board.Hits, board.AverageReactionSeconds);
            record.style.display = newRecord ? DisplayStyle.Flex : DisplayStyle.None;
            best.text = $"Mejor puntaje: {bestScore}";

            // Solo se ven las estrellas ganadas: nunca un hueco que marque lo que faltó (RNF-05).
            bool[] earned = Earned(board.Stars);
            int order = 0;
            for (int i = 0; i < stars.Length; i++)
            {
                stars[i].style.display = earned[i] ? DisplayStyle.Flex : DisplayStyle.None;
                if (!earned[i]) continue;
                pulses[i].Start(now, StarSeconds, FirstStarDelay + order * StarSeconds);
                order++;
            }
        }

        public void Tick(float now)
        {
            for (int i = 0; i < stars.Length; i++)
            {
                float t = pulses[i].Progress(now) ?? 1f;
                float scale = Mathf.Lerp(StarStartScale, 1f, t);
                stars[i].style.opacity = t;
                stars[i].style.scale = new Scale(new Vector3(scale, scale, 1f));
            }
        }

        // Una: la del centro; dos: las de los lados; tres: todas.
        private static bool[] Earned(int count) => count switch
        {
            >= 3 => new[] { true, true, true },
            2 => new[] { true, false, true },
            _ => new[] { false, true, false },
        };
    }
}
