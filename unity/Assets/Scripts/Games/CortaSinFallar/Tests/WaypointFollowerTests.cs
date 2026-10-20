// RF-13: pruebas de las rutas por waypoints
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace MoviMente.Games.CortaSinFallar.Tests
{
    public sealed class WaypointFollowerTests
    {
        private const float Frame = 1f / 60f;
        private const float Speed = 0.5f;

        private static readonly FieldPoint[] Route =
        {
            new FieldPoint(-1f, 0.3f),
            new FieldPoint(-0.5f, 0.7f),
            new FieldPoint(0f, 0.3f),
            new FieldPoint(0.5f, 0.7f),
            new FieldPoint(1f, 0.3f),
        };

        [Test]
        public void ArrancaEnElPrimerPuntoYVaHaciaElSegundo()
        {
            var follower = new WaypointFollower(Route, Speed);

            Assert.That(follower.Position.X, Is.EqualTo(-1f));
            Assert.That(follower.CurrentIndex, Is.EqualTo(1));
            Assert.That(follower.Finished, Is.False);
        }

        [Test]
        public void PasaPorCadaWaypointEnOrden()
        {
            var follower = new WaypointFollower(Route, Speed);
            var reached = new List<int>();

            for (int i = 0; i < 10000 && !follower.Finished; i++)
            {
                int before = follower.CurrentIndex;
                follower.Advance(Frame);
                for (int index = before; index < follower.CurrentIndex; index++) reached.Add(index);
            }

            Assert.That(reached, Is.EqualTo(new[] { 1, 2, 3, 4 }));
            Assert.That(follower.Position.X, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(follower.Position.Y, Is.EqualTo(0.3f).Within(1e-5f));
        }

        [Test]
        public void AvanzaAVelocidadConstanteAunqueDoble()
        {
            var follower = new WaypointFollower(Route, Speed);
            FieldPoint previous = follower.Position;
            float step = Speed * Frame;

            while (!follower.Finished)
            {
                follower.Advance(Frame);
                if (follower.Finished) break;
                float moved = previous.DistanceTo(follower.Position);
                // Al doblar en un waypoint la línea recta acorta un poco; nunca se pasa.
                Assert.That(moved, Is.LessThanOrEqualTo(step + 1e-5f));
                Assert.That(moved, Is.GreaterThan(step * 0.7f));
                previous = follower.Position;
            }
        }

        [Test]
        public void ElRecorridoTotalDuraLoQueMideLaRutaDivididoLaVelocidad()
        {
            float length = 0f;
            for (int i = 1; i < Route.Length; i++) length += Route[i - 1].DistanceTo(Route[i]);
            var follower = new WaypointFollower(Route, Speed);

            int frames = 0;
            while (!follower.Finished) { follower.Advance(Frame); frames++; }

            Assert.That(frames * Frame, Is.EqualTo(length / Speed).Within(Frame));
        }

        [Test]
        public void LoQueSobraAlLlegarSeUsaHaciaElSiguiente()
        {
            var straight = new[] { new FieldPoint(0f, 0f), new FieldPoint(1f, 0f), new FieldPoint(2f, 0f) };
            var follower = new WaypointFollower(straight, 1f);

            // 4 pasos de 0,3 = 1,2: llega al waypoint en 1,0 y sigue 0,2 hacia el siguiente.
            for (int i = 0; i < 4; i++) follower.Advance(0.3f);

            Assert.That(follower.Position.X, Is.EqualTo(1.2f).Within(1e-5f));
            Assert.That(follower.CurrentIndex, Is.EqualTo(2));
        }

        [Test]
        public void AlTerminarSeQuedaQuieto()
        {
            var follower = new WaypointFollower(Route, Speed);
            while (!follower.Finished) follower.Advance(0.5f);
            FieldPoint end = follower.Position;

            follower.Advance(1f);
            Assert.That(follower.Position.X, Is.EqualTo(end.X));
            Assert.That(follower.Position.Y, Is.EqualTo(end.Y));
        }

        [Test]
        public void RutaInvalida_SeRechaza()
        {
            Assert.Throws<ArgumentException>(() => new WaypointFollower(new[] { new FieldPoint(0f, 0f) }, Speed));
            Assert.Throws<ArgumentOutOfRangeException>(() => new WaypointFollower(Route, 0f));
        }
    }
}
