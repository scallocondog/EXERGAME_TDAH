// RF-06: umbrales de gestos como asset, editables en el Inspector sin recompilar
using UnityEngine;

namespace MoviMente.Gestures
{
    [CreateAssetMenu(fileName = "GestureSettings", menuName = "MoviMente/Ajustes de gestos")]
    public sealed class GestureSettingsAsset : ScriptableObject
    {
        // Los reconocedores guardan esta misma instancia: un cambio en el
        // Inspector durante el Play se aplica en el acto.
        public GestureSettings Values = new GestureSettings();
    }
}
