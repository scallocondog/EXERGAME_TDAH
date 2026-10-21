// RF-15, CU-06: métricas de aciertos, errores, omisiones, reacción y duración
using MoviMente.Games;
using NUnit.Framework;

namespace MoviMente.Metrics.Tests
{
    public sealed class SessionMetricsTests
    {
        [Test]
        public void ResumeLaPartidaYPromediaSoloAciertosConTiempoValido()
        {
            var metrics = new SessionMetrics();
            metrics.Add(new TrialResult(1, TrialOutcome.Hit, 0.4f, 1f));
            metrics.Add(new TrialResult(1, TrialOutcome.Hit, 0.8f, 2f));
            metrics.Add(new TrialResult(1, TrialOutcome.Error, 0.1f, 3f));
            metrics.Add(new TrialResult(1, TrialOutcome.Omission, null, 4f));

            SessionResult result = metrics.Result(120f);
            Assert.That(result.Hits, Is.EqualTo(2));
            Assert.That(result.Errors, Is.EqualTo(1));
            Assert.That(result.Omissions, Is.EqualTo(1));
            Assert.That(result.AverageReactionSeconds, Is.EqualTo(0.6f).Within(0.0001f));
            Assert.That(result.DurationSeconds, Is.EqualTo(120f));
            Assert.That(result.Score, Is.EqualTo(20));
            Assert.That(result.Stars, Is.EqualTo(2));
        }

        [Test]
        public void ReiniciarDescartaLasMetricasDeLaPartidaAnterior()
        {
            var metrics = new SessionMetrics();
            metrics.Add(new TrialResult(1, TrialOutcome.Hit, 0.5f, 1f));
            metrics.Reset();

            SessionResult result = metrics.Result(0f);
            Assert.That(result.Hits, Is.Zero);
            Assert.That(result.Score, Is.Zero);
            Assert.That(result.AverageReactionSeconds, Is.Null);
            Assert.That(metrics.Streak, Is.Zero);
        }
    }
}
