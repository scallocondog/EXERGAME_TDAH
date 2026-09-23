// RF-09, RNF-05, RNF-08: registro de minijuegos y reglas de diseño que todos cumplen
using System;
using System.Collections.Generic;
using System.Linq;
using MoviMente.Gestures;

namespace MoviMente.Games
{
    // Agregar un minijuego = registrarlo aquí; Net y Gestures no se tocan.
    public sealed class MinigameCatalog
    {
        // RNF-05: partidas de 3 a 5 minutos.
        public const float MinDurationSeconds = 180f;
        public const float MaxDurationSeconds = 300f;

        private static readonly Difficulty[] Difficulties = { Difficulty.Easy, Difficulty.Medium, Difficulty.Hard };

        private readonly List<MinigameDefinition> games = new List<MinigameDefinition>();

        public IReadOnlyList<MinigameDefinition> All => games;

        public void Register(MinigameDefinition game)
        {
            if (Find(game.Id) != null)
            {
                throw new ArgumentException($"Ya hay un minijuego con id \"{game.Id}\"", nameof(game));
            }

            foreach (Difficulty difficulty in Difficulties)
            {
                float duration = game.Profile(difficulty).DurationSeconds;
                if (duration < MinDurationSeconds || duration > MaxDurationSeconds)
                {
                    throw new ArgumentException(
                        $"\"{game.Id}\" en {difficulty} dura {duration} s; debe durar entre 3 y 5 minutos (RNF-05)",
                        nameof(game));
                }
            }

            // docs/gestos.md: swing y sacudir se confunden, nunca en el mismo minijuego.
            var used = new HashSet<GestureType>(Difficulties.SelectMany(d => game.Profile(d).Gestures));
            if (used.Contains(GestureType.Swing) && used.Contains(GestureType.Shake))
            {
                throw new ArgumentException($"\"{game.Id}\" usa swing y sacudir a la vez", nameof(game));
            }

            games.Add(game);
        }

        public MinigameDefinition Find(string id) => games.FirstOrDefault(g => g.Id == id);
    }
}
