// RF-13, RF-19: la escena real de Corta sin fallar corre una partida sin servidor
using System.Collections;
using System.Linq;
using MoviMente.Gestures;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MoviMente.Games.CortaSinFallar.Tests
{
    public sealed class CortaSceneTests
    {
        [UnityTest]
        public IEnumerator LaEscenaLanzaFrutasYElSwingLasCorta()
        {
            yield return SceneManager.LoadSceneAsync("Game_CortaSinFallar");
            yield return null;

            GameSession session = CoreSystems.Instance.Runner.Session;
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Definition.Id, Is.EqualTo(CortaSinFallarGame.Id));
            Assert.That(GameObject.Find("Zona de corte"), Is.Not.Null);

            // Sin servidor no hay mando: se simula que llegó y que confirmó.
            session.SetPadConnected(CortaSinFallarRules.PlayerSlot, true);
            session.BeginCountdown();
            CortaView view = Object.FindFirstObjectByType<CortaView>();
            FlyingObject fruit = null;
            for (float waited = 0f; waited < 8f && fruit == null; waited += Time.deltaTime)
            {
                yield return null;
                fruit = view.Rules?.Objects.FirstOrDefault(o => o.Kind == FlyingKind.Valid
                    && CortaSinFallarRules.IsCuttable(o, session.ElapsedSeconds));
            }

            Assert.That(fruit, Is.Not.Null, "ninguna fruta llegó a la zona de corte");
            Assert.That(GameObject.Find($"Fruta {fruit.Id}"), Is.Not.Null);

            int hits = 0;
            session.TrialReported += trial => hits += trial.Outcome == TrialOutcome.Hit ? 1 : 0;
            session.HandleGesture(new GestureEvent(CortaSinFallarRules.PlayerSlot, GestureType.Swing, 1f, 0));
            yield return null;

            Assert.That(hits, Is.EqualTo(1));
            Assert.That(GameObject.Find($"Fruta {fruit.Id}"), Is.Null);
        }
    }
}
