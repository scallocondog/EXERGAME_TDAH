// RF-04: forma JSON de docs/protocolo-ws.md, tal como llega por /unity
using System;

namespace MoviMente.Net
{
    // Una sola clase con todos los campos posibles: JsonUtility no tiene
    // polimorfismo y cada mensaje usa solo los suyos según "t".
    [Serializable]
    internal sealed class WireMessage
    {
        public string t;
        public string room;
        public string url;
        public string code;
        public string value;
        public string action;
        public int slot;
        public long seq;
        public long ts;
        public WireOrientation ori;
        public WireVector acc;
    }

    [Serializable]
    internal sealed class WireOrientation
    {
        public float alpha;
        public float beta;
        public float gamma;
    }

    [Serializable]
    internal sealed class WireVector
    {
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    internal sealed class OutgoingHaptic
    {
        public string t = "haptic";
        public int slot;
        public string pattern;
    }

    [Serializable]
    internal sealed class OutgoingState
    {
        public string t = "state";
        public int slot;
        public string value;
    }
}
