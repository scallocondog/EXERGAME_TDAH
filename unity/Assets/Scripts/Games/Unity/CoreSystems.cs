// RF-01, RF-07: la conexión, la sala y la partida sobreviven a los cambios de escena
using MoviMente.Gestures;
using MoviMente.Net;
using UnityEngine;

namespace MoviMente.Games
{
    // Si cada escena trajera su propia conexión, al pasar del menú al minijuego
    // se cerraría la sala y los celulares tendrían que volver a escanear.
    [DefaultExecutionOrder(-1000)]
    public sealed class CoreSystems : MonoBehaviour
    {
        [SerializeField] private ServerConnection connection;
        [SerializeField] private PadInputHub input;
        [SerializeField] private RoomQrLoader qr;
        [SerializeField] private GameRunner runner;

        public static CoreSystems Instance { get; private set; }

        public ServerConnection Connection => connection;
        public PadInputHub PadInput => input;
        public RoomQrLoader Qr => qr;
        public GameRunner Runner => runner;

        private void Awake()
        {
            // Cada escena trae una copia para poder abrirse sola en el editor;
            // si ya hay una viva, la copia se apaga antes de conectarse.
            if (Instance != null && Instance != this)
            {
                gameObject.SetActive(false);
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
