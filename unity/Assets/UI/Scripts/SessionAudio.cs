// RNF-06, CU-06: voz y efectos de la partida, enganchados a la sesión que corre GameRunner
using MoviMente.Games;
using UnityEngine;

namespace MoviMente.UI
{
    // Solo reproduce: qué suena y cuándo lo decide SessionSounds.
    public sealed class SessionAudio : MonoBehaviour
    {
        [SerializeField] private AudioClip hit;
        [SerializeField] private AudioClip error;
        [SerializeField] private AudioClip countdown;
        [SerializeField] private AudioClip go;
        [SerializeField] private AudioClip waitingForPad;
        [SerializeField] private AudioClip wellDone;

        private AudioSource voice;
        private AudioSource effects;
        private GameRunner runner;
        private GameSession session;

        private void Awake()
        {
            voice = CreateSource();
            effects = CreateSource();
        }

        // Vive dentro del prefab CoreSystems, que sobrevive a los cambios de escena:
        // así cada minijuego tiene sonido sin agregar nada a su escena.
        private void OnEnable()
        {
            CoreSystems core = GetComponentInParent<CoreSystems>();
            if (core == null) core = CoreSystems.Instance;
            if (core == null)
            {
                Debug.LogWarning("SessionAudio necesita CoreSystems para escuchar la partida.", this);
                return;
            }
            runner = core.Runner;
            runner.SessionStarted += Attach;
            if (runner.Session != null) Attach(runner.Session);
        }

        private void OnDisable()
        {
            if (runner != null) runner.SessionStarted -= Attach;
            runner = null;
            Detach();
        }

        private void Attach(GameSession next)
        {
            Detach();
            session = next;
            session.StateChanged += OnStateChanged;
            session.TrialReported += OnTrialReported;
        }

        private void Detach()
        {
            if (session == null) return;
            session.StateChanged -= OnStateChanged;
            session.TrialReported -= OnTrialReported;
            session = null;
        }

        // Una sola voz a la vez: la que entra corta a la anterior (RNF-05), y una
        // pausa en medio de "tres, dos, uno" la deja callada.
        private void OnStateChanged(GameState state)
        {
            voice.Stop();
            AudioClip clip = ClipFor(SessionSounds.For(state, session.IsWaitingForReconnection));
            if (clip == null) return;
            voice.clip = clip;
            voice.Play();
        }

        // Los efectos se pueden pisar entre sí: dos aciertos seguidos suenan los dos.
        private void OnTrialReported(TrialResult trial)
        {
            AudioClip clip = ClipFor(SessionSounds.For(trial.Outcome));
            if (clip != null) effects.PlayOneShot(clip);
        }

        private AudioClip ClipFor(SessionCue cue) => cue switch
        {
            SessionCue.Hit => hit,
            SessionCue.Error => error,
            SessionCue.Countdown => countdown,
            SessionCue.Go => go,
            SessionCue.WaitingForPad => waitingForPad,
            SessionCue.WellDone => wellDone,
            _ => null,
        };

        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            return source;
        }
    }
}
