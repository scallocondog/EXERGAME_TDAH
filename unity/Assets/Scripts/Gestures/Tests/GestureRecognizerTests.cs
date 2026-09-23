// RF-05, RF-06: pruebas del reconocedor de gestos (CU-03, CU-06, CU-07)
using NUnit.Framework;

namespace MoviMente.Gestures.Tests
{
    public sealed class GestureRecognizerTests
    {
        private static readonly Vec3 Push = new Vec3(20f, 0f, 0f);

        private FakePad pad;

        [SetUp]
        public void SetUp()
        {
            pad = new FakePad();
            pad.Hold(300, beta: 45f, gamma: 0f);
            pad.Recognizer.Calibrate();
            pad.Events.Clear();
        }

        [Test]
        public void InclinarDerecha_EmiteUnaSolaVezMientrasSeMantiene()
        {
            pad.Hold(1000, gamma: 25f);

            Assert.That(pad.Count(GestureType.TiltRight), Is.EqualTo(1));
            Assert.That(pad.Last(GestureType.TiltRight).Intensity, Is.EqualTo(25f / 40f).Within(0.01f));
        }

        [Test]
        public void Inclinar_SeRearmaSoloAlVolverCercaDelCentro()
        {
            pad.Hold(200, gamma: 25f);
            pad.Hold(200, gamma: 15f);
            pad.Hold(200, gamma: 25f);
            Assert.That(pad.Count(GestureType.TiltRight), Is.EqualTo(1));

            pad.Hold(200, gamma: 5f);
            pad.Hold(200, gamma: 25f);
            Assert.That(pad.Count(GestureType.TiltRight), Is.EqualTo(2));
        }

        [Test]
        public void Inclinar_DeDerechaAIzquierdaDirecto()
        {
            pad.Hold(200, gamma: 25f);
            pad.Hold(200, gamma: -25f);

            Assert.That(pad.Count(GestureType.TiltRight), Is.EqualTo(1));
            Assert.That(pad.Count(GestureType.TiltLeft), Is.EqualTo(1));
        }

        [Test]
        public void InclinarAdelanteYAtras_UsaBetaRespectoDeLaNeutra()
        {
            pad.Hold(200, beta: 20f);
            Assert.That(pad.Count(GestureType.TiltForward), Is.EqualTo(1));

            pad.Hold(200, beta: 45f);
            pad.Hold(200, beta: 70f);
            Assert.That(pad.Count(GestureType.TiltBack), Is.EqualTo(1));
        }

        [TestCase(2f, 0f)]
        [TestCase(22f, 0.5f)]
        [TestCase(60f, 1f)]
        [TestCase(-22f, -0.5f)]
        [TestCase(-60f, -1f)]
        public void EjeContinuo_ConZonaMuertaYTope(float gamma, float expectedX)
        {
            pad.Hold(50, gamma: gamma);

            Assert.That(pad.Recognizer.Tilt.X, Is.EqualTo(expectedX).Within(0.01f));
            Assert.That(pad.Recognizer.Tilt.Y, Is.EqualTo(0f));
        }

        [Test]
        public void Recentrar_TomaLaPoseActualComoNeutra()
        {
            pad.Hold(500, beta: 80f, gamma: 18f);
            pad.Events.Clear();

            Assert.That(pad.Recognizer.Calibrate(), Is.True);
            pad.Hold(100);
            Assert.That(pad.Recognizer.Tilt.X, Is.EqualTo(0f));
            Assert.That(pad.Recognizer.Tilt.Y, Is.EqualTo(0f));
            Assert.That(pad.Events, Is.Empty);

            pad.Hold(200, gamma: 18f + 25f);
            Assert.That(pad.Count(GestureType.TiltRight), Is.EqualTo(1));
        }

        [Test]
        public void Calibrar_SinLecturas_NoHaceNada()
        {
            var fresh = new FakePad();

            Assert.That(fresh.Recognizer.Calibrate(), Is.False);
            Assert.That(fresh.Recognizer.IsCalibrated, Is.False);
        }

        [Test]
        public void Calibrar_PromediaBienCercaDe180Grados()
        {
            var flipped = new FakePad();
            for (int i = 0; i < 20; i++) flipped.Hold(FakePad.FrameMs, beta: i % 2 == 0 ? 179f : -179f);
            flipped.Recognizer.Calibrate();

            flipped.Hold(100, beta: 180f);
            Assert.That(flipped.Recognizer.Tilt.Y, Is.EqualTo(0f));
        }

        [Test]
        public void SinCalibrar_LaPrimeraLecturaHaceDeNeutra()
        {
            var fresh = new FakePad();
            fresh.Hold(500, beta: 80f, gamma: 30f);

            Assert.That(fresh.Recognizer.IsCalibrated, Is.False);
            Assert.That(fresh.Count(GestureType.TiltRight), Is.Zero);
            Assert.That(fresh.Count(GestureType.TiltBack), Is.Zero);
        }

