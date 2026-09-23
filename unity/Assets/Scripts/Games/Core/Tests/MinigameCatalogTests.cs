// RF-09, RNF-05, RNF-08: pruebas del registro de minijuegos
using System;
using MoviMente.Gestures;
using NUnit.Framework;

namespace MoviMente.Games.Tests
{
    public sealed class MinigameCatalogTests
    {
        private MinigameCatalog catalog;

        [SetUp]
        public void SetUp() => catalog = new MinigameCatalog();

        [Test]
        public void ListaLosMinijuegosEnOrdenDeRegistro()
        {
            catalog.Register(TestGames.Create(null, "atrapa"));
            catalog.Register(TestGames.Create(null, "corta"));

            Assert.That(catalog.All.Count, Is.EqualTo(2));
            Assert.That(catalog.All[0].Id, Is.EqualTo("atrapa"));
            Assert.That(catalog.Find("corta"), Is.SameAs(catalog.All[1]));
            Assert.That(catalog.Find("no-existe"), Is.Null);
        }

        [Test]
        public void RechazaUnIdRepetido()
        {
            catalog.Register(TestGames.Create(null, "atrapa"));

            Assert.Throws<ArgumentException>(() => catalog.Register(TestGames.Create(null, "atrapa")));
        }

        [TestCase(179f)]
        [TestCase(301f)]
        public void RechazaPartidasFueraDe3a5Minutos(float duration)
        {
            Assert.Throws<ArgumentException>(() => catalog.Register(TestGames.Create(null, duration: duration)));
            Assert.That(catalog.All, Is.Empty);
        }

        [Test]
        public void RechazaSwingYSacudirEnElMismoMinijuego()
        {
            var easy = new DifficultyProfile(180f, GestureType.Shake);
            var hard = new DifficultyProfile(180f, GestureType.Swing);
            var mixed = new MinigameDefinition("mixto", "Game_Mixto", () => new FakeRules(), easy, easy, hard);

            Assert.Throws<ArgumentException>(() => catalog.Register(mixed));
        }

        [Test]
        public void AceptaSwingSoloEnDificil()
        {
            var directions = new DifficultyProfile(240f,
                GestureType.TiltLeft, GestureType.TiltRight, GestureType.TiltForward, GestureType.TiltBack);
            var withSwing = new DifficultyProfile(240f,
                GestureType.TiltLeft, GestureType.TiltRight, GestureType.TiltForward, GestureType.TiltBack,
                GestureType.Swing);
            var secuencia = new MinigameDefinition("secuencia", "Game_SigueLaSecuencia", () => new FakeRules(),
                directions, directions, withSwing);

            catalog.Register(secuencia);
            Assert.That(catalog.Find("secuencia").Profile(Difficulty.Hard).Allows(GestureType.Swing), Is.True);
            Assert.That(catalog.Find("secuencia").Profile(Difficulty.Easy).Allows(GestureType.Swing), Is.False);
        }
    }
}
