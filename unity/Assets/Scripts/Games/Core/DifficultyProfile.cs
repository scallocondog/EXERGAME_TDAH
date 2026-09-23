// RF-09: duración y gestos que acepta un minijuego en una dificultad
using System.Collections.Generic;
using MoviMente.Gestures;

namespace MoviMente.Games
{
    public sealed class DifficultyProfile
    {
        private readonly HashSet<GestureType> gestures;

        // La duración es el tope de la partida; un minijuego por rondas puede
        // terminar antes llamando a Finish().
        public DifficultyProfile(float durationSeconds, params GestureType[] gestures)
        {
            DurationSeconds = durationSeconds;
            this.gestures = new HashSet<GestureType>(gestures);
        }

        public float DurationSeconds { get; }
        public IReadOnlyCollection<GestureType> Gestures => gestures;

        public bool Allows(GestureType type) => gestures.Contains(type);
    }
}
