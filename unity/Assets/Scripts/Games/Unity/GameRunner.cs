// RF-07, RF-18, RF-19, CU-06, CU-08: corre la partida en Unity con el mando real
using System;
using MoviMente.Gestures;
using MoviMente.Net;
using UnityEngine;

namespace MoviMente.Games
{
    // Pegamento entre la red, los gestos y GameSession. La escena de cada
    // minijuego solo llama a Begin y dibuja lo que sus reglas exponen.
    public sealed class GameRunner : MonoBehaviour
    {
        [SerializeField] private ServerConnection connection;
        [SerializeField] private PadInputHub input;

        public GameSession Session { get; private set; }

        public event Action<GameSession> SessionStarted;

        public GameSession Begin(MinigameDefinition definition, Difficulty difficulty, params int[] slots)
        {
            End();
            Session = new GameSession(definition, difficulty, input.GetTilt, slots);
            // Si el mando ya se cayó antes de empezar, la partida arranca esperándolo.
            foreach (int slot in Session.Slots)
            {
                if (!connection.Link.IsPadConnected(slot)) Session.SetPadConnected(slot, false);
            }
            Session.StateChanged += OnStateChanged;
            Session.TrialReported += OnTrialReported;
            SessionStarted?.Invoke(Session);
            return Session;
        }

        public void End()
        {
            if (Session == null) return;
            Session.StateChanged -= OnStateChanged;
            Session.TrialReported -= OnTrialReported;
            Session = null;
        }

        private void OnEnable()
        {
            input.GestureDetected += OnGesture;
            input.ButtonPressed += OnButton;
            connection.Link.PadConnectionChanged += OnPadConnectionChanged;
        }

        private void OnDisable()
        {
            input.GestureDetected -= OnGesture;
            input.ButtonPressed -= OnButton;
            connection.Link.PadConnectionChanged -= OnPadConnectionChanged;
        }

        private void Update() => Session?.Tick(Time.deltaTime);

        private void OnGesture(GestureEvent gesture) => Session?.HandleGesture(gesture);

        private void OnButton(int slot, string action)
        {
            if (Session != null) SessionControls.OnButton(Session, action);
        }

        private void OnPadConnectionChanged(int slot, bool connected) => Session?.SetPadConnected(slot, connected);

        private void OnTrialReported(TrialResult trial)
        {
            string pattern = SessionFeedback.HapticFor(trial.Outcome);
            if (pattern != null) connection.SendHaptic(trial.Slot, pattern);
        }

        private void OnStateChanged(GameState state)
        {
            string value = SessionFeedback.PadStateFor(state);
            if (value == null) return;
            foreach (int slot in Session.Slots) connection.SendState(slot, value);
        }
    }
}
