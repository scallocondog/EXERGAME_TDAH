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

        private readonly VisualElement phone;
        private readonly VisualElement basket;

        public InstructionsView(VisualElement instructions)
        {
            phone = instructions.Q("demo-celular");
            basket = instructions.Q("demo-canasta");
        }

        // Un coseno da la ida y vuelta suave de la maqueta: arranca inclinado a la
        // izquierda y la canasta sigue al celular.
        public void Tick(float now)
        {
            float wave = -Mathf.Cos(2f * Mathf.PI * now / CycleSeconds);
            phone.style.rotate = new Rotate(new Angle(TiltDegrees * wave, AngleUnit.Degree));
            basket.style.translate = new Translate(SlidePixels * wave, 0f);
        }
    }
}
