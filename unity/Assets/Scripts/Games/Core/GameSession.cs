// RF-07, RF-19, CU-06, CU-08: ciclo de vida de una partida, igual para todo minijuego
using System;
using System.Collections.Generic;
using MoviMente.Gestures;

namespace MoviMente.Games
{
    public sealed class GameSession : IMinigameContext
    {
        public const float CountdownSeconds = 3f;

        private readonly Func<int, TiltAxes> tiltProvider;
        private readonly HashSet<int> requiredSlots;
        private readonly HashSet<int> disconnectedSlots = new HashSet<int>();
        private readonly DifficultyProfile profile;
        private MinigameRules rules;
        private bool started;
        private bool pausedByPlayer;

        public GameSession(MinigameDefinition definition, Difficulty difficulty,
            Func<int, TiltAxes> tiltProvider, params int[] slots)
        {
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            Difficulty = difficulty;
            this.tiltProvider = tiltProvider ?? throw new ArgumentNullException(nameof(tiltProvider));
            requiredSlots = new HashSet<int>(slots.Length > 0 ? slots : new[] { 1 });
            profile = definition.Profile(difficulty);
            rules = NewRules();
        }

        public event Action<GameState> StateChanged;
        public event Action<TrialResult> TrialReported;

        public MinigameDefinition Definition { get; }
        public Difficulty Difficulty { get; }
        public GameState State { get; private set; } = GameState.Instructions;
        public float ElapsedSeconds { get; private set; }
        public float RemainingSeconds => Math.Max(0f, profile.DurationSeconds - ElapsedSeconds);
        public float CountdownRemaining { get; private set; }
        public bool IsWaitingForReconnection => State == GameState.Paused && disconnectedSlots.Count > 0;
        // RF-19: solo una partida terminada guarda puntaje; una abandonada, no.
        public bool ShouldSaveScore => State == GameState.Finished;

        // El jugador confirma que vio las instrucciones (RF-10).
        public void BeginCountdown()
        {
            if (State != GameState.Instructions) return;
            StartCountdownOrWait();
        }

        public void Tick(float deltaSeconds)
        {
            if (State == GameState.Countdown)
            {
                CountdownRemaining -= deltaSeconds;
                if (CountdownRemaining > 0f) return;
                CountdownRemaining = 0f;
                Enter(GameState.Playing);
                if (!started)
                {
                    started = true;
                    rules.OnStart();
                }
                return;
            }

            if (State != GameState.Playing) return;

            ElapsedSeconds = Math.Min(ElapsedSeconds + deltaSeconds, profile.DurationSeconds);
            rules.OnTick(deltaSeconds);
            if (State == GameState.Playing && ElapsedSeconds >= profile.DurationSeconds) Enter(GameState.Finished);
        }

        public void HandleGesture(GestureEvent gesture)
        {
            if (State != GameState.Playing) return;
            if (!requiredSlots.Contains(gesture.Slot) || !profile.Allows(gesture.Type)) return;
            rules.OnGesture(gesture);
        }

        // Botón de pausa del mando (RF-19).
        public void PauseByPlayer()
        {
            if (State != GameState.Countdown && State != GameState.Playing && State != GameState.Paused) return;
            pausedByPlayer = true;
            Enter(GameState.Paused);
        }

        // Si todavía falta un mando, queda en pausa esperando la reconexión.
        public void Resume()
        {
            if (State != GameState.Paused || !pausedByPlayer) return;
            pausedByPlayer = false;
            StartCountdownOrWait();
        }

        // RF-07, RNF-07: la desconexión pausa y la reconexión retoma sin perder nada.
        public void SetPadConnected(int slot, bool connected)
        {
            if (!requiredSlots.Contains(slot)) return;

            if (!connected)
            {
                disconnectedSlots.Add(slot);
                if (State == GameState.Countdown || State == GameState.Playing) Enter(GameState.Paused);
                return;
            }

            disconnectedSlots.Remove(slot);
            if (State == GameState.Paused && !pausedByPlayer) StartCountdownOrWait();
        }

        // Reintentar descarta la partida en curso sin guardar y empieza otra.
        public void Retry()
        {
            if (State != GameState.Paused && State != GameState.Finished) return;
            rules = NewRules();
            started = false;
            pausedByPlayer = false;
            ElapsedSeconds = 0f;
            StartCountdownOrWait();
        }

        public void Quit()
        {
            if (State == GameState.Finished || State == GameState.Abandoned) return;
            Enter(GameState.Abandoned);
        }

        public TiltAxes GetTilt(int slot) => tiltProvider(slot);

        void IMinigameContext.Report(TrialOutcome outcome, int slot, float? reactionSeconds)
        {
            if (State != GameState.Playing) return;
            TrialReported?.Invoke(new TrialResult(slot, outcome, reactionSeconds, ElapsedSeconds));
        }

        void IMinigameContext.Finish()
        {
            if (State == GameState.Playing) Enter(GameState.Finished);
        }

        private MinigameRules NewRules()
        {
            MinigameRules created = Definition.CreateRules();
            created.Attach(this);
            return created;
        }

        private void StartCountdownOrWait()
        {
            if (disconnectedSlots.Count > 0)
            {
                Enter(GameState.Paused);
                return;
            }
            CountdownRemaining = CountdownSeconds;
            Enter(GameState.Countdown);
        }

        private void Enter(GameState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }
    }
}
