using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.QuickDuel.Core;
using UnityEngine;
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
            Assert.That(view.GetReadyText, Is.EqualTo("Get Ready"));
            Assert.That(view.PromptText, Is.Empty);
            Screenshots.Capture("02-countdown");

            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Standoff);
            yield return DuelSceneTests.WaitUntil(() => view.LetterboxAmount > 0.9f || controller.Phase != DuelPhase.Standoff);
            Assert.That(view.CountdownText, Is.Empty);
            Assert.That(view.GetReadyText, Is.Empty);
            Assert.That(view.Inverted, Is.False);
            Screenshots.Capture("03-standoff");

            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            yield return null;
            Assert.That(view.Inverted, Is.True);
            Assert.That(IsInvertFilter(view.Backdrop), Is.True, "the cue inverts the whole backdrop");
            Assert.That(view.CueText, Is.EqualTo("!"));
            foreach (var side in new[] { Side.Left, Side.Right })
            {
                Assert.That(view.Motion(side).Pose, Is.EqualTo(FighterPose.Running), "the cue sets both running");
                Assert.That(view.FighterShows(side), Is.SameAs(view.FighterIn(side).Run));
            }
            Assert.That(view.Motion(Side.Left).XFraction, Is.EqualTo(0.4).Within(0.01));
            Assert.That(view.Motion(Side.Right).XFraction, Is.EqualTo(0.6).Within(0.01));
            var crept = view.Motion(Side.Left).X;
            yield return new WaitForSecondsRealtime(0.1f);
            var stage = view.Root.Q("Stage").worldBound;
            Assert.That(view.FighterElement(Side.Left).worldBound.center.x - stage.xMin,
                Is.EqualTo(stage.width * (float)view.Motion(Side.Left).XFraction).Within(3f), "drawn where it runs, from 40% of the width");
            if (controller.Phase == DuelPhase.Strike)
                Assert.That(view.Motion(Side.Left).X, Is.GreaterThan(crept), "the left fighter creeps toward the middle");
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
            Assert.That(view.Motion(Side.Right).Pose, Is.EqualTo(FighterPose.Attacking));
            Assert.That(view.Motion(Side.Left).Pose, Is.EqualTo(FighterPose.Defeated));
            Assert.That(view.FighterIn(Side.Right).Attack, Has.Member(view.FighterShows(Side.Right)), "the winner plays its attack strip");
            Assert.That(view.FighterIn(Side.Left).Defeat, Has.Member(view.FighterShows(Side.Left)), "the loser plays its defeat strip");
            Assert.That(view.Motion(Side.Left).XFraction, Is.EqualTo(0.5), "the loser falls at the middle");
            Assert.That(view.Motion(Side.Right).XFraction, Is.LessThan(0.5), "the winner dashes on past the middle");
            Assert.That(view.PromptText, Does.Contain("next round"));
            Screenshots.Capture("05-resolved");
        }

        // gameclock_action (UQD-T38): while the cue is up the centre counts the milliseconds since the cue off the clock,
        // where the countdown's number was, beneath the cue; the first strike freezes it, and it fades from there.
        [UnityTest]
        public IEnumerator TheStrikeCounterCountsUpFreezesAtTheFirstStrikeAndFades()
        {
            yield return null;
            var settings = DuelSceneTests.FastSettings();
            settings.StrikeTimeoutSeconds = 5;
            settings.MinStandoffSeconds = settings.MaxStandoffSeconds = 0.5;
            // Long enough that the round waits for the second strike, so the frozen count is seen while the cue is up.
            settings.SettleSeconds = 2;
            controller.Settings = settings;
            Assert.That(controller.BeginRound(), Is.True);
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Standoff);
            yield return null;
            if (controller.Phase == DuelPhase.Standoff)
                Assert.That(view.CounterText, Is.Empty, "nothing counts before the cue");

            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            yield return new WaitForSecondsRealtime(0.2f);
            yield return null;
            var cue = controller.CueAt;
            var shown = int.Parse(view.CounterText);
            var expected = (controller.Now - cue) * 1000;
            Assert.That(shown, Is.InRange(150, (int)expected + 1), "milliseconds since the cue, read off the clock");
            Assert.That(view.CounterAlpha, Is.EqualTo(1f), "drawn at default_alpha while it counts");
            var counter = view.Root.Q<OutlinedText>("Counter");
            var cueLabel = view.Root.Q<OutlinedText>("Cue");
            yield return null;
            Assert.That(cueLabel.worldBound.center.y, Is.LessThan(counter.worldBound.center.y), "the cue sits above the count");
            yield return new WaitForSecondsRealtime(0.1f);
            Assert.That(int.Parse(view.CounterText), Is.GreaterThan(shown), "it keeps counting");
            Screenshots.Capture("04b-strike-counter");

            var struck = controller.Now;
            var frozen = ReactionCounter.Count(struck - cue).ToString();
            controller.Strike(Side.Left, struck);
            yield return null;
            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Strike), "the right fighter has not struck yet");
            Assert.That(view.CounterText, Is.EqualTo(frozen), "the first strike freezes it");
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(view.CounterText, Is.EqualTo(frozen));
            Assert.That(view.CounterAlpha, Is.LessThan(1f).And.GreaterThan(0.5f), "fading 0.003 a 60 fps step");

            controller.Strike(Side.Right, controller.Now);
            yield return null;
            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Resolved));
            Assert.That(view.CounterText, Is.EqualTo(frozen), "the verdict keeps the winner's count");
            Screenshots.Capture("05b-strike-counter-frozen");

            Assert.That(controller.BeginRound(), Is.True);
            yield return null;
            Assert.That(view.CounterText, Is.Empty, "the next round starts without it");
        }

        // gameclock_countdown (UQD-T37), at the original's 4 s: Get Ready above the number throughout, nothing numbered
        // for the first second, then each of 3, 2, 1 at full alpha as it appears, faded out 50 steps (0.83 s) later.
        [UnityTest]
        public IEnumerator TheCountdownIsGetReadyThenAFading321()
        {
            yield return null;
            var settings = DuelSceneTests.FastSettings();
            settings.CountdownSeconds = 4;
            controller.Settings = settings;
            Assert.That(controller.BeginRound(), Is.True);
            yield return null;

            Assert.That(view.GetReadyText, Is.EqualTo("Get Ready"));
            Assert.That(view.CountdownText, Is.Empty, "nothing numbered for Get Ready's quarter");
            var getReady = view.Root.Q<OutlinedText>("GetReady");
            var number = view.Root.Q<OutlinedText>("Countdown");
            Assert.That(getReady.style.opacity.value, Is.EqualTo(1f), "Get Ready is drawn at default_alpha");

            foreach (var shown in new[] { "3", "2", "1" })
            {
                yield return DuelSceneTests.WaitUntil(() => view.CountdownText == shown);
                Assert.That(view.CountdownAlpha, Is.GreaterThan(0.85f), $"{shown} appears at full alpha");
                Assert.That(view.GetReadyText, Is.EqualTo("Get Ready"));
                if (shown == "3")
                {
                    yield return null;
                    yield return null;
                    Assert.That(getReady.worldBound.center.y, Is.LessThan(number.worldBound.center.y), "Get Ready sits above the number");
                    Screenshots.Capture("02b-get-ready-3");
                }
                var appeared = controller.CountdownRemaining;
                yield return DuelSceneTests.WaitUntil(() => controller.CountdownRemaining <= appeared - 0.95 || view.CountdownText != shown);
                yield return null;
                if (view.CountdownText == shown)
                    Assert.That(view.CountdownAlpha, Is.EqualTo(0f), $"{shown} has faded out at 0.02 a step");
            }

            yield return DuelSceneTests.WaitUntil(() => controller.Phase != DuelPhase.Countdown);
            yield return null;
            Assert.That(view.GetReadyText, Is.Empty);
            Assert.That(view.CountdownText, Is.Empty);
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

        // The five original fighters, cosmetic only (UQD-T34): the pair the match's fighter roll lands on stands at a
        // quarter and three quarters of the stage, facing each other in their idle sprites, fading in from nothing at
        // 0.6 alpha a second (0.01 per original 60 fps step).
        [UnityTest]
        public IEnumerator TheMatchsFightersFaceEachOtherAndFadeIn()
        {
            Assert.That(view.Fighters.Select(f => f.Name), Is.EqualTo(new[] { "Myrmidon", "Ronin", "Gypsy", "Vagabond", "Exile" }));
            Assert.That(view.Fighters.All(f => f.Idle != null && f.Run != null && f.Weapon != null && f.GroundWeapon != null
                && f.Attack.Count == 4 && f.Defeat.Count == 4 && f.Attack.All(s => s != null) && f.Defeat.All(s => s != null)), Is.True);

            yield return null;
            var roll = controller.FighterRoll;
            foreach (var side in new[] { Side.Left, Side.Right })
            {
                Assert.That(view.FighterIn(side), Is.SameAs(view.Fighters[FighterDraw.Pick(roll, side, 5)]));
                Assert.That(view.FighterElement(side).style.backgroundImage.value.sprite, Is.SameAs(view.FighterIn(side).Idle));
            }
            Assert.That(view.FighterAlpha(Side.Left), Is.LessThan(0.2f), "the fighters start faded out");
            Assert.That(view.FighterElement(Side.Left).style.scale.value.value.x, Is.EqualTo(1f), "the left fighter faces right");
            Assert.That(view.FighterElement(Side.Right).style.scale.value.value.x, Is.EqualTo(-1f), "the right fighter is mirrored to face left");

            yield return new WaitForSecondsRealtime(0.5f);
            var midway = view.FighterAlpha(Side.Right);
            Assert.That(midway, Is.InRange(0.2f, 0.95f), "half a second in, part way through the 1.67 s fade");
            Assert.That(view.FighterElement(Side.Right).style.opacity.value, Is.EqualTo(midway));

            yield return new WaitForSecondsRealtime(1.4f);
            Assert.That(view.FighterAlpha(Side.Left), Is.EqualTo(1f));
            Assert.That(view.FighterAlpha(Side.Right), Is.EqualTo(1f));

            var stage = view.Root.Q("Stage").worldBound;
            var left = view.FighterElement(Side.Left).worldBound;
            var right = view.FighterElement(Side.Right).worldBound;
            Assert.That(left.center.x - stage.xMin, Is.EqualTo(stage.width * 0.25f).Within(2f), "a quarter of the width");
            Assert.That(right.center.x - stage.xMin, Is.EqualTo(stage.width * 0.75f).Within(2f), "three quarters of the width");
            Assert.That(left.center.y - stage.yMin, Is.EqualTo(stage.height * 2f / 3f).Within(2f), "two thirds of the way down");
            Assert.That(right.center.y, Is.EqualTo(left.center.y).Within(0.5f));
            Screenshots.Capture("06-fighters");

            // A round of the same match keeps the pair on the field, fully faded in.
            controller.Settings = DuelSceneTests.FastSettings();
            Assert.That(controller.BeginRound(), Is.True);
            yield return null;
            Assert.That(controller.FighterRoll, Is.EqualTo(roll));
            Assert.That(view.FighterAlpha(Side.Left), Is.EqualTo(1f));
        }

        // obj_game: when both seats hold the same fighter, the left one is drawn in color_red and the right in
        // color_blue; otherwise neither is tinted. Each mode change starts a new match with a newly drawn pair.
        [UnityTest]
        public IEnumerator TheSameFighterTwiceIsTintedRedAndBlue()
        {
            var session = controller.GetComponent<DuelSession>();
            yield return null;

            for (var i = 0; i < 200 && FighterDraw.SameFighter(controller.FighterRoll, 5); i++)
                Assert.That(session.CycleMode(), Is.True);
            yield return null;
            Assert.That(view.FighterIn(Side.Left), Is.Not.SameAs(view.FighterIn(Side.Right)));
            Assert.That(Tint(Side.Left), Is.EqualTo(Color.white));
            Assert.That(Tint(Side.Right), Is.EqualTo(Color.white));
            Assert.That(view.FighterAlpha(Side.Left), Is.LessThan(0.2f), "a new pair fades in afresh");

            for (var i = 0; i < 200 && !FighterDraw.SameFighter(controller.FighterRoll, 5); i++)
                Assert.That(session.CycleMode(), Is.True);
            yield return null;
            Assert.That(view.FighterIn(Side.Left), Is.SameAs(view.FighterIn(Side.Right)));
            Assert.That((Color32)Tint(Side.Left), Is.EqualTo(new Color32(196, 64, 64, 255)));
            Assert.That((Color32)Tint(Side.Right), Is.EqualTo(new Color32(64, 64, 196, 255)));
            Screenshots.Capture("07-same-fighter");
        }

        // The fighters' motion through a round (UQD-T35): on the cue both run from 40% and 60% of the width, the winner
        // dashes from the middle on to the loser's quarter-width mark playing its attack strip, the loser plays its defeat
        // strip at the middle, three seconds on the winner shows its idle sprite again, and the next round puts both back.
        [UnityTest]
        public IEnumerator TheWinnerDashesThroughAndTheLoserFalls()
        {
            controller.Settings = DuelSceneTests.FastSettings();
            Assert.That(controller.BeginRound(), Is.True);
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            yield return null;
            Assert.That(view.Motion(Side.Left).Pose, Is.EqualTo(FighterPose.Running));

            controller.Strike(Side.Left, controller.CueAt + 0.15);
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Resolved);
            yield return null;
            Assert.That(controller.Result.Winner, Is.EqualTo(Side.Left));
            Assert.That(view.Motion(Side.Left).Pose, Is.EqualTo(FighterPose.Attacking));
            Assert.That(view.Motion(Side.Right).Pose, Is.EqualTo(FighterPose.Defeated));
            Assert.That(view.FighterShows(Side.Left), Is.SameAs(view.FighterIn(Side.Left).Attack[view.Motion(Side.Left).Frame]));
            Assert.That(view.FighterShows(Side.Right), Is.SameAs(view.FighterIn(Side.Right).Defeat[view.Motion(Side.Right).Frame]));
            Assert.That(view.Root.Q("Stage").IndexOf(view.FighterElement(Side.Left).parent),
                Is.GreaterThan(view.Root.Q("Stage").IndexOf(view.FighterElement(Side.Right).parent)), "the winner is drawn in front");

            yield return new WaitForSecondsRealtime(1f);
            Assert.That(view.Motion(Side.Left).XFraction, Is.EqualTo(0.75).Within(0.01), "the winner stops on the loser's mark");
            Assert.That(view.FighterShows(Side.Left), Is.SameAs(view.FighterIn(Side.Left).Attack[3]), "the strip holds its last frame");
            Assert.That(view.FighterShows(Side.Right), Is.SameAs(view.FighterIn(Side.Right).Defeat[3]));
            var stage = view.Root.Q("Stage").worldBound;
            Assert.That(view.FighterElement(Side.Left).worldBound.center.x - stage.xMin, Is.EqualTo(stage.width * 0.75f).Within(3f));
            Assert.That(view.FighterElement(Side.Right).worldBound.center.x - stage.xMin, Is.EqualTo(stage.width * 0.5f).Within(3f));
            Screenshots.Capture("08-winner-dashed");

            yield return DuelSceneTests.WaitUntil(() => view.FighterShows(Side.Left) == view.FighterIn(Side.Left).Idle, 4f);
            Assert.That(view.Motion(Side.Left).PoseSeconds, Is.GreaterThanOrEqualTo(FighterMotion.WinnerIdleAfterSeconds));
            Assert.That(view.FighterShows(Side.Right), Is.SameAs(view.FighterIn(Side.Right).Defeat[3]), "the loser stays down");

            Assert.That(controller.BeginRound(), Is.True);
            yield return null;
            foreach (var side in new[] { Side.Left, Side.Right })
            {
                Assert.That(view.Motion(side).Pose, Is.EqualTo(FighterPose.Idle));
                Assert.That(view.Motion(side).XFraction, Is.EqualTo(FighterLineup.X(side)));
                Assert.That(view.FighterShows(side), Is.SameAs(view.FighterIn(side).Idle));
            }
        }

        private Color Tint(Side side) => view.FighterElement(side).style.unityBackgroundImageTintColor.value;

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
