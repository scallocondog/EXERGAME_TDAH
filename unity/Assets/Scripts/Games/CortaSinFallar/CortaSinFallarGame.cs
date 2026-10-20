// RF-09, RF-13, RNF-08: registro de Corta sin fallar en el catálogo
using System;
using MoviMente.Gestures;

namespace MoviMente.Games.CortaSinFallar
{
    public static class CortaSinFallarGame
    {
        public const string Id = "corta-sin-fallar";
        public const string SceneName = "Game_CortaSinFallar";

        // onRulesCreated le entrega a la escena las reglas de cada partida
        // (también al reintentar) para que dibuje frutas y prohibidos.
        public static MinigameDefinition CreateDefinition(Func<Random> randomFactory = null,
            Action<CortaSinFallarRules> onRulesCreated = null)
        {
            randomFactory ??= () => new Random();
            return new MinigameDefinition(Id, SceneName, () =>
                {
                    var rules = new CortaSinFallarRules(randomFactory());
                    onRulesCreated?.Invoke(rules);
                    return rules;
                },
                // Solo swing; nunca junto con sacudir (docs/gestos.md).
                easy: new DifficultyProfile(180f, GestureType.Swing),
                medium: new DifficultyProfile(240f, GestureType.Swing),
                hard: new DifficultyProfile(300f, GestureType.Swing));
        }
    }
}
