// RF-09: reglas de prueba que registran lo que la sesión les entrega
using System.Collections.Generic;
using MoviMente.Gestures;

namespace MoviMente.Games.Tests
{
    internal sealed class FakeRules : MinigameRules
    {
        public int Starts;
        public float TickedSeconds;
        public readonly List<GestureEvent> Gestures = new List<GestureEvent>();

        public IMinigameContext Ctx => Context;

        public override void OnStart() => Starts++;

        public override void OnTick(float deltaSeconds) => TickedSeconds += deltaSeconds;

        public override void OnGesture(GestureEvent gesture) => Gestures.Add(gesture);
    }

    internal static class TestGames
    {
        public const float Duration = 180f;

        public static MinigameDefinition Create(List<FakeRules> created, string id = "prueba",
            float duration = Duration, params GestureType[] gestures)
        {
            if (gestures.Length == 0) gestures = new[] { GestureType.TiltLeft, GestureType.TiltRight };
            var profile = new DifficultyProfile(duration, gestures);
            return new MinigameDefinition(id, "Game_Prueba", () =>
            {
                var rules = new FakeRules();
                created?.Add(rules);
                return rules;
            }, profile, profile, profile);
        }

        public static GestureEvent Gesture(GestureType type, int slot = 1) => new GestureEvent(slot, type, 1f, 0);
    }
}
