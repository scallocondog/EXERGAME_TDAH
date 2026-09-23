// RF-01: del ws(s)://host:puerto/unity se deriva la base HTTP para el QR
using System;

namespace MoviMente.Net
{
    public static class ServerUrls
    {
        public static string HttpBase(string webSocketUrl)
        {
            var uri = new Uri(webSocketUrl);
            string scheme = uri.Scheme == "wss" ? "https" : "http";
            return $"{scheme}://{uri.Host}:{uri.Port}";
        }

        public static string Qr(string webSocketUrl, string roomCode) =>
            $"{HttpBase(webSocketUrl)}/qr/{Uri.EscapeDataString(roomCode)}";
    }
}
