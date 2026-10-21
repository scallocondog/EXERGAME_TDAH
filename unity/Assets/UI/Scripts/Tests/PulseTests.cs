// RNF-05: pruebas de las animaciones cortas que corren una sola vez
using NUnit.Framework;

namespace MoviMente.UI.Tests
{
    public sealed class PulseTests
    {
        [Test]
        public void AntesDeEmpezar_NoCorre()
        {
            Assert.That(new Pulse().Progress(0f), Is.Null);
        }

        [Test]
        public void MientrasDura_AvanzaDeCeroAUno()
        {
            var pulse = new Pulse();
            pulse.Start(10f, 0.2f);
            Assert.That(pulse.Progress(10.1f), Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void ConRetraso_EsperaEnCero()
        {
            var pulse = new Pulse();
            pulse.Start(0f, 0.3f, delay: 0.5f);
            Assert.That(pulse.Progress(0.2f), Is.Zero);
        }

        [Test]
        public void AlTerminar_SeQuedaQuieta()
        {
            var pulse = new Pulse();
            pulse.Start(0f, 0.2f);
            Assert.That(pulse.Progress(0.2f), Is.Null);
            Assert.That(pulse.Progress(0.1f), Is.Null);
        }
    }
}
