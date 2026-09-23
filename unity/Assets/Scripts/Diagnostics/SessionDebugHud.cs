// RF-07, RF-10, RF-19: HUD de prueba para jugar un minijuego antes de la UI final
using MoviMente.Games;
using UnityEngine;

namespace MoviMente.Diagnostics
{
    // Herramienta del equipo mientras se construye la UI de Assets/UI/ (Santiago):
    // muestra en qué etapa está la partida y, si falta el mando, el QR para unirse.
    public sealed class SessionDebugHud : MonoBehaviour
    {
        private const float ReferenceHeight = 1080f;

        private GUIStyle big;
        private GUIStyle text;

        private void OnGUI()
        {
            CoreSystems systems = CoreSystems.Instance;
            GameSession session = systems != null ? systems.Runner.Session : null;
            if (session == null) return;

            float scale = Screen.height / ReferenceHeight;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            big ??= new GUIStyle(GUI.skin.label) { fontSize = 96, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            text ??= new GUIStyle(GUI.skin.label) { fontSize = 40, alignment = TextAnchor.MiddleCenter };

            float width = Screen.width / scale;
            var center = new Rect(0f, ReferenceHeight * 0.3f, width, 140f);
            var below = new Rect(0f, ReferenceHeight * 0.3f + 140f, width, 60f);

            GUI.Label(new Rect(40f, 30f, 600f, 60f), $"{Mathf.CeilToInt(session.RemainingSeconds)} s", text);

            switch (session.State)
            {
                case GameState.Instructions:
                    GUI.Label(center, "¡Prepárate!", big);
                    GUI.Label(below, "Toca el botón del mando para empezar", text);
                    break;
                case GameState.Countdown:
                    GUI.Label(center, Mathf.CeilToInt(session.CountdownRemaining).ToString(), big);
                    break;
                case GameState.Paused when session.IsWaitingForReconnection:
                    GUI.Label(center, "Esperando tu mando…", big);
                    DrawJoinHint(systems, width);
                    break;
                case GameState.Paused:
                    GUI.Label(center, "Pausa", big);
                    GUI.Label(below, "Toca pausa otra vez para seguir", text);
                    break;
                case GameState.Finished:
                    GUI.Label(center, "¡Muy bien!", big);
                    break;
            }
        }

        private void DrawJoinHint(CoreSystems systems, float width)
        {
            string room = systems.Connection.Link.RoomCode ?? "----";
            GUI.Label(new Rect(0f, ReferenceHeight * 0.3f + 140f, width, 60f), $"Sala {room}", text);
            Texture2D qr = systems.Qr.Qr;
            if (qr != null) GUI.DrawTexture(new Rect(width / 2f - 150f, ReferenceHeight * 0.3f + 220f, 300f, 300f), qr);
        }
    }
}
