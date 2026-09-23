// RF-01, RF-04, RF-07: interpreta lo que llega del servidor y arma lo que envía Unity
using System;
using System.Collections.Generic;
using MoviMente.Gestures;
using UnityEngine;

namespace MoviMente.Net
{
    // Sin sockets ni MonoBehaviour: se prueba entregándole JSON a mano.
    public sealed class GameLink
    {
        private readonly HashSet<int> connectedPads = new HashSet<int>();

        public event Action<string, string> RoomCreated;
        public event Action RoomClosed;
        public event Action<int, bool> PadConnectionChanged;
        public event Action<int, MotionSample> MotionReceived;
        public event Action<int, string> ButtonPressed;
        public event Action<int> CalibrateRequested;
        public event Action<string> ErrorReceived;

        public string RoomCode { get; private set; }
        public string JoinUrl { get; private set; }

        public bool IsPadConnected(int slot) => connectedPads.Contains(slot);

        public static string CreateRoomMessage() => "{\"t\":\"create_room\"}";

        public static string HapticMessage(int slot, string pattern) =>
            JsonUtility.ToJson(new OutgoingHaptic { slot = slot, pattern = pattern });

        public static string StateMessage(int slot, string value) =>
            JsonUtility.ToJson(new OutgoingState { slot = slot, value = value });

        public void Receive(string json)
        {
            WireMessage msg;
            try
            {
                msg = JsonUtility.FromJson<WireMessage>(json);
            }
            catch (ArgumentException)
            {
                ErrorReceived?.Invoke("json_malformado");
                return;
            }
            if (msg == null || string.IsNullOrEmpty(msg.t)) return;

            switch (msg.t)
            {
                case "room_created":
                    RoomCode = msg.room;
                    JoinUrl = msg.url;
                    RoomCreated?.Invoke(msg.room, msg.url);
                    break;
                case "pad_state":
                    SetPadConnected(msg.slot, msg.value == "connected");
                    break;
                case "motion":
                    MotionReceived?.Invoke(msg.slot, ToSample(msg));
                    break;
                case "button":
                    ButtonPressed?.Invoke(msg.slot, msg.action);
                    break;
                case "calibrate":
                    CalibrateRequested?.Invoke(msg.slot);
                    break;
                case "error":
                    ErrorReceived?.Invoke(msg.code);
                    break;
            }
        }

        // Si se cae la conexión con el servidor, este cierra la sala: los mandos
        // quedan sueltos y habrá que crear otra al reconectar.
        public void ConnectionLost()
        {
            foreach (int slot in new List<int>(connectedPads)) SetPadConnected(slot, false);
            bool hadRoom = RoomCode != null;
            RoomCode = null;
            JoinUrl = null;
            if (hadRoom) RoomClosed?.Invoke();
        }

        private void SetPadConnected(int slot, bool connected)
        {
            bool changed = connected ? connectedPads.Add(slot) : connectedPads.Remove(slot);
            if (changed) PadConnectionChanged?.Invoke(slot, connected);
        }

        private static MotionSample ToSample(WireMessage msg)
        {
            WireOrientation ori = msg.ori ?? new WireOrientation();
            WireVector acc = msg.acc ?? new WireVector();
            return new MotionSample(msg.ts, ori.alpha, ori.beta, ori.gamma, new Vec3(acc.x, acc.y, acc.z));
        }
    }
}
