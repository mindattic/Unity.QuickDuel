using System.Collections;
using NUnit.Framework;
using Unity.QuickDuel.Core;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.QuickDuel.Tests
{
    public class DuelTauntTests
    {
        private DuelController controller;
        private DuelTaunt taunt;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("Duel", LoadSceneMode.Single);
            yield return null;
            controller = Object.FindFirstObjectByType<DuelController>();
            taunt = controller.GetComponent<DuelTaunt>();
            Assert.That(taunt, Is.Not.Null, "Duel scene has no DuelTaunt.");
            controller.Settings = DuelSceneTests.FastSettings();
        }

        [UnityTest]
        public IEnumerator AnEarlyStrikeIsTauntedAndTheTauntClearsNextRound()
        {
            controller.BeginRound();
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Standoff);
            controller.Strike(Side.Right, controller.Now);

            Assert.That(DuelTaunt.Lines, Does.Contain(taunt.Current));
            for (var i = 0; i < 30; i++) yield return null;
            Screenshots.Capture("06-taunt");

            controller.BeginRound();
            Assert.That(taunt.Current, Is.Empty);
        }

        [UnityTest]
        public IEnumerator AFairWinIsNotTaunted()
        {
            controller.BeginRound();
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            controller.Strike(Side.Left, controller.CueAt + 0.2);
            controller.Strike(Side.Right, controller.CueAt + 0.3);

            Assert.That(taunt.Current, Is.Empty);
        }
    }
}
