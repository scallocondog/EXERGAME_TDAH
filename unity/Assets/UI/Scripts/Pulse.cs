// RNF-05: animaciones cortas que corren una vez y se quedan quietas (docs/ux/sistema-visual.md §5)
namespace MoviMente.UI
{
    public sealed class Pulse
    {
        private float startedAt;
        private float duration;
        private bool running;

        public void Start(float now, float seconds, float delay = 0f)
        {
            startedAt = now + delay;
            duration = seconds;
            running = true;
        }

        public void Stop() => running = false;

        // 0 mientras espera su retraso, luego de 0 a 1; null cuando no corre.
        public float? Progress(float now)
        {
            if (!running) return null;
            float t = (now - startedAt) / duration;
            if (t >= 1f)
            {
                running = false;
                return null;
            }
            return t < 0f ? 0f : t;
        }
    }
}
