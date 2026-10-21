// RF-16, RNF-05: pruebas de lo que la TV muestra del desempeño
using MoviMente.Games;
using NUnit.Framework;

namespace MoviMente.UI.Tests
{
    public sealed class ScoreBoardTests
    {
        private ScoreBoard board;

        [SetUp]
        public void SetUp() => board = new ScoreBoard();

        [Test]
        public void CadaAcierto_SumaDiezPuntos()
        {
            Add(TrialOutcome.Hit, 3);
            Assert.That(board.Score, Is.EqualTo(30));
            Assert.That(board.Hits, Is.EqualTo(3));
        }

        [Test]
        public void ElError_NoRestaPuntos_SoloCortaLaRacha()
        {
            Add(TrialOutcome.Hit, 3);
            Add(TrialOutcome.Error, 1);
            Assert.That(board.Score, Is.EqualTo(30));
            Assert.That(board.Streak, Is.EqualTo(0));
        }

        [Test]
        public void LaOmision_TambienCortaLaRacha()
        {
            Add(TrialOutcome.Hit, 2);
            Add(TrialOutcome.Omission, 1);
            Assert.That(board.Streak, Is.EqualTo(0));
            Assert.That(board.Misses, Is.EqualTo(1));
        }

        [Test]
        public void LaRacha_SeMuestraDesdeElTercerAciertoSeguido()
        {
            Add(TrialOutcome.Hit, 2);
            Assert.That(board.ShowsStreak, Is.False);
            Add(TrialOutcome.Hit, 1);
            Assert.That(board.ShowsStreak, Is.True);
        }

        [Test]
        public void SinJugadas_HayUnaEstrella()
        {
            Assert.That(board.Stars, Is.EqualTo(1));
        }

        [TestCase(10, 0, 3)]
        [TestCase(8, 2, 3)]
        [TestCase(7, 3, 2)]
        [TestCase(5, 5, 2)]
        [TestCase(4, 6, 1)]
        [TestCase(0, 10, 1)]
        public void Estrellas_SegunLaProporcionDeAciertos(int hits, int misses, int expected)
        {
            Add(TrialOutcome.Hit, hits);
            Add(TrialOutcome.Error, misses);
            Assert.That(board.Stars, Is.EqualTo(expected));
        }

        [Test]
        public void ElTiempoDeReaccion_PromediaSoloLosAciertos()
        {
            board.Add(new TrialResult(1, TrialOutcome.Hit, 0.4f, 1f));
            board.Add(new TrialResult(1, TrialOutcome.Hit, 0.8f, 2f));
            board.Add(new TrialResult(1, TrialOutcome.Error, 0.1f, 3f));
            Assert.That(board.AverageReactionSeconds, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void SinTiempos_NoHayPromedio()
        {
            board.Add(new TrialResult(1, TrialOutcome.Hit, null, 1f));
            Assert.That(board.AverageReactionSeconds, Is.Null);
        }

        [Test]
        public void AlReintentar_SeEmpiezaDeCero()
        {
            board.Add(new TrialResult(1, TrialOutcome.Hit, 0.5f, 1f));
            board.Reset();
            Assert.That(board.Score, Is.Zero);
            Assert.That(board.Hits, Is.Zero);
            Assert.That(board.Streak, Is.Zero);
            Assert.That(board.AverageReactionSeconds, Is.Null);
        }

        private void Add(TrialOutcome outcome, int times)
        {
            for (int i = 0; i < times; i++) board.Add(new TrialResult(1, outcome, null, i));
        }
    }
}
