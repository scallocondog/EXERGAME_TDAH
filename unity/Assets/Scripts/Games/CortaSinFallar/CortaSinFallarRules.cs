// RF-13: cortar con swing las frutas y no cortar lo prohibido (reacción e inhibición)
using System;
using System.Collections.Generic;
using MoviMente.Gestures;

namespace MoviMente.Games.CortaSinFallar
{
    public sealed class CortaSinFallarRules : MinigameRules
    {
        public const int CategoryCount = 3;
        public const int PlayerSlot = 1;
        // Zona de corte: franja horizontal de todo el ancho. El swing no apunta,
        // así que corta lo que esté en la franja en ese momento.
        public const float BandLow = 0.55f;
        public const float BandHigh = 0.85f;

        private const float FirstLaunchDelaySeconds = 0.8f;
        private const float LaunchSpread = 0.8f;
        private const float ApexDrift = 0.35f;
        private const float ApexMarginX = 0.85f;
        // La cima siempre queda dentro de la franja, con margen: así cada fruta
        // pasa una sola vez por la zona de corte, subiendo y bajando.
        private const float ApexMinY = 0.62f;
        private const float ApexMaxY = 0.78f;
        private const float RouteOutsideX = 1.15f;

        private readonly Random random;
        private readonly List<FlyingObject> objects = new List<FlyingObject>();
        private CortaTuning tuning = CortaTuning.Medium;
        private float nextLaunchAt;
        private float nextForbiddenAt;
        private int nextId;

        public CortaSinFallarRules(Random random)
        {
            this.random = random ?? throw new ArgumentNullException(nameof(random));
        }

        public event Action<FlyingObject> ObjectSpawned;
        public event Action<FlyingObject> ObjectCut;
        // Salió del campo sin que lo cortaran.
        public event Action<FlyingObject> ObjectLeft;

        public IReadOnlyList<FlyingObject> Objects => objects;

        public override void OnStart()
        {
            tuning = CortaTuning.For(Context.Difficulty);
            nextLaunchAt = Context.ElapsedSeconds + FirstLaunchDelaySeconds;
            // El primer prohibido llega después de unas frutas, para que se entienda primero qué cortar.
            nextForbiddenAt = Context.ElapsedSeconds + tuning.ForbiddenIntervalSeconds;
        }

        public override void OnTick(float deltaSeconds)
        {
            float now = Context.ElapsedSeconds;
            Move(now, deltaSeconds);
            while (now >= nextLaunchAt)
            {
                LaunchFruit(nextLaunchAt);
                nextLaunchAt += tuning.LaunchIntervalSeconds;
            }
            while (now >= nextForbiddenAt)
            {
                SpawnForbidden(nextForbiddenAt);
                nextForbiddenAt += tuning.ForbiddenIntervalSeconds;
            }
        }

        public override void OnGesture(GestureEvent gesture)
        {
            if (gesture.Type != GestureType.Swing) return;

            float now = Context.ElapsedSeconds;
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                FlyingObject target = objects[i];
                if (!IsCuttable(target, now)) continue;

                objects.RemoveAt(i);
                if (target.Kind == FlyingKind.Valid)
                {
                    // Tiempo de reacción: desde que la fruta entró a la zona de corte.
                    Context.Report(TrialOutcome.Hit, gesture.Slot, now - target.WindowStart);
                }
                else
                {
                    Context.Report(TrialOutcome.Error, gesture.Slot);
                }
                ObjectCut?.Invoke(target);
            }
        }

        // La fruta se juzga con la ventana predicha; el prohibido, con su posición.
        public static bool IsCuttable(FlyingObject target, float now) => target.Kind == FlyingKind.Valid
            ? now >= target.WindowStart && now <= target.WindowEnd
            : target.Position.Y >= BandLow && target.Position.Y <= BandHigh;

        private void Move(float now, float deltaSeconds)
        {
            for (int i = objects.Count - 1; i >= 0; i--)
            {
                FlyingObject target = objects[i];
                bool left;
                if (target.Kind == FlyingKind.Valid)
                {
                    float t = now - target.SpawnedAt;
                    target.Position = target.Projectile.PositionAt(t);
                    left = t >= target.Projectile.FlightTime;
                }
                else
                {
                    target.Route.Advance(deltaSeconds);
                    target.Position = target.Route.Position;
                    left = target.Route.Finished;
                }
                if (!left) continue;

                objects.RemoveAt(i);
                // Dejar caer una fruta es omisión; dejar pasar un prohibido es lo correcto.
                if (target.Kind == FlyingKind.Valid) Context.Report(TrialOutcome.Omission, PlayerSlot);
                ObjectLeft?.Invoke(target);
            }
        }

        private void LaunchFruit(float at)
        {
            float startX = Range(-LaunchSpread, LaunchSpread);
            float apexX = Math.Max(-ApexMarginX, Math.Min(ApexMarginX, startX + Range(-ApexDrift, ApexDrift)));
            float apexY = Range(ApexMinY, ApexMaxY);
            Projectile projectile = Projectile.Aim(new FieldPoint(startX, 0f), new FieldPoint(apexX, apexY), tuning.Gravity);

            // Predicción: entra a la franja subiendo por BandLow y sale bajando por BandLow.
            projectile.TryTimesAtHeight(BandLow, out float enters, out float exits);
            var fruit = new FlyingObject(nextId++, random.Next(CategoryCount), at, projectile, at + enters, at + exits);
            Add(fruit);
        }

        // Ruta en zigzag de un lado al otro que cruza la zona de corte dos veces.
        private void SpawnForbidden(float at)
        {
            float direction = random.Next(2) == 0 ? 1f : -1f;
            var waypoints = new FieldPoint[5];
            for (int i = 0; i < waypoints.Length; i++)
            {
                float x = direction * (-RouteOutsideX + 2f * RouteOutsideX * i / (waypoints.Length - 1));
                bool inBand = i % 2 == 1;
                float y = inBand ? Range(BandLow + 0.05f, BandHigh - 0.05f) : Range(0.25f, 0.45f);
                waypoints[i] = new FieldPoint(x, y);
            }
            Add(new FlyingObject(nextId++, at, new WaypointFollower(waypoints, tuning.ForbiddenSpeed)));
        }

        private void Add(FlyingObject target)
        {
            objects.Add(target);
            ObjectSpawned?.Invoke(target);
        }

        private float Range(float min, float max) => min + (float)random.NextDouble() * (max - min);
    }
}
