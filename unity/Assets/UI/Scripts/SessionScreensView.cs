// RF-08, RF-10, RF-16, RF-19, CU-08: las pantallas de la TV durante la partida (UI Toolkit)
using System;
using System.Collections.Generic;
using MoviMente.Games;
using MoviMente.Gestures;
using MoviMente.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoviMente.UI
{
    // Vive dentro del prefab CoreSystems, igual que SessionAudio: sigue la partida que
    // corre GameRunner y muestra la pantalla que le toca (docs/ux/sistema-visual.md §6).
    // Solo dibuja y elige opciones de menú; las reglas siguen en GameSession.
    [RequireComponent(typeof(UIDocument))]
    public sealed class SessionScreensView : MonoBehaviour
    {
        // Los menús los maneja el Mando 1 (docs/ux/sistema-visual.md §4).
        private const int MenuSlot = 1;
        // Ya está en el protocolo (docs/protocolo-ws.md); Net todavía no tiene la constante.
        private const string TapPattern = "tap";
        private const string ActiveClass = "activa";
        private const string LightThemeClass = "tema-claro";
        private const float EnterSeconds = 0.22f;
        private const float EnterRise = 24f;

        private enum PauseOption { Resume, Restart, Exit }

        private enum ResultsOption { PlayAgain, OtherGame }

        [Tooltip("Tema A, claro: para proyector en una sala muy iluminada. Sin marcar, B oscuro.")]
        [SerializeField] private bool lightTheme;

        private readonly ScoreBoard score = new ScoreBoard();
        private readonly Dictionary<SessionScreen, VisualElement> screens = new Dictionary<SessionScreen, VisualElement>();
        private readonly Pulse enter = new Pulse();

        private CoreSystems core;
        private GameSession session;
        private VisualElement root;
        private VisualElement hudLayer;
        private VisualElement entering;
        private Label missingPad;
        private SessionScreen current = SessionScreen.None;
        private string gameClass;

        private RoomView room;
        private InstructionsView instructions;
        private CountdownView countdown;
        private HudView hud;
        private MenuView pauseMenu;
        private MenuView resultsMenu;
        private ResultsView results;

        // "Salir" y "Elegir otro juego" llevan al menú de juegos, que todavía no existe
        // (RF-09): cuando exista, escucha esto para cargar su escena.
        public event Action GameMenuRequested;

        private void OnEnable()
        {
            root = GetComponent<UIDocument>().rootVisualElement.Q("raiz");
            root.EnableInClassList(LightThemeClass, lightTheme);
            FindScreens();

            core = GetComponentInParent<CoreSystems>();
            if (core == null) core = CoreSystems.Instance;
            if (core == null)
            {
                Debug.LogWarning("SessionScreensView necesita CoreSystems para seguir la partida.", this);
                return;
            }
            core.PadInput.GestureDetected += OnGesture;
            core.PadInput.ButtonPressed += OnButton;
            room.Bind(core.Connection.Link, core.Qr);
        }

        private void OnDisable()
        {
            if (core != null)
            {
                core.PadInput.GestureDetected -= OnGesture;
                core.PadInput.ButtonPressed -= OnButton;
            }
            room?.Unbind();
            Detach();
            core = null;
            current = SessionScreen.None;
        }

        private void Update()
        {
            if (core == null) return;
            GameSession running = core.Runner.Session;
            if (running != session) Attach(running);

            float now = Time.unscaledTime;
            Show(Current(), now);
            if (session == null) return;

            switch (current)
            {
                case SessionScreen.Instructions:
                    instructions.Tick(now);
                    break;
                case SessionScreen.Countdown:
                    countdown.Tick(session.CountdownRemaining, now);
                    break;
                case SessionScreen.Results:
                    results.Tick(now);
                    break;
            }
            if (SessionScreens.ShowsHud(current)) hud.Tick(session, now);
            PaintEnter(now);
        }

        private void FindScreens()
        {
            screens.Clear();
            screens[SessionScreen.Room] = root.Q("sala");
            screens[SessionScreen.Instructions] = root.Q("instrucciones");
            screens[SessionScreen.Countdown] = root.Q("cuenta");
            screens[SessionScreen.Pause] = root.Q("pausa");
            screens[SessionScreen.WaitingForPad] = root.Q("sin-mando");
            screens[SessionScreen.Results] = root.Q("resultados");
            hudLayer = root.Q("hud");
            missingPad = root.Q<Label>("mando-perdido");

            room = new RoomView(screens[SessionScreen.Room]);
            instructions = new InstructionsView(screens[SessionScreen.Instructions]);
            countdown = new CountdownView(screens[SessionScreen.Countdown]);
            hud = new HudView(hudLayer);
            pauseMenu = new MenuView(screens[SessionScreen.Pause]);
            resultsMenu = new MenuView(screens[SessionScreen.Results]);
            results = new ResultsView(screens[SessionScreen.Results]);
        }

        // Una partida nueva (o ninguna, cuando la escena del minijuego se cierra).
        private void Attach(GameSession next)
        {
            Detach();
            session = next;
            if (session == null) return;

            session.TrialReported += OnTrialReported;
            score.Reset();
            hud.Reset();

            string id = session.Definition.Id;
            GameCard card = GameCards.For(id);
            if (gameClass != null) root.RemoveFromClassList(gameClass);
            gameClass = GameCards.ClassFor(id);
            root.AddToClassList(gameClass);
            root.Query<Label>(className: "nombre-juego").ForEach(label => label.text = card.Name);
            root.Query<Label>(className: "frase-juego").ForEach(label => label.text = card.Instruction);
        }

        private void Detach()
        {
            if (session == null) return;
            session.TrialReported -= OnTrialReported;
            session = null;
        }

        // Los mandos solo se miran antes de empezar: después, la sesión ya sabe si falta uno.
        private SessionScreen Current()
        {
            if (session == null) return SessionScreen.None;
            bool padsReady = session.State != GameState.Instructions || PadsReady();
            return SessionScreens.For(session.State, session.IsWaitingForReconnection, padsReady);
        }

        private bool PadsReady()
        {
            foreach (int slot in session.Slots)
            {
                if (!core.Connection.Link.IsPadConnected(slot)) return false;
            }
            return true;
        }

        private void Show(SessionScreen next, float now)
        {
            if (next == current) return;
            current = next;
            foreach (KeyValuePair<SessionScreen, VisualElement> pair in screens)
            {
                pair.Value.EnableInClassList(ActiveClass, pair.Key == next);
            }
            hudLayer.EnableInClassList(ActiveClass, SessionScreens.ShowsHud(next));

            switch (next)
            {
                case SessionScreen.Countdown:
                    countdown.Reset();
                    break;
                case SessionScreen.Pause:
                    pauseMenu.Reset((int)PauseOption.Resume);
                    break;
                case SessionScreen.WaitingForPad:
                    missingPad.text = MissingPad().ToString();
                    break;
                case SessionScreen.Results:
                    resultsMenu.Reset((int)ResultsOption.PlayAgain);
                    results.Show(score, now);
                    break;
            }
            StartEnter(next, now);
        }

        // Entrar a una pantalla: fundido y 24 px hacia arriba. En las capas se mueve
        // solo el panel, así el velo no deja ver un borde del juego al subir.
        private void StartEnter(SessionScreen next, float now)
        {
            if (entering != null) PaintEnterAt(1f);
            entering = null;
            if (!screens.TryGetValue(next, out VisualElement screen)) return;
            entering = next is SessionScreen.Pause or SessionScreen.WaitingForPad ? screen.Q(className: "panel-sombra") : screen;
            enter.Start(now, EnterSeconds);
            PaintEnterAt(0f);
        }

        private void PaintEnter(float now)
        {
            if (entering == null) return;
            float? t = enter.Progress(now);
            PaintEnterAt(t ?? 1f);
            if (!t.HasValue) entering = null;
        }

        private void PaintEnterAt(float t)
        {
            float eased = 1f - (1f - t) * (1f - t);
            entering.style.opacity = eased;
            entering.style.translate = new Translate(0f, EnterRise * (1f - eased));
        }

        private int MissingPad()
        {
            foreach (int slot in session.Slots)
            {
                if (!core.Connection.Link.IsPadConnected(slot)) return slot;
            }
            return MenuSlot;
        }

        private void OnTrialReported(TrialResult trial)
        {
            score.Add(trial);
            hud.Feedback(trial.Outcome, score, Time.unscaledTime);
        }

        // Inclinar mueve el foco del menú que está a la vista, y el mando vibra "tap".
        private void OnGesture(GestureEvent gesture)
        {
            if (gesture.Slot != MenuSlot) return;
            MenuView menu = current switch
            {
                SessionScreen.Pause => pauseMenu,
                SessionScreen.Results => resultsMenu,
                _ => null,
            };
            if (menu == null || !menu.Move(FocusList.StepFor(gesture.Type), Time.unscaledTime)) return;
            core.Connection.SendHaptic(MenuSlot, TapPattern);
        }

        // Confirmar en las instrucciones y pausar lo resuelve SessionControls; aquí solo
        // se eligen las opciones de los menús de pausa y de resultados (RF-08, RF-19).
        private void OnButton(int slot, string action)
        {
            if (slot != MenuSlot || action != PadButton.Confirm || session == null) return;
            if (current == SessionScreen.Pause) Choose((PauseOption)pauseMenu.Index);
            else if (current == SessionScreen.Results) Choose((ResultsOption)resultsMenu.Index);
        }

        private void Choose(PauseOption option)
        {
            switch (option)
            {
                case PauseOption.Resume:
                    session.Resume();
                    break;
                case PauseOption.Restart:
                    Restart();
                    break;
                case PauseOption.Exit:
                    session.Quit();
                    GameMenuRequested?.Invoke();
                    break;
            }
        }

        private void Choose(ResultsOption option)
        {
            if (option == ResultsOption.PlayAgain) Restart();
            else GameMenuRequested?.Invoke();
        }

        private void Restart()
        {
            score.Reset();
            hud.Reset();
            session.Retry();
        }
    }
}
