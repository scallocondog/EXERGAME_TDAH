// CU-06: 3, 2, 1; cada número entra con escala y fundido, sin "¡YA!" con destello (RNF-05)
using UnityEngine;
using UnityEngine.UIElements;

namespace MoviMente.UI
{
    internal sealed class CountdownView
    {
        private const float EnterSeconds = 0.22f;
        private const float EnterScale = 0.9f;

        private readonly VisualElement circle;
        private readonly Label number;
        private readonly Pulse pulse = new Pulse();
        private string shown;

        public CountdownView(VisualElement countdown)
        {
            circle = countdown.Q("cuenta-circulo");
            number = countdown.Q<Label>("cuenta-numero");
        }

        // Al volver de una pausa, la cuenta arranca otra vez desde el 3.
        public void Reset() => shown = null;

        public void Tick(float remainingSeconds, float now)
        {
            string text = HudText.Countdown(remainingSeconds);
            if (text != shown)
            {
                shown = text;
                number.text = text;
                pulse.Start(now, EnterSeconds);
            }

            float t = pulse.Progress(now) ?? 1f;
            float scale = Mathf.Lerp(EnterScale, 1f, t);
            circle.style.opacity = t;
            circle.style.scale = new Scale(new Vector3(scale, scale, 1f));
        }
    }
}
