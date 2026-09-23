// RF-06: mando simulado a 60 Hz para probar el reconocedor sin celular
using System.Collections.Generic;
using System.Linq;

namespace MoviMente.Gestures.Tests
{
    internal sealed class FakePad
    {
        public const int FrameMs = 16;
        public static readonly Vec3 Gravity = new Vec3(0f, 9.81f, 0f);

        public readonly GestureSettings Settings = new GestureSettings();
        public readonly GestureRecognizer Recognizer;
        public readonly List<GestureEvent> Events = new List<GestureEvent>();

        public long Now = 1_000_000;
        public float Beta = 45f;
        public float Gamma;

        public FakePad(int slot = 1)
        {
            Recognizer = new GestureRecognizer(slot, Settings);
            Recognizer.GestureDetected += Events.Add;
        }

        public void Hold(int ms, float? beta = null, float? gamma = null)
        {
            Beta = beta ?? Beta;
            Gamma = gamma ?? Gamma;
            for (int elapsed = 0; elapsed < ms; elapsed += FrameMs) Send(Gravity);
        }

        public void Jolt(Vec3 extra, int frames = 1)
        {
            for (int i = 0; i < frames; i++) Send(Gravity + extra);
        }

        public void Send(Vec3 acceleration)
        {
            Now += FrameMs;
            Recognizer.Process(new MotionSample(Now, 0f, Beta, Gamma, acceleration));
        }

        public int Count(GestureType type) => Events.Count(e => e.Type == type);

        public GestureEvent Last(GestureType type) => Events.Last(e => e.Type == type);
    }
}
