// RF-10, RF-16, RF-19, CU-08: pruebas de qué pantalla muestra la TV en cada momento
using MoviMente.Games;
using MoviMente.Gestures;
using NUnit.Framework;

namespace MoviMente.UI.Tests
{
    public sealed class SessionScreensTests
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

        [TestCase(GameState.Instructions, false, true, SessionScreen.Instructions)]
        [TestCase(GameState.Instructions, false, false, SessionScreen.Room)]
        [TestCase(GameState.Countdown, false, true, SessionScreen.Countdown)]
        [TestCase(GameState.Playing, false, true, SessionScreen.Playing)]
        [TestCase(GameState.Paused, false, true, SessionScreen.Pause)]
        [TestCase(GameState.Paused, true, false, SessionScreen.WaitingForPad)]
        [TestCase(GameState.Finished, false, true, SessionScreen.Results)]
        [TestCase(GameState.Abandoned, false, true, SessionScreen.None)]
        public void Pantalla_SegunElEstado(GameState state, bool waitingForPad, bool padsReady, SessionScreen expected)
        {
            Assert.That(SessionScreens.For(state, waitingForPad, padsReady), Is.EqualTo(expected));
        }

        [TestCase(SessionScreen.Playing, true)]
        [TestCase(SessionScreen.Pause, true)]
        [TestCase(SessionScreen.WaitingForPad, true)]
        [TestCase(SessionScreen.Countdown, false)]
        [TestCase(SessionScreen.Instructions, false)]
        [TestCase(SessionScreen.Results, false)]
        public void LasCapas_DejanElHudDetras(SessionScreen screen, bool expected)
        {
            Assert.That(SessionScreens.ShowsHud(screen), Is.EqualTo(expected));
        }

        [Test]
        public void SiPausaElJugador_SeVeElMenuDePausa()
        {
            StartPlaying();
            session.PauseByPlayer();
            Assert.That(Current(), Is.EqualTo(SessionScreen.Pause));
        }

        [Test]
        public void SiSeCaeElMando_SeVeLaEspera_YNoLaPausa()
        {
            StartPlaying();
            session.SetPadConnected(1, false);
            Assert.That(Current(), Is.EqualTo(SessionScreen.WaitingForPad));
        }

        [Test]
        public void AlVolverElMando_SeRepiteLaCuentaRegresiva()
        {
            StartPlaying();
            session.SetPadConnected(1, false);
            session.SetPadConnected(1, true);
            Assert.That(Current(), Is.EqualTo(SessionScreen.Countdown));
        }

        [Test]
        public void AlSalirDesdeLaPausa_NoQuedaNingunaPantalla()
        {
            StartPlaying();
            session.PauseByPlayer();
            session.Quit();
            Assert.That(Current(), Is.EqualTo(SessionScreen.None));
        }

        private SessionScreen Current() => SessionScreens.For(session.State, session.IsWaitingForReconnection, padsReady: true);

        private void StartPlaying()
        {
            session.BeginCountdown();
            session.Tick(GameSession.CountdownSeconds);
            Assert.That(session.State, Is.EqualTo(GameState.Playing));
        }
    }
}
