// RF-09, RF-11, RNF-08: registro de Atrapa lo correcto en el catálogo
using System;

namespace MoviMente.Games.AtrapaLoCorrecto
{
    public static class AtrapaLoCorrectoGame
    {
        public const string Id = "atrapa-lo-correcto";
        public const string SceneName = "Game_AtrapaLoCorrecto";

        // onRulesCreated le entrega a la escena las reglas de cada partida
        // (también al reintentar) para que dibuje canasta y objetos.
        public static MinigameDefinition CreateDefinition(Func<Random> randomFactory = null,
            Action<AtrapaLoCorrectoRules> onRulesCreated = null)
        {
            randomFactory ??= () => new Random();
            return new MinigameDefinition(Id, SceneName, () =>
                {
                    var rules = new AtrapaLoCorrectoRules(randomFactory());
                    onRulesCreated?.Invoke(rules);
                    return rules;
                },
                // Solo inclinación continua: ningún gesto discreto (docs/gestos.md).
                easy: new DifficultyProfile(180f),
                medium: new DifficultyProfile(240f),
                hard: new DifficultyProfile(300f));
        }
    }
}
