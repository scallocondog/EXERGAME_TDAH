// RF-13, RF-15: pruebas de Corta sin fallar sobre una sesión real
using System;
using System.Collections.Generic;
using System.Linq;
using MoviMente.Gestures;
using NUnit.Framework;

namespace MoviMente.Games.CortaSinFallar.Tests
{
    public sealed class CortaSinFallarTests
    {
        private const float Frame = 1f / 60f;

        private CortaSinFallarRules rules;
        private GameSession session;
        private List<TrialResult> trials;
        private List<FlyingObject> cut;
        private List<FlyingObject> left;

        private void Start(Difficulty difficulty = Difficulty.Medium, int seed = 7)
        {
            var definition = CortaSinFallarGame.CreateDefinition(() => new Random(seed), r => rules = r);
            session = new GameSession(definition, difficulty, _ => TiltAxes.Neutral);
            trials = new List<TrialResult>();
            cut = new List<FlyingObject>();
            left = new List<FlyingObject>();
            session.TrialReported += trials.Add;
            rules.ObjectCut += cut.Add;
            rules.ObjectLeft += left.Add;
            session.BeginCountdown();
            session.Tick(GameSession.CountdownSeconds);
        }

        private void Run(float seconds)
        {
            for (float t = 0f; t < seconds; t += Frame) session.Tick(Frame);
        }

        private void Swing() => session.HandleGesture(new GestureEvent(1, GestureType.Swing, 1f, 0));

        private FlyingObject WaitFor(FlyingKind kind)
        {
            for (int i = 0; i < 5000; i++)
            {
                FlyingObject found = rules.Objects.FirstOrDefault(o => o.Kind == kind && o.SpawnedAt >= session.ElapsedSeconds - Frame);
                if (found != null) return found;
                session.Tick(Frame);
            }
            throw new AssertionException($"no apareció un {kind}");
        }

        private void RunUntil(float elapsed)
        {
            while (session.ElapsedSeconds < elapsed) session.Tick(Frame);
        }

        [Test]
        public void SeRegistraEnElCatalogo_SoloConSwing()
        {
            var catalog = new MinigameCatalog();
            catalog.Register(CortaSinFallarGame.CreateDefinition());

            MinigameDefinition game = catalog.Find(CortaSinFallarGame.Id);
            Assert.That(game.SceneName, Is.EqualTo("Game_CortaSinFallar"));
            foreach (Difficulty difficulty in new[] { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard })
            {
                Assert.That(game.Profile(difficulty).Gestures, Is.EquivalentTo(new[] { GestureType.Swing }));
            }
        }

        [Test]
        public void LaVentanaPredichaCoincideConLoQueSeSimula()
        {
            Start();
            FlyingObject fruit = WaitFor(FlyingKind.Valid);
            var inBand = new List<float>();
            while (rules.Objects.Contains(fruit))
            {
                session.Tick(Frame);
                if (rules.Objects.Contains(fruit) && fruit.Position.Y >= CortaSinFallarRules.BandLow) inBand.Add(session.ElapsedSeconds);
            }

            Assert.That(inBand.First(), Is.EqualTo(fruit.WindowStart).Within(Frame + 1e-4f));
            Assert.That(inBand.Last(), Is.EqualTo(fruit.WindowEnd).Within(Frame + 1e-4f));
            // La cima predicha siempre cae dentro de la zona de corte.
            Assert.That(fruit.Projectile.Apex.Y, Is.InRange(CortaSinFallarRules.BandLow, CortaSinFallarRules.BandHigh));
        }

        [Test]
        public void CortarDentroDeLaVentana_EsAciertoConTiempoDeReaccion()
        {
            Start();
            FlyingObject fruit = WaitFor(FlyingKind.Valid);
            RunUntil(fruit.WindowStart + 0.3f);
            int before = trials.Count;

            Swing();

            TrialResult hit = trials.Skip(before).Single();
            Assert.That(hit.Outcome, Is.EqualTo(TrialOutcome.Hit));
            Assert.That(hit.ReactionSeconds, Is.EqualTo(0.3f).Within(Frame + 1e-3f));
            Assert.That(cut, Has.Member(fruit));
            Assert.That(rules.Objects, Has.No.Member(fruit));
        }

        [Test]
        public void CortarAntesDeQueLlegue_NoLaCorta()
        {
            Start();
            FlyingObject fruit = WaitFor(FlyingKind.Valid);
            int before = trials.Count;

            Swing();

            Assert.That(rules.Objects, Has.Member(fruit));
            Assert.That(trials.Skip(before).Any(t => t.Outcome == TrialOutcome.Hit), Is.False);
        }

