// RNF-06, CU-06: pruebas de qué suena en cada momento de la partida
using MoviMente.Games;
using MoviMente.Gestures;
using NUnit.Framework;

namespace MoviMente.UI.Tests
{
    public sealed class SessionSoundsTests
    {
        private sealed class EmptyRules : MinigameRules { }

        private GameSession session;

        [SetUp]
        public void SetUp()
        {
            var profile = new DifficultyProfile(180f);
            var definition = new MinigameDefinition("vacio", "Game_Vacio", () => new EmptyRules(), profile, profile, profile);
            session = new GameSession(definition, Difficulty.Easy, _ => TiltAxes.Neutral, 1);
        }

        [TestCase(TrialOutcome.Hit, SessionCue.Hit)]
        [TestCase(TrialOutcome.Error, SessionCue.Error)]
        [TestCase(TrialOutcome.Omission, SessionCue.None)]
        public void Efecto_SegunElResultado(TrialOutcome outcome, SessionCue expected)
        {
            Assert.That(SessionSounds.For(outcome), Is.EqualTo(expected));
        }

        [TestCase(GameState.Instructions, false, SessionCue.None)]
        [TestCase(GameState.Countdown, false, SessionCue.Countdown)]
        [TestCase(GameState.Playing, false, SessionCue.Go)]
        [TestCase(GameState.Paused, false, SessionCue.None)]
        [TestCase(GameState.Paused, true, SessionCue.WaitingForPad)]
        [TestCase(GameState.Finished, false, SessionCue.WellDone)]
        [TestCase(GameState.Abandoned, false, SessionCue.None)]
        public void Voz_SegunElEstado(GameState state, bool waitingForPad, SessionCue expected)
        {
            Assert.That(SessionSounds.For(state, waitingForPad), Is.EqualTo(expected));
        }

        [Test]
        public void SiSeCaeElMando_LaVozPideEsperarlo()
        {
            StartPlaying();
            session.SetPadConnected(1, false);
            Assert.That(SessionSounds.For(session.State, session.IsWaitingForReconnection), Is.EqualTo(SessionCue.WaitingForPad));
        }

        [Test]
        public void SiPausaElJugador_NoHablaDelMando()
        {
            StartPlaying();
            session.PauseByPlayer();
            Assert.That(SessionSounds.For(session.State, session.IsWaitingForReconnection), Is.EqualTo(SessionCue.None));
        }

        [Test]
        public void AlVolverElMando_SeRepiteLaCuentaRegresiva()
        {
            StartPlaying();
            session.SetPadConnected(1, false);
            session.SetPadConnected(1, true);
            Assert.That(SessionSounds.For(session.State, session.IsWaitingForReconnection), Is.EqualTo(SessionCue.Countdown));
        }

        private void StartPlaying()
        {
            session.BeginCountdown();
            session.Tick(GameSession.CountdownSeconds);
            Assert.That(session.State, Is.EqualTo(GameState.Playing));
        }
    }
}
