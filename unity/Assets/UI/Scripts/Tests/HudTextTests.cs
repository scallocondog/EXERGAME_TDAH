// RF-15, RF-16: pruebas de los números que escribe la TV
using NUnit.Framework;

namespace MoviMente.UI.Tests
{
    public sealed class HudTextTests
    {
        [TestCase(180f, "3:00")]
        [TestCase(179.2f, "3:00")]
        [TestCase(61f, "1:01")]
        [TestCase(59.01f, "1:00")]
        [TestCase(0.2f, "0:01")]
        [TestCase(0f, "0:00")]
        [TestCase(-3f, "0:00")]
        public void Reloj_EnMinutosYSegundos(float seconds, string expected)
        {
            Assert.That(HudText.Clock(seconds), Is.EqualTo(expected));
        }

        [TestCase(3f, "3")]
        [TestCase(2.4f, "3")]
        [TestCase(2f, "2")]
        [TestCase(0.01f, "1")]
        [TestCase(0f, "1")]
        public void CuentaRegresiva_DeTresAUno(float seconds, string expected)
        {
            Assert.That(HudText.Countdown(seconds), Is.EqualTo(expected));
        }

        [TestCase(18, 0.6f, "18 aciertos · respondiste en 0,6 s")]
        [TestCase(1, 1.26f, "1 acierto · respondiste en 1,3 s")]
        public void Resumen_ConTiempoDeReaccion(int hits, float reaction, string expected)
        {
            Assert.That(HudText.Summary(hits, reaction), Is.EqualTo(expected));
        }

        [TestCase(0, "0 aciertos")]
        [TestCase(1, "1 acierto")]
        public void Resumen_SinTiempoDeReaccion(int hits, string expected)
        {
            Assert.That(HudText.Summary(hits, null), Is.EqualTo(expected));
        }
    }
}
