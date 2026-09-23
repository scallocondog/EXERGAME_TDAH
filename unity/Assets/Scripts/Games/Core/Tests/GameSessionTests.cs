// RF-07, RF-19, CU-06, CU-08: pruebas del ciclo de vida de la partida
using System.Collections.Generic;
using MoviMente.Gestures;
using NUnit.Framework;

namespace MoviMente.Games.Tests
{
    public sealed class GameSessionTests
    {
        private List<FakeRules> created;
        private GameSession session;
        private List<GameState> states;
        private List<TrialResult> trials;

        private FakeRules Rules => created[created.Count - 1];

        [SetUp]
        public void SetUp()
        {
            created = new List<FakeRules>();
            session = NewSession();
        }

        private GameSession NewSession(params int[] slots)
        {
            var s = new GameSession(TestGames.Create(created), Difficulty.Medium, _ => new TiltAxes(0.5f, 0f), slots);
            states = new List<GameState>();
            trials = new List<TrialResult>();
            s.StateChanged += states.Add;
            s.TrialReported += trials.Add;
            return s;
        }

        private void StartPlaying()
        {
            session.BeginCountdown();
            session.Tick(GameSession.CountdownSeconds);
        }

        [Test]
        public void Empieza_EnInstrucciones_YPasaPorLaCuentaRegresiva()
        {
            Assert.That(session.State, Is.EqualTo(GameState.Instructions));

            session.BeginCountdown();
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
            session.Tick(2.9f);
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
            Assert.That(Rules.Starts, Is.Zero);

            session.Tick(0.2f);
            Assert.That(session.State, Is.EqualTo(GameState.Playing));
            Assert.That(Rules.Starts, Is.EqualTo(1));
            Assert.That(states, Is.EqualTo(new[] { GameState.Countdown, GameState.Playing }));
        }

        [Test]
        public void Gestos_SoloLleganJugandoYSiElPerfilLosPermite()
        {
            session.HandleGesture(TestGames.Gesture(GestureType.TiltLeft));
            session.BeginCountdown();
            session.HandleGesture(TestGames.Gesture(GestureType.TiltLeft));
            Assert.That(Rules.Gestures, Is.Empty);

            session.Tick(GameSession.CountdownSeconds);
            session.HandleGesture(TestGames.Gesture(GestureType.TiltLeft));
            session.HandleGesture(TestGames.Gesture(GestureType.Swing));
            session.HandleGesture(TestGames.Gesture(GestureType.TiltRight, slot: 2));

            Assert.That(Rules.Gestures.Count, Is.EqualTo(1));
            Assert.That(Rules.Gestures[0].Type, Is.EqualTo(GestureType.TiltLeft));
        }

        [Test]
        public void Tiempo_TerminaAlTopeYSeGuardaPuntaje()
        {
            StartPlaying();
            session.Tick(100f);
            Assert.That(session.RemainingSeconds, Is.EqualTo(TestGames.Duration - 100f).Within(0.001f));

            session.Tick(100f);
            Assert.That(session.State, Is.EqualTo(GameState.Finished));
            Assert.That(session.ElapsedSeconds, Is.EqualTo(TestGames.Duration));
            Assert.That(session.ShouldSaveScore, Is.True);

            session.Tick(10f);
            Assert.That(session.ElapsedSeconds, Is.EqualTo(TestGames.Duration));
        }

        [Test]
        public void Pausa_CongelaElTiempoYLosGestos_YReanudarVuelveConCuentaRegresiva()
        {
            StartPlaying();
            session.Tick(10f);
            session.PauseByPlayer();

            session.Tick(50f);
            session.HandleGesture(TestGames.Gesture(GestureType.TiltLeft));
            Assert.That(session.ElapsedSeconds, Is.EqualTo(10f));
            Assert.That(Rules.Gestures, Is.Empty);

            session.Resume();
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
            session.Tick(GameSession.CountdownSeconds);
            Assert.That(session.State, Is.EqualTo(GameState.Playing));
            Assert.That(Rules.Starts, Is.EqualTo(1));
        }

        [Test]
        public void Salir_AbandonaSinGuardarPuntaje()
        {
            StartPlaying();
            session.PauseByPlayer();
            session.Quit();

            Assert.That(session.State, Is.EqualTo(GameState.Abandoned));
            Assert.That(session.ShouldSaveScore, Is.False);

            session.Resume();
            session.Tick(10f);
            Assert.That(session.State, Is.EqualTo(GameState.Abandoned));
        }

