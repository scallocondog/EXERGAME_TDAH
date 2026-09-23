// RF-07, RF-18, RF-19: pruebas de lo que la partida le dice al mando y de sus botones
using MoviMente.Gestures;
using MoviMente.Net;
using NUnit.Framework;

namespace MoviMente.Games.Tests
{
    public sealed class SessionBridgeTests
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

        [TestCase(TrialOutcome.Hit, HapticPattern.Hit)]
        [TestCase(TrialOutcome.Error, HapticPattern.Miss)]
        [TestCase(TrialOutcome.Omission, null)]
        public void Vibracion_SegunElResultado(TrialOutcome outcome, string expected)
        {
            Assert.That(SessionFeedback.HapticFor(outcome), Is.EqualTo(expected));
        }

        [TestCase(GameState.Instructions, null)]
        [TestCase(GameState.Countdown, PadScreenState.Playing)]
        [TestCase(GameState.Playing, PadScreenState.Playing)]
        [TestCase(GameState.Paused, PadScreenState.Paused)]
        [TestCase(GameState.Finished, null)]
        [TestCase(GameState.Abandoned, null)]
        public void EstadoDelMando_SegunLaPartida(GameState state, string expected)
        {
            Assert.That(SessionFeedback.PadStateFor(state), Is.EqualTo(expected));
        }

        [Test]
        public void Confirmar_EnInstrucciones_EmpiezaLaCuentaRegresiva()
        {
            SessionControls.OnButton(session, PadButton.Confirm);
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));

            // Fuera de las instrucciones, confirmar es de la UI (menú de pausa).
            SessionControls.OnButton(session, PadButton.Confirm);
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
        }

        [Test]
        public void Pausa_PausaYOtraVezReanuda()
        {
            SessionControls.OnButton(session, PadButton.Confirm);
            session.Tick(GameSession.CountdownSeconds);

            SessionControls.OnButton(session, PadButton.Pause);
            Assert.That(session.State, Is.EqualTo(GameState.Paused));

            SessionControls.OnButton(session, PadButton.Pause);
            Assert.That(session.State, Is.EqualTo(GameState.Countdown));
        }

        [Test]
        public void Pausa_ConElMandoCaido_SigueEsperandoLaReconexion()
        {
            SessionControls.OnButton(session, PadButton.Confirm);
            session.Tick(GameSession.CountdownSeconds);
            session.SetPadConnected(1, false);

            SessionControls.OnButton(session, PadButton.Pause);
            SessionControls.OnButton(session, PadButton.Pause);
            Assert.That(session.IsWaitingForReconnection, Is.True);
        }

        [Test]
        public void Pausa_EnInstrucciones_NoHaceNada()
        {
            SessionControls.OnButton(session, PadButton.Pause);
            Assert.That(session.State, Is.EqualTo(GameState.Instructions));
        }

        [Test]
        public void LaSesionExponeSusMandos()
        {
            Assert.That(session.Slots, Is.EquivalentTo(new[] { 1 }));
        }
    }
}
