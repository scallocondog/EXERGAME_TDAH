// RF-01, RF-07: conexión de Unity con el servidor por /unity, con reconexión
using System;
using System.Text;
using NativeWebSocket;
using UnityEngine;

namespace MoviMente.Net
{
    public enum ConnectionStatus
    {
        Connecting,
        Connected,
        Disconnected,
    }

    public sealed class ServerConnection : MonoBehaviour
    {
        // Los mismos tiempos que usa el mando (protocolo-ws.md §5).
        private static readonly float[] RetryDelaysSeconds = { 0.5f, 1f, 2f, 4f, 5f };

        [Tooltip("Puerto local del servidor, sin certificado: Unity y el servidor corren en la misma PC.")]
        [SerializeField] private string serverUrl = "ws://127.0.0.1:3444/unity";

        private WebSocket socket;
        private int attempt;
        private bool shuttingDown;

        public GameLink Link { get; } = new GameLink();
        public ConnectionStatus Status { get; private set; } = ConnectionStatus.Disconnected;
        public string ServerUrl => serverUrl;

        public event Action<ConnectionStatus> StatusChanged;

        public void Send(string json)
        {
            if (socket != null && socket.State == WebSocketState.Open) _ = socket.SendText(json);
        }

        public void SendHaptic(int slot, string pattern) => Send(GameLink.HapticMessage(slot, pattern));

        public void SendState(int slot, string value) => Send(GameLink.StateMessage(slot, value));

        private void Start() => Connect();

        private void OnDestroy()
        {
            shuttingDown = true;
            CancelInvoke();
            if (socket != null) _ = socket.Close();
        }

        private async void Connect()
        {
            SetStatus(ConnectionStatus.Connecting);
            var current = new WebSocket(ServerUrls.WebSocket(serverUrl));
            socket = current;

            current.OnOpen += () =>
            {
                attempt = 0;
                SetStatus(ConnectionStatus.Connected);
                Send(GameLink.CreateRoomMessage());
            };
            current.OnMessage += bytes => Link.Receive(Encoding.UTF8.GetString(bytes));
            current.OnError += error => Debug.LogWarning($"[Net] {serverUrl}: {error}");
            current.OnClose += _ => OnClosed(current);

            await current.Connect();
        }

        private void OnClosed(WebSocket closed)
        {
            // Los eventos llegan en diferido: ignorar sockets viejos o un objeto ya destruido.
            if (this == null || closed != socket) return;

            Link.ConnectionLost();
            SetStatus(ConnectionStatus.Disconnected);
            if (shuttingDown) return;

            float delay = RetryDelaysSeconds[Math.Min(attempt, RetryDelaysSeconds.Length - 1)];
            attempt++;
            Invoke(nameof(Connect), delay);
        }

        private void SetStatus(ConnectionStatus next)
        {
            if (Status == next) return;
            Status = next;
            StatusChanged?.Invoke(next);
        }
    }
}