        [Test]
        public void Desconexion_PausaYLaReconexionRetomaDondeQuedo()
        {
            StartPlaying();
            session.Tick(42f);

            session.SetPadConnected(1, false);
            Assert.That(session.State, Is.EqualTo(GameState.Paused));
            Assert.That(session.IsWaitingForReconnection, Is.True);
            session.Tick(30f);

            session.SetPadConnected(1, true);
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
            session.Tick(GameSession.CountdownSeconds);
            Assert.That(session.State, Is.EqualTo(GameState.Playing));
            Assert.That(session.ElapsedSeconds, Is.EqualTo(42f));
            Assert.That(Rules.Starts, Is.EqualTo(1));
        }

        [Test]
        public void PausaDelJugador_NoSeReanudaSolaAlReconectar()
        {
            StartPlaying();
            session.PauseByPlayer();
            session.SetPadConnected(1, false);
            session.SetPadConnected(1, true);

            Assert.That(session.State, Is.EqualTo(GameState.Paused));
            session.Resume();
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
        }

        [Test]
        public void Reanudar_SinMando_EsperaLaReconexion()
        {
            StartPlaying();
            session.SetPadConnected(1, false);
            session.PauseByPlayer();
            session.Resume();

            Assert.That(session.State, Is.EqualTo(GameState.Paused));
            Assert.That(session.IsWaitingForReconnection, Is.True);

            session.SetPadConnected(1, true);
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
        }

        [Test]
        public void DesconexionEnInstrucciones_NoArrancaHastaReconectar()
        {
            session.SetPadConnected(1, false);
            session.BeginCountdown();
            Assert.That(session.State, Is.EqualTo(GameState.Paused));

            session.SetPadConnected(1, true);
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
        }

        [Test]
        public void MandoQueNoJuega_NoPausaLaPartida()
        {
            StartPlaying();
            session.SetPadConnected(2, false);

            Assert.That(session.State, Is.EqualTo(GameState.Playing));
        }

        [Test]
        public void DosMandos_CualquieraQueSeCaigaPausa()
        {
            session = NewSession(1, 2);
            StartPlaying();
            session.HandleGesture(TestGames.Gesture(GestureType.TiltRight, slot: 2));
            Assert.That(Rules.Gestures.Count, Is.EqualTo(1));

            session.SetPadConnected(2, false);
            Assert.That(session.State, Is.EqualTo(GameState.Paused));
        }

        [Test]
        public void Reintentar_DescartaLaPartidaYEmpiezaDeCero()
        {
            StartPlaying();
            session.Tick(60f);
            FakeRules first = Rules;
            session.PauseByPlayer();

            session.Retry();
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
            Assert.That(session.ElapsedSeconds, Is.Zero);
            Assert.That(Rules, Is.Not.SameAs(first));

            session.Tick(GameSession.CountdownSeconds);
            Assert.That(Rules.Starts, Is.EqualTo(1));
        }

        [Test]
        public void Reintentar_TambienDesdeElResumen()
        {
            StartPlaying();
            session.Tick(TestGames.Duration);
            session.Retry();

            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
        }

        [Test]
        public void LasReglasReportanResultadosSoloMientrasSeJuega()
        {
            StartPlaying();
            session.Tick(12f);
            Rules.Ctx.Report(TrialOutcome.Hit, 1, 0.8f);
            Rules.Ctx.Report(TrialOutcome.Omission, 1);

            session.PauseByPlayer();
            Rules.Ctx.Report(TrialOutcome.Error, 1, 0.5f);

            Assert.That(trials.Count, Is.EqualTo(2));
            Assert.That(trials[0].Outcome, Is.EqualTo(TrialOutcome.Hit));
            Assert.That(trials[0].ReactionSeconds, Is.EqualTo(0.8f));
            Assert.That(trials[0].ElapsedSeconds, Is.EqualTo(12f));
            Assert.That(trials[1].ReactionSeconds, Is.Null);
        }

        [Test]
        public void LasReglasPuedenTerminarAntesDelTope()
        {
            StartPlaying();
            session.Tick(30f);
            Rules.Ctx.Finish();

            Assert.That(session.State, Is.EqualTo(GameState.Finished));
            Assert.That(session.ShouldSaveScore, Is.True);
        }

        [Test]
        public void LasReglasLeenLaInclinacionYLaDificultad()
        {
            StartPlaying();

            Assert.That(Rules.Ctx.GetTilt(1).X, Is.EqualTo(0.5f));
            Assert.That(Rules.Ctx.Difficulty, Is.EqualTo(Difficulty.Medium));
        }
    }
}
