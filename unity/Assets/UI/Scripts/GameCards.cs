// RF-10, RNF-06: cómo se presenta cada minijuego en la TV: nombre, frase y demostración del gesto
namespace MoviMente.UI
{
    // Qué muestra la demostración de las instrucciones (docs/ux/sistema-visual.md §6).
    public enum GestureDemo
    {
        None,
        // El celular se inclina y la canasta lo sigue.
        Tilt,
        // Latigazo con el celular: la fruta de la franja se parte en dos.
        Swing,
    }

    public readonly struct GameCard
    {
        public readonly string Name;
        // Una línea, en segunda persona; la voz dice lo mismo.
        public readonly string Instruction;
        public readonly GestureDemo Demo;

        public GameCard(string name, string instruction, GestureDemo demo)
        {
            Name = name;
            Instruction = instruction;
            Demo = demo;
        }
    }

    public static class GameCards
    {
        // Los ids son los de cada MinigameDefinition. Se comparan como texto para que la
        // UI no dependa del ensamblado de cada minijuego (RNF-08). El ícono lo pone el
        // USS con la clase "juego--<id>".
        public static GameCard For(string id) => id switch
        {
            "atrapa-lo-correcto" => new GameCard("Atrapa lo correcto", "Inclina para mover la canasta", GestureDemo.Tilt),
            "corta-sin-fallar" => new GameCard("Corta sin fallar", "Corta las frutas, no las piedras", GestureDemo.Swing),
            _ => new GameCard(id, "¡Prepárate!", GestureDemo.None),
        };

        public static string ClassFor(string id) => $"juego--{id}";
    }
}