        [Test]
        public void DejarCaerUnaFruta_EsOmision()
        {
            Start();
            FlyingObject fruit = WaitFor(FlyingKind.Valid);
            while (rules.Objects.Contains(fruit))
            {
                int before = trials.Count;
                session.Tick(Frame);
                if (rules.Objects.Contains(fruit)) continue;

                TrialResult omission = trials.Skip(before).Single(t => t.Outcome == TrialOutcome.Omission);
                Assert.That(omission.ReactionSeconds, Is.Null);
            }
            Assert.That(left, Has.Member(fruit));
        }

        [Test]
        public void ElProhibidoSigueSuRutaYCruzaLaZonaDeCorte()
        {
            Start();
            FlyingObject forbidden = WaitFor(FlyingKind.Forbidden);

            Assert.That(forbidden.Route.Waypoints.Count, Is.EqualTo(5));
            Assert.That(forbidden.Route.Waypoints.Any(p => p.Y >= CortaSinFallarRules.BandLow && p.Y <= CortaSinFallarRules.BandHigh), Is.True);
            Assert.That(Math.Abs(forbidden.Route.Waypoints[0].X), Is.GreaterThan(1f), "entra desde afuera del campo");
            Assert.That(Math.Abs(forbidden.Route.Waypoints[4].X), Is.GreaterThan(1f), "sale por el otro lado");
        }

        [Test]
        public void CortarAlProhibido_EsError_YDejarloPasarNoCuenta()
        {
            Start();
            FlyingObject forbidden = WaitFor(FlyingKind.Forbidden);
            for (int i = 0; i < 2000 && !CortaSinFallarRules.IsCuttable(forbidden, session.ElapsedSeconds); i++) session.Tick(Frame);
            int before = trials.Count;

            Swing();

            Assert.That(trials.Skip(before).Count(t => t.Outcome == TrialOutcome.Error), Is.EqualTo(1));
            Assert.That(cut, Has.Member(forbidden));

            FlyingObject next = WaitFor(FlyingKind.Forbidden);
            while (rules.Objects.Contains(next))
            {
                int count = trials.Count;
                session.Tick(Frame);
                if (!rules.Objects.Contains(next)) Assert.That(trials.Skip(count).Any(t => t.Outcome == TrialOutcome.Error), Is.False);
            }
            Assert.That(left, Has.Member(next));
        }

        [Test]
        public void OtrosGestos_NoCortan()
        {
            Start();
            FlyingObject fruit = WaitFor(FlyingKind.Valid);
            RunUntil(fruit.WindowStart + 0.2f);

            session.HandleGesture(new GestureEvent(1, GestureType.Shake, 1f, 0));
            session.HandleGesture(new GestureEvent(1, GestureType.TiltLeft, 1f, 0));

            Assert.That(rules.Objects, Has.Member(fruit));
        }

        [Test]
        public void MasDificil_MenosTiempoParaCortar()
        {
            CortaTuning[] levels = { CortaTuning.Easy, CortaTuning.Medium, CortaTuning.Hard };
            var start = new FieldPoint(0f, 0f);
            var apex = new FieldPoint(0f, 0.7f);
            float previousWindow = float.MaxValue;
            foreach (CortaTuning level in levels)
            {
                Projectile shot = Projectile.Aim(start, apex, level.Gravity);
                shot.TryTimesAtHeight(CortaSinFallarRules.BandLow, out float enters, out float exits);
                Assert.That(exits - enters, Is.LessThan(previousWindow));
                previousWindow = exits - enters;
            }
            Assert.That(CortaTuning.Hard.LaunchIntervalSeconds, Is.LessThan(CortaTuning.Easy.LaunchIntervalSeconds));
            Assert.That(CortaTuning.Hard.ForbiddenIntervalSeconds, Is.LessThan(CortaTuning.Easy.ForbiddenIntervalSeconds));
        }

        [Test]
        public void MismaSemilla_MismaPartida()
        {
            Start(seed: 13);
            Run(30f);
            string first = string.Join(";", rules.Objects.Select(o => $"{o.Kind}{o.Category}{o.Position}"));

            Start(seed: 13);
            Run(30f);
            Assert.That(string.Join(";", rules.Objects.Select(o => $"{o.Kind}{o.Category}{o.Position}")), Is.EqualTo(first));
        }

        [Test]
        public void EnPausa_NadaSeMueveNiSeCorta()
        {
            Start();
            FlyingObject fruit = WaitFor(FlyingKind.Valid);
            RunUntil(fruit.WindowStart + 0.1f);
            FieldPoint before = fruit.Position;
            session.PauseByPlayer();

            Run(5f);
            Swing();

            Assert.That(fruit.Position.Y, Is.EqualTo(before.Y));
            Assert.That(rules.Objects, Has.Member(fruit));
        }
    }
}
