// RF-11 a RF-14, RNF-08: base común; cada minijuego solo implementa sus reglas
using MoviMente.Gestures;

namespace MoviMente.Games
{
    // La sesión solo llama a estos métodos mientras se juega: las reglas no
    // tienen que preocuparse por pausas, desconexiones ni cuentas regresivas.
    public abstract class MinigameRules
    {
        protected IMinigameContext Context { get; private set; }

        internal void Attach(IMinigameContext context) => Context = context;

        // Una sola vez por partida, cuando termina la primera cuenta regresiva.
        public virtual void OnStart() { }

        public virtual void OnTick(float deltaSeconds) { }

        // Solo llegan los gestos que el perfil de dificultad permite.
        public virtual void OnGesture(GestureEvent gesture) { }
    }
}
