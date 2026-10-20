// RF-13: pruebas de la física predictiva (tiro parabólico resuelto al revés)
using System;
using NUnit.Framework;

namespace MoviMente.Games.CortaSinFallar.Tests
{
    public sealed class ProjectileTests
    {
        private const float Tolerance = 1e-4f;

        [TestCase(0f, 0f, 0.3f, 0.7f, 0.9f)]
        [TestCase(-0.8f, 0f, -0.5f, 0.62f, 1.8f)]
        [TestCase(0.6f, 0f, 0.85f, 0.78f, 1.3f)]
        public void Apuntar_DejaLaCimaDondeSePidio(float x0, float y0, float apexX, float apexY, float gravity)
        {
            Projectile shot = Projectile.Aim(new FieldPoint(x0, y0), new FieldPoint(apexX, apexY), gravity);

            Assert.That(shot.Apex.X, Is.EqualTo(apexX).Within(Tolerance));
            Assert.That(shot.Apex.Y, Is.EqualTo(apexY).Within(Tolerance));
            // En la cima la velocidad vertical es cero: un instante antes y después está más abajo.
            Assert.That(shot.PositionAt(shot.ApexTime - 0.01f).Y, Is.LessThan(apexY));
            Assert.That(shot.PositionAt(shot.ApexTime + 0.01f).Y, Is.LessThan(apexY));
        }

        [Test]
        public void ElVueloVuelveALaAlturaDeSalidaEnElDobleDelTiempoDeSubida()
        {
            Projectile shot = Projectile.Aim(new FieldPoint(-0.2f, 0f), new FieldPoint(0.1f, 0.7f), 0.9f);

            FieldPoint landing = shot.PositionAt(shot.FlightTime);
            Assert.That(shot.FlightTime, Is.EqualTo(2f * shot.ApexTime).Within(Tolerance));
            Assert.That(landing.Y, Is.EqualTo(0f).Within(Tolerance));
            // Simétrico: avanza lo mismo de lado subiendo que bajando.
            Assert.That(landing.X, Is.EqualTo(0.4f).Within(Tolerance));
        }

        [Test]
        public void MasGravedad_MismaCima_VueloMasCorto()
        {
            var start = new FieldPoint(0f, 0f);
            var apex = new FieldPoint(0.2f, 0.7f);

            Projectile slow = Projectile.Aim(start, apex, 0.9f);
            Projectile fast = Projectile.Aim(start, apex, 1.8f);

            Assert.That(fast.FlightTime, Is.LessThan(slow.FlightTime));
            Assert.That(fast.Apex.Y, Is.EqualTo(slow.Apex.Y).Within(Tolerance));
        }

        [Test]
        public void LosInstantesEnUnaAlturaCoincidenConLaPosicion()
        {
            Projectile shot = Projectile.Aim(new FieldPoint(0f, 0f), new FieldPoint(0.3f, 0.75f), 1.3f);

            Assert.That(shot.TryTimesAtHeight(0.55f, out float rising, out float falling), Is.True);
            Assert.That(shot.PositionAt(rising).Y, Is.EqualTo(0.55f).Within(Tolerance));
            Assert.That(shot.PositionAt(falling).Y, Is.EqualTo(0.55f).Within(Tolerance));
            Assert.That(rising, Is.LessThan(shot.ApexTime));
            Assert.That(falling, Is.GreaterThan(shot.ApexTime));
        }

        [Test]
        public void UnaAlturaPorEncimaDeLaCima_NoSeAlcanza()
        {
            Projectile shot = Projectile.Aim(new FieldPoint(0f, 0f), new FieldPoint(0f, 0.6f), 1f);

            Assert.That(shot.TryTimesAtHeight(0.9f, out _, out _), Is.False);
        }

        [Test]
        public void DatosImposibles_SeRechazan()
        {
            Assert.Throws<ArgumentException>(() => Projectile.Aim(new FieldPoint(0f, 0.5f), new FieldPoint(0f, 0.4f), 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => Projectile.Aim(new FieldPoint(0f, 0f), new FieldPoint(0f, 0.5f), 0f));
        }
    }
}
