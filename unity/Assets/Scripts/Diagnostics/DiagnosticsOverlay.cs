// RF-01, RF-06, RF-07: panel de diagnóstico para calibrar umbrales con el celular real
using System.Collections.Generic;
using MoviMente.Gestures;
using MoviMente.Net;
using UnityEngine;

namespace MoviMente.Diagnostics
{
    // Herramienta del equipo, no pantalla del jugador: IMGUI para no depender
    // de la UI de Assets/UI/ (Santiago).
    public sealed class DiagnosticsOverlay : MonoBehaviour
    {
        private const int Slots = 2;
        private const int LogSize = 12;
        private const float ReferenceHeight = 1080f;

        [SerializeField] private ServerConnection connection;
        [SerializeField] private PadInputHub input;
        [SerializeField] private RoomQrLoader qr;

        private readonly LinkedList<string> log = new LinkedList<string>();
        private readonly int[] samplesThisSecond = new int[Slots + 1];
        private readonly int[] samplesPerSecond = new int[Slots + 1];
        private readonly string[] lastButton = new string[Slots + 1];
        private float windowStart;
        private GUIStyle text;
        private GUIStyle big;

        private void OnEnable()
        {
            connection.Link.MotionReceived += CountSample;
            connection.Link.PadConnectionChanged += OnPadConnection;
            connection.Link.ErrorReceived += OnError;
            input.GestureDetected += OnGesture;
            input.ButtonPressed += OnButton;
        }

        private void OnDisable()
        {
            connection.Link.MotionReceived -= CountSample;
            connection.Link.PadConnectionChanged -= OnPadConnection;
            connection.Link.ErrorReceived -= OnError;
            input.GestureDetected -= OnGesture;
            input.ButtonPressed -= OnButton;
        }

        private void Update()
        {
            if (Time.unscaledTime - windowStart < 1f) return;
            windowStart = Time.unscaledTime;
            for (int slot = 1; slot <= Slots; slot++)
            {
                samplesPerSecond[slot] = samplesThisSecond[slot];
                samplesThisSecond[slot] = 0;
            }
        }

        private void CountSample(int slot, MotionSample sample)
        {
            if (slot >= 1 && slot <= Slots) samplesThisSecond[slot]++;
        }

        private void OnPadConnection(int slot, bool connected) =>
            Log($"Mando {slot} {(connected ? "conectado" : "desconectado")}");

        private void OnError(string code) => Log($"Error del servidor: {code}");

        private void OnGesture(GestureEvent gesture) =>
            Log($"Mando {gesture.Slot}: {gesture.Type} ({gesture.Intensity:0.00})");

        private void OnButton(int slot, string action)
        {
            if (slot >= 1 && slot <= Slots) lastButton[slot] = action;
            Log($"Mando {slot}: botón {action}");
        }

        private void Log(string line)
        {
            log.AddFirst($"{Time.unscaledTime,7:0.0}s  {line}");
            if (log.Count > LogSize) log.RemoveLast();
        }

        private void OnGUI()
        {
            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            text ??= new GUIStyle(GUI.skin.label) { fontSize = 28 };
            big ??= new GUIStyle(GUI.skin.label) { fontSize = 120, fontStyle = FontStyle.Bold };

            float width = Screen.width / scale;
            GUILayout.BeginArea(new Rect(40f, 30f, width - 80f, ReferenceHeight - 60f));
            GUILayout.BeginHorizontal();

            GUILayout.BeginVertical(GUILayout.Width(560f));
            DrawRoom();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            for (int slot = 1; slot <= Slots; slot++) DrawPad(slot);
            GUILayout.Space(20f);
            foreach (string line in log) GUILayout.Label(line, text);
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }

        private void DrawRoom()
        {
            GUILayout.Label($"Servidor: {StatusText(connection.Status)}", text);
            GUILayout.Label(connection.ServerUrl, text);
            GUILayout.Label(connection.Link.RoomCode ?? "----", big);
            if (qr != null && qr.Qr != null)
            {
                Rect area = GUILayoutUtility.GetRect(512f, 512f, GUILayout.ExpandWidth(false));
                GUI.DrawTexture(area, qr.Qr, ScaleMode.ScaleToFit);
            }
            GUILayout.Label(connection.Link.JoinUrl ?? "Esperando sala…", text);
        }

        private void DrawPad(int slot)
        {
            bool connected = connection.Link.IsPadConnected(slot);
            string state = connected ? $"conectado · {samplesPerSecond[slot]} Hz" : "sin mando";
            string calibrated = input.IsCalibrated(slot) ? "calibrado" : "sin calibrar";
            string steady = input.IsSteady(slot) ? " · estable" : "";
            GUILayout.Label($"Mando {slot}: {state} · {calibrated}{steady} · último botón: {lastButton[slot] ?? "—"}", text);

            TiltAxes tilt = input.GetTilt(slot);
            DrawAxis("X", tilt.X);
            DrawAxis("Y", tilt.Y);
        }

        private void DrawAxis(string label, float value)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label} {value,5:+0.00;-0.00; 0.00}", text, GUILayout.Width(170f));
            Rect bar = GUILayoutUtility.GetRect(600f, 28f, GUILayout.ExpandWidth(false));
            GUI.Box(bar, GUIContent.none);
            float center = bar.x + bar.width / 2f;
            float half = bar.width / 2f * Mathf.Abs(value);
            var fill = new Rect(value >= 0f ? center : center - half, bar.y + 4f, half, bar.height - 8f);
            GUI.DrawTexture(fill, Texture2D.whiteTexture);
            GUILayout.EndHorizontal();
        }

        private static string StatusText(ConnectionStatus status) => status switch
        {
            ConnectionStatus.Connected => "conectado",
            ConnectionStatus.Connecting => "conectando…",
            _ => "sin conexión (reintentando)",
        };
    }
}
