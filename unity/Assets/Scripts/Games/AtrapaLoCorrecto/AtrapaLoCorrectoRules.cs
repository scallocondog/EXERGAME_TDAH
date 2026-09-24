// RF-11: atrapar solo los objetos de la categoría pedida (atención selectiva)
using System;
using System.Collections.Generic;

namespace MoviMente.Games.AtrapaLoCorrecto
{
    public sealed class AtrapaLoCorrectoRules : MinigameRules
    {
        public const int CategoryCount = 3;
        public const float ItemRadius = 0.08f;
        public const int PlayerSlot = 1;
        private const float SpawnMargin = 0.9f;
        private const float FirstSpawnDelaySeconds = 0.5f;
        // Suaviza el temblor del sensor sin que la canasta se sienta pesada (~40 ms).
        private const float BasketFollowRate = 25f;

        private readonly Random random;
        private readonly List<FallingItem> items = new List<FallingItem>();
        private AtrapaTuning tuning = AtrapaTuning.Medium;
        private float nextSpawnAt;
        private int nextId;

        public AtrapaLoCorrectoRules(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            // Se conoce desde antes de empezar: la pantalla de instrucciones la muestra (RF-10).
            TargetCategory = random.Next(CategoryCount);
        }

        public event Action<FallingItem> ItemSpawned;
        // true si cayó en la canasta.
        public event Action<FallingItem, bool> ItemResolved;

        // Una sola instrucción por partida (RNF-05): la categoría no cambia.
        public int TargetCategory { get; }
        public float BasketX { get; private set; }
        public float BasketHalfWidth => tuning.BasketHalfWidth;
        public IReadOnlyList<FallingItem> Items => items;

        public override void OnStart()
        {
            tuning = AtrapaTuning.For(Context.Difficulty);
            nextSpawnAt = Context.ElapsedSeconds + FirstSpawnDelaySeconds;
        }

        public override void OnTick(float deltaSeconds)
        {
            float now = Context.ElapsedSeconds;
            MoveBasket(deltaSeconds);
            Fall(deltaSeconds, now);
            while (now >= nextSpawnAt)
            {
                Spawn(nextSpawnAt);
                nextSpawnAt += tuning.SpawnIntervalSeconds;
            }
        }

        // Posición, no velocidad: inclinar del todo lleva la canasta al borde.
        private void MoveBasket(float deltaSeconds)
        {
            float limit = 1f - tuning.BasketHalfWidth;
            float target = Math.Max(-limit, Math.Min(limit, Context.GetTilt(PlayerSlot).X * limit));
            float follow = 1f - (float)Math.Exp(-BasketFollowRate * deltaSeconds);
            BasketX += (target - BasketX) * follow;
        }

        private void Fall(float deltaSeconds, float now)
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                FallingItem item = items[i];
                item.Y -= deltaSeconds / tuning.FallSeconds;
                bool underBasket = Math.Abs(item.X - BasketX) <= tuning.BasketHalfWidth + ItemRadius;
                if (underBasket && item.AlignedAt == null) item.AlignedAt = now;
                if (item.Y > 0f) continue;

                items.RemoveAt(i);
                Resolve(item, underBasket);
            }
        }

        private void Resolve(FallingItem item, bool caught)
        {
            float? reaction = item.AlignedAt - item.SpawnedAt;
            if (caught)
            {
                Context.Report(item.IsTarget ? TrialOutcome.Hit : TrialOutcome.Error, PlayerSlot, reaction);
            }
            else if (item.IsTarget)
            {
                Context.Report(TrialOutcome.Omission, PlayerSlot);
            }
            // Dejar pasar un distractor es lo correcto: no se reporta nada.
            ItemResolved?.Invoke(item, caught);
        }

        private void Spawn(float at)
        {
            bool isTarget = random.NextDouble() < tuning.TargetRatio;
            int category = isTarget
                ? TargetCategory
                : (TargetCategory + 1 + random.Next(CategoryCount - 1)) % CategoryCount;
            float x = (float)(random.NextDouble() * 2.0 - 1.0) * SpawnMargin;

            var item = new FallingItem(nextId++, category, isTarget, x, at);
            items.Add(item);
            ItemSpawned?.Invoke(item);
        }
    }
}
