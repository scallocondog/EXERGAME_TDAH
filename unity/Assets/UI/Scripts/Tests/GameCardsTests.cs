// RF-10: pruebas de cómo se presenta cada minijuego
using NUnit.Framework;

namespace MoviMente.UI.Tests
{
    public sealed class GameCardsTests
    {
        [Test]
        public void AtrapaLoCorrecto_SeMuestraInclinando()
        {
            GameCard card = GameCards.For("atrapa-lo-correcto");
            Assert.That(card.Name, Is.EqualTo("Atrapa lo correcto"));
            Assert.That(card.Instruction, Is.EqualTo("Inclina para mover la canasta"));
            Assert.That(card.Demo, Is.EqualTo(GestureDemo.Tilt));
        }

        [Test]
        public void CortaSinFallar_SeMuestraConSwing()
        {
            GameCard card = GameCards.For("corta-sin-fallar");
            Assert.That(card.Name, Is.EqualTo("Corta sin fallar"));
            Assert.That(card.Instruction, Is.EqualTo("Corta las frutas, no las piedras"));
            Assert.That(card.Demo, Is.EqualTo(GestureDemo.Swing));
        }

        [Test]
        public void UnMinijuegoNuevo_SeVeAunqueNoTengaFicha()
        {
            GameCard card = GameCards.For("juego-nuevo");
            Assert.That(card.Name, Is.EqualTo("juego-nuevo"));
            Assert.That(card.Instruction, Is.Not.Empty);
            Assert.That(card.Demo, Is.EqualTo(GestureDemo.None));
        }

        [Test]
        public void LaClaseDelIcono_LlevaElId()
        {
            Assert.That(GameCards.ClassFor("atrapa-lo-correcto"), Is.EqualTo("juego--atrapa-lo-correcto"));
        }
    }
}
