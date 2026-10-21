// RF-01: el código de la sala y el QR, para unir el mando que todavía falta
using System.Collections.Generic;
using MoviMente.Net;
using UnityEngine;
using UnityEngine.UIElements;

namespace MoviMente.UI
{
    internal sealed class RoomView
    {
        private const string NoLetter = "–";

        private readonly List<Label> keys;
        private readonly VisualElement qr;
        private GameLink link;
        private RoomQrLoader loader;

        public RoomView(VisualElement room)
        {
            keys = room.Q("codigo").Query<Label>().ToList();
            qr = room.Q("qr");
        }

        public void Bind(GameLink nextLink, RoomQrLoader nextLoader)
        {
            Unbind();
            link = nextLink;
            loader = nextLoader;
            link.RoomCreated += OnRoomCreated;
            link.RoomClosed += OnRoomClosed;
            loader.QrChanged += ShowQr;
            ShowCode(link.RoomCode);
            ShowQr(loader.Qr);
        }

        public void Unbind()
        {
            if (link != null)
            {
                link.RoomCreated -= OnRoomCreated;
                link.RoomClosed -= OnRoomClosed;
            }
            if (loader != null) loader.QrChanged -= ShowQr;
            link = null;
            loader = null;
        }

        private void OnRoomCreated(string roomCode, string joinUrl) => ShowCode(roomCode);

        private void OnRoomClosed() => ShowCode(null);

        // Una ficha por letra, como en la maqueta; sin sala, rayas.
        private void ShowCode(string roomCode)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                keys[i].text = roomCode != null && i < roomCode.Length ? roomCode[i].ToString() : NoLetter;
            }
        }

        private void ShowQr(Texture2D texture)
        {
            qr.style.backgroundImage = texture != null ? new StyleBackground(texture) : new StyleBackground(StyleKeyword.None);
        }
    }
}