        [Test]
        public void Swing_UnGolpeEsUnSoloSwing()
        {
            pad.Jolt(Push, frames: 3);
            pad.Hold(300);

            Assert.That(pad.Count(GestureType.Swing), Is.EqualTo(1));
            Assert.That(pad.Last(GestureType.Swing).Intensity, Is.GreaterThan(0.4f));
        }

        [Test]
        public void Swing_DosGolpesSeparadosSonDosSwings()
        {
            pad.Jolt(Push, frames: 2);
            pad.Hold(500);
            pad.Jolt(Push, frames: 2);
            pad.Hold(100);

            Assert.That(pad.Count(GestureType.Swing), Is.EqualTo(2));
        }

        [Test]
        public void MovimientoSuave_NoEsSwingNiSacudida()
        {
            for (int i = 0; i < 10; i++)
            {
                pad.Jolt(new Vec3(i % 2 == 0 ? 4f : -4f, 0f, 0f), frames: 3);
            }

            Assert.That(pad.Count(GestureType.Swing), Is.Zero);
            Assert.That(pad.Count(GestureType.Shake), Is.Zero);
        }

        [Test]
        public void Sacudir_PicosAlternadosRapidos()
        {
            for (int i = 0; i < 6; i++)
            {
                pad.Jolt(new Vec3(i % 2 == 0 ? 11f : -11f, 0f, 0f), frames: 2);
                pad.Hold(FakePad.FrameMs * 3);
            }

            Assert.That(pad.Count(GestureType.Shake), Is.GreaterThanOrEqualTo(1));
            Assert.That(pad.Count(GestureType.Swing), Is.Zero);
        }

        [Test]
        public void Sacudir_PicosLentosNoCuentan()
        {
            for (int i = 0; i < 4; i++)
            {
                pad.Jolt(new Vec3(i % 2 == 0 ? 12f : -12f, 0f, 0f));
                pad.Hold(500);
            }

            Assert.That(pad.Count(GestureType.Shake), Is.Zero);
        }

        [Test]
        public void Estable_SeAvisaUnaVezTrasSostenerQuieto()
        {
            pad.Hold(1600);
            Assert.That(pad.Count(GestureType.Steady), Is.EqualTo(1));
            Assert.That(pad.Recognizer.IsSteady, Is.True);

            pad.Hold(2000);
            Assert.That(pad.Count(GestureType.Steady), Is.EqualTo(1));

            pad.Jolt(Push);
            Assert.That(pad.Recognizer.IsSteady, Is.False);

            pad.Hold(1700);
            Assert.That(pad.Count(GestureType.Steady), Is.EqualTo(2));
        }

        [Test]
        public void Estable_ToleraUnTemblorPequeno()
        {
            for (int i = 0; i < 100; i++) pad.Hold(FakePad.FrameMs, gamma: i % 2 == 0 ? 2f : -2f);

            Assert.That(pad.Count(GestureType.Steady), Is.EqualTo(1));
        }

        [Test]
        public void Estable_NoMientrasSeInclinaDespacio()
        {
            for (int i = 0; i < 100; i++) pad.Hold(FakePad.FrameMs, gamma: i * 0.3f);

            Assert.That(pad.Count(GestureType.Steady), Is.Zero);
        }

        [Test]
        public void LosEventosLlevanSlotYTimestampDelMando()
        {
            var second = new FakePad(slot: 2);
            second.Hold(200, gamma: 0f);
            second.Hold(100, gamma: 30f);

            GestureEvent tilt = second.Last(GestureType.TiltRight);
            Assert.That(tilt.Slot, Is.EqualTo(2));
            Assert.That(tilt.Timestamp, Is.GreaterThan(1_000_000));
            Assert.That(tilt.Timestamp, Is.LessThanOrEqualTo(second.Now));
        }

        [Test]
        public void InvertirX_CambiaElSentido()
        {
            pad.Settings.InvertX = true;
            pad.Hold(200, gamma: 25f);

            Assert.That(pad.Count(GestureType.TiltLeft), Is.EqualTo(1));
            Assert.That(pad.Count(GestureType.TiltRight), Is.Zero);
        }

        [Test]
        public void LosUmbralesSeLeenEnVivo()
        {
            pad.Settings.TiltEnterDegrees = 30f;
            pad.Hold(200, gamma: 25f);

            Assert.That(pad.Count(GestureType.TiltRight), Is.Zero);
        }

        [Test]
        public void RelojQueRetrocede_DescartaLecturasViejasAlCalibrar()
        {
            pad.Hold(300, gamma: 30f);
            pad.Now = 0;
            pad.Hold(100, gamma: 0f);
            pad.Recognizer.Calibrate();

            pad.Hold(50, gamma: 0f);
            Assert.That(pad.Recognizer.Tilt.X, Is.EqualTo(0f));
        }

        [Test]
        public void RelojQueRetrocede_NoInventaGestos()
        {
            pad.Hold(200);
            pad.Now = 0;
            pad.Jolt(new Vec3(0f, 5f, 0f));
            pad.Hold(200);

            Assert.That(pad.Count(GestureType.Swing), Is.Zero);
            Assert.That(pad.Count(GestureType.Shake), Is.Zero);
        }
    }
}
