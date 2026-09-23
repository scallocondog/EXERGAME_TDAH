// RF-09, RNF-08: todo lo que el menú necesita saber de un minijuego
using System;
using System.Collections.Generic;

namespace MoviMente.Games
{
    public sealed class MinigameDefinition
    {
        private readonly Func<MinigameRules> createRules;
        private readonly Dictionary<Difficulty, DifficultyProfile> profiles;

        public MinigameDefinition(string id, string sceneName, Func<MinigameRules> createRules,
            DifficultyProfile easy, DifficultyProfile medium, DifficultyProfile hard)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
            SceneName = sceneName ?? throw new ArgumentNullException(nameof(sceneName));
            this.createRules = createRules ?? throw new ArgumentNullException(nameof(createRules));
            profiles = new Dictionary<Difficulty, DifficultyProfile>
            {
                [Difficulty.Easy] = easy ?? throw new ArgumentNullException(nameof(easy)),
                [Difficulty.Medium] = medium ?? throw new ArgumentNullException(nameof(medium)),
                [Difficulty.Hard] = hard ?? throw new ArgumentNullException(nameof(hard)),
            };
        }

        public string Id { get; }
        public string SceneName { get; }

        public DifficultyProfile Profile(Difficulty difficulty) => profiles[difficulty];

        // Reglas nuevas en cada partida, así reintentar parte de cero.
        public MinigameRules CreateRules() => createRules();
    }
}
