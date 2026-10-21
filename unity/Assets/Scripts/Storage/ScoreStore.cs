// RF-17: mejores puntajes y dificultades desbloqueadas, solo en la PC local
using System;
using MoviMente.Games;

namespace MoviMente.Storage
{
    public interface IScorePreferences
    {
        int GetInt(string key, int defaultValue);
        void SetInt(string key, int value);
        void Save();
    }

    public sealed class ScoreStore
    {
        private readonly IScorePreferences preferences;

        public ScoreStore(IScorePreferences preferences) =>
            this.preferences = preferences ?? throw new ArgumentNullException(nameof(preferences));

        public int BestScore(string gameId, Difficulty difficulty) =>
            Math.Max(0, preferences.GetInt(Key(gameId, "best." + difficulty), 0));

        public Difficulty HighestUnlocked(string gameId)
        {
            int stored = preferences.GetInt(Key(gameId, "unlocked"), (int)Difficulty.Easy);
            return (Difficulty)Math.Max((int)Difficulty.Easy, Math.Min((int)Difficulty.Hard, stored));
        }

        // Devuelve true solo cuando supera el récord previo de esa dificultad.
        public bool SaveCompleted(string gameId, Difficulty difficulty, int score, int stars)
        {
            if (score < 0) throw new ArgumentOutOfRangeException(nameof(score));
            if (stars < 1 || stars > 3) throw new ArgumentOutOfRangeException(nameof(stars));

            string bestKey = Key(gameId, "best." + difficulty);
            int previous = preferences.GetInt(bestKey, -1);
            bool newRecord = score > previous;
            if (newRecord) preferences.SetInt(bestKey, score);

            // Dos estrellas en fácil abren medio; dos en medio abren difícil.
            Difficulty unlocked = HighestUnlocked(gameId);
            if (stars >= 2 && difficulty < Difficulty.Hard && difficulty >= unlocked)
                preferences.SetInt(Key(gameId, "unlocked"), (int)difficulty + 1);

            if (newRecord || (stars >= 2 && difficulty < Difficulty.Hard && difficulty >= unlocked))
                preferences.Save();
            return newRecord;
        }

        private static string Key(string gameId, string field)
        {
            if (string.IsNullOrWhiteSpace(gameId)) throw new ArgumentException("Falta el ID del juego.", nameof(gameId));
            return "movimente.v1." + gameId + "." + field;
        }
    }
}
