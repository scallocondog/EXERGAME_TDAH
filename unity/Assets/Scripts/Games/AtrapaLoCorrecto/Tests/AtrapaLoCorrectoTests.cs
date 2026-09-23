// RF-11, RF-15: pruebas de Atrapa lo correcto sobre una sesión real
using System;
using System.Collections.Generic;
using System.Linq;
using MoviMente.Gestures;
using NUnit.Framework;

namespace MoviMente.Games.AtrapaLoCorrecto.Tests
{
    public sealed class AtrapaLoCorrectoTests
    {
        private const float Frame = 1f / 60f;

        private AtrapaLoCorrectoRules rules;
        private GameSession session;
        private float tiltX;
        private List<TrialResult> trials;
        private List<(FallingItem item, bool caught)> resolved;

        private void Start(Difficulty difficulty = Difficulty.Medium, int seed = 7)
        {
            var definition = AtrapaLoCorrectoGame.CreateDefinition(() => new Random(seed), r => rules = r);
            session = new GameSession(definition, difficulty, _ => new TiltAxes(tiltX, 0f));
            trials = new List<TrialResult>();
            resolved = new List<(FallingItem, bool)>();
            session.TrialReported += trials.Add;
            rules.ItemResolved += (item, caught) => resolved.Add((item, caught));
            session.BeginCountdown();
            session.Tick(GameSession.CountdownSeconds);
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame) session.Tick(Frame);
        }

        // Inclinación que deja la canasta centrada bajo x.
        private float TiltFor(float x) => x / (1f - rules.BasketHalfWidth);

        // Un objeto recién aparecido y lejos del centro, para que la canasta
        // tenga que moverse de verdad hasta él.
        private FallingItem WaitForItem(bool target)
        {
            for (int i = 0; i < 5000; i++)
            {
                FallingItem item = rules.Items.FirstOrDefault(it =>
                    it.IsTarget == target && it.Y > 0.99f && Math.Abs(it.X) > 0.4f);
                if (item != null) return item;
                session.Tick(Frame);
            }
            throw new AssertionException("no apareció el objeto esperado");
        }

        // Devuelve solo lo reportado en el cuadro en que se resolvió ese objeto:
        // otros objetos pueden caer mientras tanto.
        private List<TrialResult> FollowUntilResolved(FallingItem item, float x)
        {
            tiltX = TiltFor(x);
            for (int i = 0; i < 2000; i++)
            {
                int before = trials.Count;
                session.Tick(Frame);
                if (resolved.Any(r => r.item == item)) return trials.Skip(before).ToList();
            }
            throw new AssertionException("el objeto no terminó de caer");
        }

        private static float FarSide(FallingItem item) => item.X >= 0f ? -1f : 1f;

        [Test]
        public void SeRegistraEnElCatalogo_ConPartidasDe3a5Minutos()
        {
            var catalog = new MinigameCatalog();
            catalog.Register(AtrapaLoCorrectoGame.CreateDefinition());

            MinigameDefinition game = catalog.Find(AtrapaLoCorrectoGame.Id);
            Assert.That(game.SceneName, Is.EqualTo("Game_AtrapaLoCorrecto"));
            Assert.That(game.Profile(Difficulty.Easy).Gestures, Is.Empty);
        }

        [Test]
        public void LaCategoriaPedida_SeConoceAntesDeEmpezar()
        {
            var definition = AtrapaLoCorrectoGame.CreateDefinition(() => new Random(5), r => rules = r);
            session = new GameSession(definition, Difficulty.Easy, _ => TiltAxes.Neutral);
            int beforeStart = rules.TargetCategory;

            session.BeginCountdown();
            session.Tick(GameSession.CountdownSeconds + 5f);

            Assert.That(session.State, Is.EqualTo(GameState.Playing));
            Assert.That(rules.TargetCategory, Is.EqualTo(beforeStart));
            Assert.That(rules.Items.All(i => !i.IsTarget || i.Category == beforeStart), Is.True);
        }

        [Test]
        public void LaCanastaSigueLaInclinacionYNoSaleDelCampo()
        {
            Start();
            tiltX = 0.5f;
            Run(0.5f);
            Assert.That(rules.BasketX, Is.EqualTo(0.5f * (1f - rules.BasketHalfWidth)).Within(0.01f));

            tiltX = 1f;
            Run(0.5f);
            Assert.That(rules.BasketX, Is.EqualTo(1f - rules.BasketHalfWidth).Within(0.01f));
        }

