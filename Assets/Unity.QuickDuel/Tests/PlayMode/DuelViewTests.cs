using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.QuickDuel.Core;
using UnityEngine.UIElements;
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
            Assert.That(IsInvertFilter(view.Backdrop), Is.True, "the cue inverts the whole backdrop");
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

        // The scene carries the 11 original arenas, each with its art, and draws the one the round's roll lands on,
        // afresh every round (UQD-T33).
        [UnityTest]
        public IEnumerator EachRoundIsFoughtOnTheArenaItsRollLandsOn()
        {
            Assert.That(view.Arenas, Has.Count.EqualTo(11));
            Assert.That(view.Arenas.Select(a => a.Name), Is.EqualTo(new[]
            {
                "Wastes of Nod", "Pastel Fields", "Frozen Tundra", "The Fumaroles", "Dusty Junction", "Sozza's Point",
                "South Ridge", "Arid Plains", "Interior Sea", "Tu'verone Forest", "Harvest Cresent",
            }));
            Assert.That(view.Arenas.All(a => a.Sprite != null && !string.IsNullOrEmpty(a.Description)), Is.True);

            yield return null;
            Assert.That(view.Arena, Is.SameAs(view.Arenas[ArenaDraw.Pick(controller.ArenaRoll, 11)]), "an arena before the first round");
            Assert.That(view.Backdrop.style.backgroundImage.value.sprite, Is.SameAs(view.Arena.Sprite));

            controller.Settings = DuelSceneTests.FastSettings();
            var shown = new HashSet<string>();
            for (var round = 0; round < 8; round++)
            {
                Assert.That(controller.BeginRound(), Is.True);
                yield return null;
                Assert.That(view.Arena, Is.SameAs(view.Arenas[ArenaDraw.Pick(controller.ArenaRoll, 11)]));
                Assert.That(view.Backdrop.style.backgroundImage.value.sprite, Is.SameAs(view.Arena.Sprite));
                Assert.That(IsInvertFilter(view.Backdrop), Is.False);
                shown.Add(view.Arena.Id);
                yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Resolved, 3f);
            }
            Assert.That(shown.Count, Is.GreaterThan(1), "the arena is drawn again every round");
        }

        private static bool IsInvertFilter(VisualElement element)
        {
            var filter = element.style.filter;
            return filter.keyword == StyleKeyword.Undefined && filter.value != null
                && filter.value.Any(f => f.type == FilterFunctionType.Invert);
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
