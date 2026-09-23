// RF-01: descarga el QR de la sala para la pantalla de inicio
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace MoviMente.Net
{
    public sealed class RoomQrLoader : MonoBehaviour
    {
        [SerializeField] private ServerConnection connection;
        [Tooltip("RawImage de la pantalla de sala (UI de Santiago). Opcional.")]
        [SerializeField] private RawImage target;

        public Texture2D Qr { get; private set; }

        public event Action<Texture2D> QrChanged;

        private void OnEnable()
        {
            connection.Link.RoomCreated += OnRoomCreated;
            connection.Link.RoomClosed += OnRoomClosed;
        }

        private void OnDisable()
        {
            connection.Link.RoomCreated -= OnRoomCreated;
            connection.Link.RoomClosed -= OnRoomClosed;
        }

        private void OnDestroy() => SetQr(null);

        private void OnRoomCreated(string roomCode, string joinUrl) => StartCoroutine(Download(roomCode));

        private void OnRoomClosed() => SetQr(null);

        private class BypassCertificateHandler : CertificateHandler
        {
            protected override bool ValidateCertificate(byte[] certificateData) => true;
        }

        private IEnumerator Download(string roomCode)
        {
            using UnityWebRequest request = UnityWebRequestTexture.GetTexture(ServerUrls.Qr(connection.ServerUrl, roomCode));
            request.certificateHandler = new BypassCertificateHandler();
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[Net] No se pudo descargar el QR de {roomCode}: {request.error}");
                yield break;
            }
            // Mientras bajaba pudo cerrarse la sala o crearse otra.
            if (roomCode != connection.Link.RoomCode) yield break;

            Texture2D texture = DownloadHandlerTexture.GetContent(request);
            // Sin suavizado: un QR borroso en la TV cuesta leerlo con la cámara.
            texture.filterMode = FilterMode.Point;
            SetQr(texture);
        }

        private void SetQr(Texture2D texture)
        {
            if (Qr != null) Destroy(Qr);
            Qr = texture;
            if (target != null)
            {
                target.texture = texture;
                target.enabled = texture != null;
            }
            QrChanged?.Invoke(texture);
        }
    }
}
