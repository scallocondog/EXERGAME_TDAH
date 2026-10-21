// RF-08: pruebas del foco que se mueve inclinando el celular
using System;
using MoviMente.Gestures;
using NUnit.Framework;

namespace MoviMente.UI.Tests
{
    public sealed class FocusListTests
    {
        [Test]
        public void AlEntrar_ElFocoEmpiezaDondeSePide()
        {
            var focus = new FocusList(3);
            focus.Reset(2);
            Assert.That(focus.Index, Is.EqualTo(2));
        }

        [Test]
        public void UnFocoFueraDeRango_SeAjustaAlMenu()
        {
            var focus = new FocusList(3);
            focus.Reset(7);
            Assert.That(focus.Index, Is.EqualTo(2));
        }

        [Test]
        public void Inclinar_MueveUnaOpcionPorVez()
        {
            var focus = new FocusList(3);
            Assert.That(focus.Move(1, 0f), Is.True);
            Assert.That(focus.Index, Is.EqualTo(1));
        }

        [Test]
        public void EnLosExtremos_ElFocoNoDaLaVuelta()
        {
            var focus = new FocusList(2);
            Assert.That(focus.Move(-1, 0f), Is.False);
            Assert.That(focus.Index, Is.EqualTo(0));
            focus.Move(1, 1f);
            Assert.That(focus.Move(1, 2f), Is.False);
            Assert.That(focus.Index, Is.EqualTo(1));
        }

        [Test]
        public void ElEnfriamiento_FrenaUnaInclinacionLarga()
        {
            var focus = new FocusList(4);
            focus.Move(1, 0f);
            Assert.That(focus.Move(1, FocusList.CooldownSeconds - 0.01f), Is.False);
            Assert.That(focus.Move(1, FocusList.CooldownSeconds), Is.True);
            Assert.That(focus.Index, Is.EqualTo(2));
        }

        [Test]
        public void ChocarConElExtremo_NoGastaElEnfriamiento()
        {
            var focus = new FocusList(3);
            focus.Move(-1, 0f);
            Assert.That(focus.Move(1, 0.1f), Is.True);
        }

        [Test]
        public void AlVolverAEntrar_SeOlvidaElEnfriamiento()
        {
            var focus = new FocusList(3);
            focus.Move(1, 0f);
            focus.Reset(0);
            Assert.That(focus.Move(1, 0.1f), Is.True);
        }

        [TestCase(GestureType.TiltLeft, -1)]
        [TestCase(GestureType.TiltForward, -1)]
        [TestCase(GestureType.TiltRight, 1)]
        [TestCase(GestureType.TiltBack, 1)]
        [TestCase(GestureType.Swing, 0)]
        [TestCase(GestureType.Shake, 0)]
        [TestCase(GestureType.Steady, 0)]
        public void CadaInclinacion_MueveHaciaSuLado(GestureType gesture, int expected)
        {
            Assert.That(FocusList.StepFor(gesture), Is.EqualTo(expected));
        }

        [Test]
        public void UnMenuSinOpciones_NoSePuedeCrear()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new FocusList(0));
        }
    }
}
