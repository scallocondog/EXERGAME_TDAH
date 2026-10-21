// RF-10, RNF-06: cómo se presenta cada minijuego en la TV: nombre y frase del gesto
namespace MoviMente.UI
{
    public readonly struct GameCard
    {
        public readonly string Name;
        // Una línea, en segunda persona y en positivo; la voz dice lo mismo.
        public readonly string Instruction;

        public GameCard(string name, string instruction)
        {
            Name = name;
            Instruction = instruction;
        }
    }

    public static class GameCards
    {
        // Los ids son los de cada MinigameDefinition. Se comparan como texto para que la
        // UI no dependa del ensamblado de cada minijuego (RNF-08). El ícono lo pone el
        // USS con la clase "juego--<id>".
        public static GameCard For(string id) => id switch
        {
            "atrapa-lo-correcto" => new GameCard("Atrapa lo correcto", "Inclina para mover la canasta"),
            _ => new GameCard(id, "¡Prepárate!"),
        };

        public static string ClassFor(string id) => $"juego--{id}";
    }
}
