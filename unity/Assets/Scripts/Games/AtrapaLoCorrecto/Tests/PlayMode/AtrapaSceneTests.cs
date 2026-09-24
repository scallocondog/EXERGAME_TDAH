// RF-11, RF-19: la escena real de Atrapa lo correcto corre una partida sin servidor
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace MoviMente.Games.AtrapaLoCorrecto.Tests
{
    public sealed class AtrapaSceneTests
    {
        [UnityTest]
        public IEnumerator LaEscenaArrancaLaPartidaYDibujaCanastaYObjetos()
        {
            yield return SceneManager.LoadSceneAsync("Game_AtrapaLoCorrecto");
            yield return null;

            GameSession session = CoreSystems.Instance.Runner.Session;
            Assert.That(session, Is.Not.Null);
            Assert.That(session.Definition.Id, Is.EqualTo(AtrapaLoCorrectoGame.Id));
            Assert.That(GameObject.Find("Canasta"), Is.Not.Null);
            Assert.That(GameObject.Find("Categoría pedida"), Is.Not.Null);

            // Sin servidor no hay mando: se simula que llegó y que confirmó.
            session.SetPadConnected(AtrapaLoCorrectoRules.PlayerSlot, true);
            session.BeginCountdown();
            yield return new WaitForSeconds(GameSession.CountdownSeconds + 1.5f);

            Assert.That(session.State, Is.EqualTo(GameState.Playing));
            AtrapaView view = Object.FindFirstObjectByType<AtrapaView>();
            Assert.That(view.Rules.Items.Count, Is.GreaterThan(0));
            Assert.That(GameObject.Find($"Objeto {view.Rules.Items[0].Id}"), Is.Not.Null);

            // Al perder el mando la partida se pausa y nada sigue cayendo (CU-08).
            session.SetPadConnected(AtrapaLoCorrectoRules.PlayerSlot, false);
            float y = view.Rules.Items[0].Y;
            yield return new WaitForSeconds(0.5f);
            Assert.That(session.IsWaitingForReconnection, Is.True);
            Assert.That(view.Rules.Items[0].Y, Is.EqualTo(y));
        }
    }
}