        [Test]
        public void AtraparUnObjetoPedido_EsAciertoConTiempoDeReaccion()
        {
            Start();
            FallingItem item = WaitForItem(target: true);
            tiltX = TiltFor(FarSide(item));
            Run(0.3f);
            List<TrialResult> reported = FollowUntilResolved(item, item.X);

            TrialResult hit = reported.Single();
            Assert.That(hit.Outcome, Is.EqualTo(TrialOutcome.Hit));
            Assert.That(hit.Slot, Is.EqualTo(AtrapaLoCorrectoRules.PlayerSlot));
            Assert.That(hit.ReactionSeconds, Is.GreaterThan(0.25f).And.LessThan(0.6f));
        }

        [Test]
        public void AtraparUnDistractor_EsError()
        {
            Start();
            FallingItem item = WaitForItem(target: false);
            List<TrialResult> reported = FollowUntilResolved(item, item.X);

            Assert.That(reported.Single().Outcome, Is.EqualTo(TrialOutcome.Error));
            Assert.That(resolved.Single(r => r.item == item).caught, Is.True);
        }

        [Test]
        public void DejarCaerUnObjetoPedido_EsOmision_YUnDistractorNoCuenta()
        {
            Start();
            FallingItem target = WaitForItem(target: true);
            TrialResult omission = FollowUntilResolved(target, FarSide(target)).Single();
            Assert.That(omission.Outcome, Is.EqualTo(TrialOutcome.Omission));
            Assert.That(omission.ReactionSeconds, Is.Null);

            FallingItem distractor = WaitForItem(target: false);
            List<TrialResult> reported = FollowUntilResolved(distractor, FarSide(distractor));
            Assert.That(resolved.Single(r => r.item == distractor).caught, Is.False);
            Assert.That(reported, Is.Empty);
        }

        [Test]
        public void LosDistractoresSonDeOtraCategoria_YLaProporcionSigueLaDificultad()
        {
            Start(Difficulty.Easy, seed: 3);
            var spawned = new List<FallingItem>();
            rules.ItemSpawned += spawned.Add;
            Run(170f);

            Assert.That(spawned.Count, Is.GreaterThan(60));
            Assert.That(spawned.Where(i => i.IsTarget).All(i => i.Category == rules.TargetCategory), Is.True);
            Assert.That(spawned.Where(i => !i.IsTarget).All(i => i.Category != rules.TargetCategory), Is.True);
            double ratio = spawned.Count(i => i.IsTarget) / (double)spawned.Count;
            Assert.That(ratio, Is.EqualTo(AtrapaTuning.Easy.TargetRatio).Within(0.12));
        }

        [Test]
        public void MasDificil_EsMasRapidoYConCanastaMasAngosta()
        {
            AtrapaTuning[] levels = { AtrapaTuning.Easy, AtrapaTuning.Medium, AtrapaTuning.Hard };
            for (int i = 1; i < levels.Length; i++)
            {
                Assert.That(levels[i].FallSeconds, Is.LessThan(levels[i - 1].FallSeconds));
                Assert.That(levels[i].SpawnIntervalSeconds, Is.LessThan(levels[i - 1].SpawnIntervalSeconds));
                Assert.That(levels[i].BasketHalfWidth, Is.LessThan(levels[i - 1].BasketHalfWidth));
            }
        }

        [Test]
        public void EnPausa_NadaCaeNiSeReporta()
        {
            Start();
            WaitForItem(target: true);
            float[] before = rules.Items.Select(i => i.Y).ToArray();
            int reportedBefore = trials.Count;
            session.PauseByPlayer();
            Run(10f);

            Assert.That(rules.Items.Select(i => i.Y).ToArray(), Is.EqualTo(before));
            Assert.That(trials.Count, Is.EqualTo(reportedBefore));
        }

        [Test]
        public void MismaSemilla_MismaPartida()
        {
            Start(seed: 11);
            Run(20f);
            int[] first = rules.Items.Select(i => i.Category).ToArray();
            int target = rules.TargetCategory;

            Start(seed: 11);
            Run(20f);
            Assert.That(rules.TargetCategory, Is.EqualTo(target));
            Assert.That(rules.Items.Select(i => i.Category).ToArray(), Is.EqualTo(first));
        }
    }
}
