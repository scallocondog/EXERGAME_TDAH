// RF-11: pruebas del Wandering de los objetos en difícil
using System;
using System.Collections.Generic;
using System.Linq;
using MoviMente.Gestures;
using NUnit.Framework;

namespace MoviMente.Games.AtrapaLoCorrecto.Tests
{
    public sealed class WanderingTests
    {
        private const float Frame = 1f / 60f;
        private const float Margin = 0.9f;

        private AtrapaLoCorrectoRules rules;
        private GameSession session;
        private float tiltX;
        private List<TrialResult> trials;
        // Recorrido lateral de cada objeto, cuadro a cuadro.
        private Dictionary<int, List<float>> paths;

        private void Start(Difficulty difficulty, int seed = 7)
        {
            var definition = AtrapaLoCorrectoGame.CreateDefinition(() => new Random(seed), r => rules = r);
            session = new GameSession(definition, difficulty, _ => new TiltAxes(tiltX, 0f));
            trials = new List<TrialResult>();
            paths = new Dictionary<int, List<float>>();
            session.TrialReported += trials.Add;
            session.BeginCountdown();
            session.Tick(GameSession.CountdownSeconds);
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame)
            {
                session.Tick(Frame);
                foreach (FallingItem item in rules.Items)
                {
                    if (!paths.TryGetValue(item.Id, out List<float> path)) paths[item.Id] = path = new List<float>();
                    path.Add(item.X);
                }
            }
        }

        [TestCase(Difficulty.Easy)]
        [TestCase(Difficulty.Medium)]
        public void FacilYMedio_CaenRecto(Difficulty difficulty)
        {
            Start(difficulty);
            Run(20f);

            Assert.That(paths.Count, Is.GreaterThan(5));
            Assert.That(paths.Values.All(path => path.All(x => x == path[0])), Is.True);
        }

        [Test]
        public void Dificil_LosObjetosSeMecenSinSalirDelCampoNiSaltar()
        {
            Start(Difficulty.Hard);
            Run(30f);

            // Solo los que cayeron completos: tuvieron tiempo de pasear.
            List<List<float>> complete = paths.Values.Where(p => p.Count > 100).ToList();
            Assert.That(complete.Count, Is.GreaterThan(10));

            float maxStep = AtrapaTuning.Hard.WanderSpeed * Frame + 1e-4f;
            foreach (List<float> path in complete)
            {
                Assert.That(path.All(x => Math.Abs(x) <= Margin + 1e-4f), Is.True, "salió del campo");
                for (int i = 1; i < path.Count; i++)
                {
                    Assert.That(Math.Abs(path[i] - path[i - 1]), Is.LessThanOrEqualTo(maxStep), "saltó de lado");
                }
            }
            // Pasear de verdad: la mayoría se aleja bastante de donde apareció.
            int wandered = complete.Count(p => p.Max() - p.Min() > 0.1f);
            Assert.That(wandered, Is.GreaterThan(complete.Count / 2));
        }

        [Test]
        public void Dificil_LaCaidaMantieneSuVelocidad()
        {
            Start(Difficulty.Hard);
            Run(3f);
            FallingItem item = rules.Items.First();
            float before = item.Y;

            session.Tick(Frame);

            Assert.That(before - item.Y, Is.EqualTo(Frame / AtrapaTuning.Hard.FallSeconds).Within(1e-5f));
        }

        [Test]
        public void Dificil_MismaSemillaMismoRecorrido()
        {
            Start(Difficulty.Hard, seed: 21);
            Run(10f);
            float[] first = paths.OrderBy(p => p.Key).SelectMany(p => p.Value).ToArray();

            Start(Difficulty.Hard, seed: 21);
            Run(10f);
            float[] second = paths.OrderBy(p => p.Key).SelectMany(p => p.Value).ToArray();

            Assert.That(second, Is.EqualTo(first));
        }

        [Test]
        public void Dificil_SiguiendoAlObjetoSeLoAtrapa()
        {
            Start(Difficulty.Hard);
            FallingItem target = null;
            for (int i = 0; i < 5000 && target == null; i++)
            {
                session.Tick(Frame);
                target = rules.Items.FirstOrDefault(it => it.IsTarget && it.Y > 0.99f);
            }
            Assert.That(target, Is.Not.Null);

            var reported = new List<TrialResult>();
            bool resolved = false;
            rules.ItemResolved += (item, _) => resolved |= item == target;
            for (int i = 0; i < 2000 && !resolved; i++)
            {
                // La canasta persigue la posición actual del objeto, que va cambiando.
                tiltX = target.X / (1f - rules.BasketHalfWidth);
                int before = trials.Count;
                session.Tick(Frame);
                if (resolved) reported.AddRange(trials.Skip(before));
            }

            Assert.That(reported.Single().Outcome, Is.EqualTo(TrialOutcome.Hit));
        }
    }
}
