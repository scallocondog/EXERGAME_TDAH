// RF-01, RF-04, RF-07: pruebas del intérprete de mensajes de Unity
using System.Collections.Generic;
using MoviMente.Gestures;
using NUnit.Framework;

namespace MoviMente.Net.Tests
{
    public sealed class GameLinkTests
    {
        private GameLink link;

        [SetUp]
        public void SetUp() => link = new GameLink();

        [Test]
        public void RoomCreated_GuardaCodigoYUrl()
        {
            string code = null, url = null;
            link.RoomCreated += (c, u) => { code = c; url = u; };

            link.Receive("{\"t\":\"room_created\",\"room\":\"A7K2\",\"url\":\"https://192.168.1.10:3443/?room=A7K2\"}");

            Assert.That(code, Is.EqualTo("A7K2"));
            Assert.That(url, Is.EqualTo("https://192.168.1.10:3443/?room=A7K2"));
            Assert.That(link.RoomCode, Is.EqualTo("A7K2"));
        }

        [Test]
        public void Motion_SeConvierteEnMuestraConSlot()
        {
            int slot = 0;
            MotionSample sample = default;
            link.MotionReceived += (s, m) => { slot = s; sample = m; };

            link.Receive("{\"t\":\"motion\",\"room\":\"A7K2\",\"seq\":128,\"ts\":1737590000123," +
                "\"ori\":{\"alpha\":12.4,\"beta\":-3.1,\"gamma\":45.0},\"acc\":{\"x\":0.12,\"y\":9.71,\"z\":0.03},\"slot\":2}");

            Assert.That(slot, Is.EqualTo(2));
            Assert.That(sample.Timestamp, Is.EqualTo(1737590000123L));
            Assert.That(sample.Beta, Is.EqualTo(-3.1f).Within(1e-4f));
            Assert.That(sample.Gamma, Is.EqualTo(45f).Within(1e-4f));
            Assert.That(sample.Acceleration.Y, Is.EqualTo(9.71f).Within(1e-4f));
        }

        [Test]
        public void PadState_AvisaSoloCuandoCambia()
        {
            var changes = new List<string>();
            link.PadConnectionChanged += (s, c) => changes.Add($"{s}:{c}");

            link.Receive("{\"t\":\"pad_state\",\"slot\":1,\"value\":\"connected\"}");
            link.Receive("{\"t\":\"pad_state\",\"slot\":1,\"value\":\"connected\"}");
            Assert.That(link.IsPadConnected(1), Is.True);

            link.Receive("{\"t\":\"pad_state\",\"slot\":1,\"value\":\"disconnected\"}");
            Assert.That(link.IsPadConnected(1), Is.False);
            Assert.That(changes, Is.EqualTo(new[] { "1:True", "1:False" }));
        }

        [Test]
        public void BotonesYCalibracion_LleganConSuSlot()
        {
            string action = null;
            int calibrated = 0;
            link.ButtonPressed += (s, a) => action = $"{s}:{a}";
            link.CalibrateRequested += s => calibrated = s;

            link.Receive("{\"t\":\"button\",\"room\":\"A7K2\",\"action\":\"pause\",\"slot\":1}");
            link.Receive("{\"t\":\"calibrate\",\"room\":\"A7K2\",\"slot\":2}");

            Assert.That(action, Is.EqualTo("1:pause"));
            Assert.That(calibrated, Is.EqualTo(2));
        }

        [Test]
        public void JsonRoto_NoRompeNada()
        {
            string error = null;
            link.ErrorReceived += e => error = e;

            Assert.DoesNotThrow(() => link.Receive("{no es json"));
            Assert.DoesNotThrow(() => link.Receive("{\"sin\":\"tipo\"}"));
            Assert.That(error, Is.EqualTo("json_malformado"));
        }

        [Test]
        public void PerderLaConexion_DesconectaMandosYCierraLaSala()
        {
            var changes = new List<string>();
            bool closed = false;
            link.PadConnectionChanged += (s, c) => changes.Add($"{s}:{c}");
            link.RoomClosed += () => closed = true;
            link.Receive("{\"t\":\"room_created\",\"room\":\"A7K2\",\"url\":\"u\"}");
            link.Receive("{\"t\":\"pad_state\",\"slot\":1,\"value\":\"connected\"}");

            link.ConnectionLost();

            Assert.That(closed, Is.True);
            Assert.That(link.RoomCode, Is.Null);
            Assert.That(link.IsPadConnected(1), Is.False);
            Assert.That(changes, Is.EqualTo(new[] { "1:True", "1:False" }));
        }

        [Test]
        public void MensajesSalientes_SiguenElProtocolo()
        {
            Assert.That(GameLink.CreateRoomMessage(), Is.EqualTo("{\"t\":\"create_room\"}"));
            Assert.That(GameLink.HapticMessage(2, HapticPattern.Hit), Is.EqualTo("{\"t\":\"haptic\",\"slot\":2,\"pattern\":\"hit\"}"));
            Assert.That(GameLink.StateMessage(1, PadScreenState.Paused), Is.EqualTo("{\"t\":\"state\",\"slot\":1,\"value\":\"paused\"}"));
        }

        [TestCase("ws://localhost:3444/unity", "A7K2", "http://localhost:3444/qr/A7K2")]
        [TestCase("wss://192.168.1.10:3443/unity", "A7K2", "https://192.168.1.10:3443/qr/A7K2")]
        [TestCase("https://192.168.1.10:3443/unity", "A7K2", "https://192.168.1.10:3443/qr/A7K2")]
        public void UrlDelQr_SeDerivaDeLaUrlDelServidor(string serverUrl, string room, string expected)
        {
            Assert.That(ServerUrls.Qr(serverUrl, room), Is.EqualTo(expected));
        }

        [TestCase("ws://localhost:3444/unity", "ws://localhost:3444/unity")]
        [TestCase("http://localhost:3444/unity", "ws://localhost:3444/unity")]
        [TestCase("https://192.168.1.10:3443/unity", "wss://192.168.1.10:3443/unity")]
        public void UrlDelWebSocket_AceptaHttpOWs(string serverUrl, string expected)
        {
            Assert.That(ServerUrls.WebSocket(serverUrl), Is.EqualTo(expected));
        }
    }
}
