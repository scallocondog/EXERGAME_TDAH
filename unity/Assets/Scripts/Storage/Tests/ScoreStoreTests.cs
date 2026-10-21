// RF-17, CU-09: récords y dificultades por juego en almacenamiento local
using System;
using System.Collections.Generic;
using MoviMente.Games;
using NUnit.Framework;
using UnityEngine;

namespace MoviMente.Storage.Tests
{
    public sealed class ScoreStoreTests
    {
        private sealed class MemoryPreferences : IScorePreferences
        {
            private readonly Dictionary<string, int> values = new Dictionary<string, int>();
            public int SaveCount { get; private set; }
            public int GetInt(string key, int defaultValue) => values.TryGetValue(key, out int value) ? value : defaultValue;
            public void SetInt(string key, int value) => values[key] = value;
            public void Save() => SaveCount++;
        }

        [Test]
        public void SoloSuperarElMejorPuntajeCreaRecord()
        {
            var memory = new MemoryPreferences();
            var store = new ScoreStore(memory);

            Assert.That(store.SaveCompleted("corta", Difficulty.Easy, 20, 1), Is.True);
            Assert.That(store.SaveCompleted("corta", Difficulty.Easy, 20, 1), Is.False);
            Assert.That(store.SaveCompleted("corta", Difficulty.Easy, 10, 1), Is.False);
            Assert.That(store.BestScore("corta", Difficulty.Easy), Is.EqualTo(20));
            Assert.That(memory.SaveCount, Is.EqualTo(1));
        }

        [Test]
        public void RecordYDesbloqueoSonIndependientesPorJuegoYDificultad()
        {
            var store = new ScoreStore(new MemoryPreferences());
            store.SaveCompleted("corta", Difficulty.Easy, 30, 2);
            Assert.That(store.HighestUnlocked("corta"), Is.EqualTo(Difficulty.Medium));
            Assert.That(store.HighestUnlocked("atrapa"), Is.EqualTo(Difficulty.Easy));
            Assert.That(store.BestScore("corta", Difficulty.Medium), Is.Zero);

            store.SaveCompleted("corta", Difficulty.Medium, 40, 2);
            Assert.That(store.HighestUnlocked("corta"), Is.EqualTo(Difficulty.Hard));
            Assert.That(store.BestScore("corta", Difficulty.Medium), Is.EqualTo(40));
        }

        [Test]
        public void UnaEstrellaNoDesbloqueaElSiguienteNivel()
        {
            var store = new ScoreStore(new MemoryPreferences());
            store.SaveCompleted("corta", Difficulty.Easy, 0, 1);
            Assert.That(store.HighestUnlocked("corta"), Is.EqualTo(Difficulty.Easy));
        }

        [Test]
        public void DesbloqueaConDosEstrellasAunqueNoSupereElRecord()
        {
            var store = new ScoreStore(new MemoryPreferences());
            store.SaveCompleted("corta", Difficulty.Easy, 100, 1);

            Assert.That(store.SaveCompleted("corta", Difficulty.Easy, 80, 2), Is.False);
            Assert.That(store.BestScore("corta", Difficulty.Easy), Is.EqualTo(100));
            Assert.That(store.HighestUnlocked("corta"), Is.EqualTo(Difficulty.Medium));
        }

        [Test]
        public void PlayerPrefsConservaElRecordEntreInstancias()
        {
            string gameId = "test-" + Guid.NewGuid().ToString("N");
            string prefix = "movimente.v1." + gameId + ".";
            try
            {
                var first = new ScoreStore(new PlayerPrefsScorePreferences());
                first.SaveCompleted(gameId, Difficulty.Easy, 70, 2);

                var second = new ScoreStore(new PlayerPrefsScorePreferences());
                Assert.That(second.BestScore(gameId, Difficulty.Easy), Is.EqualTo(70));
                Assert.That(second.HighestUnlocked(gameId), Is.EqualTo(Difficulty.Medium));
            }
            finally
            {
                PlayerPrefs.DeleteKey(prefix + "best.Easy");
                PlayerPrefs.DeleteKey(prefix + "unlocked");
                PlayerPrefs.Save();
            }
        }
    }
}
