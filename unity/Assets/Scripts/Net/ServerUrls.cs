// RF-01: URLs del servidor a partir de la que se configura en ServerConnection
using System;

namespace MoviMente.Net
{
    // Acepta la URL con ws(s):// o http(s)://: es fácil pegar una u otra en el Inspector.
    public static class ServerUrls
    {
        public static string WebSocket(string serverUrl)
        {
            var uri = new Uri(serverUrl);
            string scheme = IsSecure(uri) ? "wss" : "ws";
            return $"{scheme}://{uri.Host}:{uri.Port}{uri.PathAndQuery}";
        }

        public static string HttpBase(string serverUrl)
        {
            var uri = new Uri(serverUrl);
            string scheme = IsSecure(uri) ? "https" : "http";
            return $"{scheme}://{uri.Host}:{uri.Port}";
        }

        public static string Qr(string serverUrl, string roomCode) =>
            $"{HttpBase(serverUrl)}/qr/{Uri.EscapeDataString(roomCode)}";

        private static bool IsSecure(Uri uri) => uri.Scheme == "wss" || uri.Scheme == "https";
    }
}
