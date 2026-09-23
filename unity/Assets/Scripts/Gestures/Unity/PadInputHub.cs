// RF-05, RF-06, RNF-08: une la red con un reconocedor de gestos por mando
using System;
using System.Collections.Generic;
using MoviMente.Net;
using UnityEngine;

namespace MoviMente.Gestures
{
    // Punto único del que leen menús y minijuegos: gestos, inclinación y botones.
    // Nadie más escucha el motion crudo.
    public sealed class PadInputHub : MonoBehaviour
    {
        [SerializeField] private ServerConnection connection;
        [SerializeField] private GestureSettingsAsset settings;

        private readonly Dictionary<int, GestureRecognizer> pads = new Dictionary<int, GestureRecognizer>();
        private GestureSettings fallbackSettings;

        public event Action<GestureEvent> GestureDetected;
        // confirm, recenter o pause; recenter ya llega aplicado.
        public event Action<int, string> ButtonPressed;

        public TiltAxes GetTilt(int slot) => pads.TryGetValue(slot, out GestureRecognizer pad) ? pad.Tilt : TiltAxes.Neutral;

        public bool IsCalibrated(int slot) => pads.TryGetValue(slot, out GestureRecognizer pad) && pad.IsCalibrated;

        public bool IsSteady(int slot) => pads.TryGetValue(slot, out GestureRecognizer pad) && pad.IsSteady;

        private void OnEnable()
        {
            connection.Link.MotionReceived += OnMotion;
            connection.Link.CalibrateRequested += Calibrate;
            connection.Link.ButtonPressed += OnButton;
            connection.Link.RoomClosed += pads.Clear;
        }

        private void OnDisable()
        {
            connection.Link.MotionReceived -= OnMotion;
            connection.Link.CalibrateRequested -= Calibrate;
            connection.Link.ButtonPressed -= OnButton;
            connection.Link.RoomClosed -= pads.Clear;
        }

        private void OnMotion(int slot, MotionSample sample) => Pad(slot).Process(sample);

        private void Calibrate(int slot) => Pad(slot).Calibrate();

        private void OnButton(int slot, string action)
        {
            if (action == PadButton.Recenter) Calibrate(slot);
            ButtonPressed?.Invoke(slot, action);
        }

        private GestureRecognizer Pad(int slot)
        {
            if (pads.TryGetValue(slot, out GestureRecognizer pad)) return pad;

            pad = new GestureRecognizer(slot, Settings());
            pad.GestureDetected += gesture => GestureDetected?.Invoke(gesture);
            pads.Add(slot, pad);
            return pad;
        }

        private GestureSettings Settings()
        {
            if (settings != null) return settings.Values;
            if (fallbackSettings == null)
            {
                Debug.LogWarning("[Gestures] PadInputHub sin GestureSettingsAsset: se usan los valores por defecto.");
                fallbackSettings = new GestureSettings();
            }
            return fallbackSettings;
        }
    }
}
