// RF-10: la demostración del gesto, lo único que se anima en bucle (RNF-05)
using UnityEngine;
using UnityEngine.UIElements;

namespace MoviMente.UI
{
    internal sealed class InstructionsView
    {
        // docs/ux/sistema-visual.md §5: ciclo de 2,4 s, celular ±18° y canasta ±170 px.
        private const float CycleSeconds = 2.4f;
        private const float TiltDegrees = 18f;
        private const float SlidePixels = 170f;
        // Swing: el celular va de -35° a +35° de golpe y vuelve despacio.
        private const float SwingDegrees = 35f;
        private const float FruitRisePixels = 260f;
        private const float HalfDriftPixels = 120f;
        private const float HalfDropPixels = 70f;
        private const float HalfTurnDegrees = 25f;

        private readonly VisualElement demo;
        private readonly VisualElement phone;
        private readonly VisualElement basket;
        private readonly VisualElement leftHalf;
        private readonly VisualElement rightHalf;
        private readonly VisualElement cut;
        private GestureDemo shown;

        public InstructionsView(VisualElement instructions)
        {
            demo = instructions.Q("demo");
            phone = instructions.Q("demo-celular");
            basket = instructions.Q("demo-canasta");
            leftHalf = instructions.Q("demo-fruta-izquierda");
            rightHalf = instructions.Q("demo-fruta-derecha");
            cut = instructions.Q("demo-tajo");
        }

        // Sin demostración conocida, la caja no se muestra: mejor nada que un gesto equivocado.
        public void Show(GestureDemo next)
        {
            shown = next;
            demo.style.display = next == GestureDemo.None ? DisplayStyle.None : DisplayStyle.Flex;
            demo.EnableInClassList("demo--swing", next == GestureDemo.Swing);
        }

        public void Tick(float now)
        {
            if (shown == GestureDemo.Tilt) TickTilt(now);
            else if (shown == GestureDemo.Swing) TickSwing(now);
        }

        // Un coseno da la ida y vuelta suave de la maqueta: arranca inclinado a la
        // izquierda y la canasta sigue al celular.
        private void TickTilt(float now)
        {
            float wave = -Mathf.Cos(2f * Mathf.PI * now / CycleSeconds);
            phone.style.rotate = new Rotate(new Angle(TiltDegrees * wave, AngleUnit.Degree));
            basket.style.translate = new Translate(SlidePixels * wave, 0f);
        }

        // La fruta sube a la franja, el celular da el latigazo, aparece el tajo, la
        // fruta se parte y el celular vuelve despacio a su lugar.
        private void TickSwing(float now)
        {
            float p = Mathf.Repeat(now, CycleSeconds) / CycleSeconds;

            phone.style.rotate = new Rotate(new Angle(SwingDegrees * Swing(p), AngleUnit.Degree));

            float rise = 1f - EaseOut(Mathf.InverseLerp(0f, 0.35f, p));
            float split = Mathf.InverseLerp(0.45f, 0.75f, p);
            float fruitOpacity = p < 0.45f ? 1f : 1f - split;
            PaintHalf(leftHalf, -1f, rise, split, fruitOpacity);
            PaintHalf(rightHalf, 1f, rise, split, fruitOpacity);

            cut.style.opacity = p < 0.4f ? 0f : 1f - Mathf.InverseLerp(0.4f, 0.62f, p);
        }

        // De -1 a 1: quieto, latigazo rápido, pausa arriba y vuelta despacio.
        private static float Swing(float p)
        {
            if (p < 0.35f) return -1f;
            if (p < 0.45f) return Mathf.Lerp(-1f, 1f, EaseIn(Mathf.InverseLerp(0.35f, 0.45f, p)));
            if (p < 0.6f) return 1f;
            return Mathf.Lerp(1f, -1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.6f, 0.95f, p)));
        }

        private static void PaintHalf(VisualElement half, float side, float rise, float split, float opacity)
        {
            half.style.translate = new Translate(side * HalfDriftPixels * split,
                FruitRisePixels * rise + HalfDropPixels * split * split);
            half.style.rotate = new Rotate(new Angle(side * HalfTurnDegrees * split, AngleUnit.Degree));
            half.style.opacity = opacity;
        }

        private static float EaseIn(float t) => t * t;

        private static float EaseOut(float t) => 1f - (1f - t) * (1f - t);
    }
}
