using System.Collections;
using NUnit.Framework;
using Unity.QuickDuel.Core;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.QuickDuel.Tests
{
    public class DuelViewTests
    {
        private DuelController controller;
        private DuelView view;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("Duel", LoadSceneMode.Single);
            controller = Object.FindFirstObjectByType<DuelController>();
            view = controller.GetComponent<DuelView>();
            Assert.That(view, Is.Not.Null, "Duel scene has no DuelView.");
        }

        [UnityTest]
        public IEnumerator ARoundPlaysThroughEveryPresentationBeat()
        {
            yield return null;
            Assert.That(view.PromptText, Does.Contain("Space"));
            Screenshots.Capture("01-ready");

            controller.Settings = new DuelSettings
            {
                CountdownSeconds = 1.5, MinStandoffSeconds = 0.4, MaxStandoffSeconds = 0.5, StrikeTimeoutSeconds = 2, SettleSeconds = TuningStore.Profile.Duel.SettleSeconds
            };
            controller.BeginRound();
            yield return DuelSceneTests.WaitUntil(() => view.CountdownText == "2");
            Assert.That(view.CountdownText, Is.EqualTo("2"));
            Assert.That(view.PromptText, Is.Empty);
            Screenshots.Capture("02-countdown");

            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Standoff);
            yield return DuelSceneTests.WaitUntil(() => view.LetterboxAmount > 0.9f || controller.Phase != DuelPhase.Standoff);
            Assert.That(view.CountdownText, Is.Empty);
            Assert.That(view.Inverted, Is.False);
            Screenshots.Capture("03-standoff");

            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            yield return null;
            Assert.That(view.Inverted, Is.True);
            Assert.That(view.CueText, Is.EqualTo("!"));
            Screenshots.Capture("04-cue");

            var cue = controller.CueAt;
            controller.Strike(Side.Left, cue + 0.312);
            controller.Strike(Side.Right, cue + 0.187);
            yield return DuelSceneTests.WaitUntil(() => view.LetterboxAmount < 0.01f);
            for (var i = 0; i < 20; i++) yield return null;

            Assert.That(view.Inverted, Is.False);
            Assert.That(view.HeadlineText, Is.EqualTo("Right wins"));
            Assert.That(view.ReactionText(Side.Left), Is.EqualTo("312 ms"));
            Assert.That(view.ReactionText(Side.Right), Is.EqualTo("187 ms"));
            Assert.That(view.HasFallen(Side.Left), Is.True);
            Assert.That(view.HasFallen(Side.Right), Is.False);
            Assert.That(view.PromptText, Does.Contain("next round"));
            Screenshots.Capture("05-resolved");
        }

        [Test]
        public void ReactionsFormatAsMilliseconds()
        {
            Assert.That(DuelView.FormatReaction(0.2004), Is.EqualTo("200 ms"));
            Assert.That(DuelView.FormatReaction(-0.1), Is.EqualTo("too early"));
            Assert.That(DuelView.FormatReaction(null), Is.EqualTo("no strike"));
        }
    }
}
