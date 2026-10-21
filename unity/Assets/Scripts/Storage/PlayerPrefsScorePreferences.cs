// RF-17: adaptador local para guardar puntajes sin cuentas ni datos personales
using UnityEngine;

namespace MoviMente.Storage
{
    public sealed class PlayerPrefsScorePreferences : IScorePreferences
    {
        public int GetInt(string key, int defaultValue) => PlayerPrefs.GetInt(key, defaultValue);
        public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
        public void Save() => PlayerPrefs.Save();
    }
}
