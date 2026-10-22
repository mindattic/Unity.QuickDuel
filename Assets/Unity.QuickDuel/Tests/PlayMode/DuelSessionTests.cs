using System.Collections;
using NUnit.Framework;
using Unity.QuickDuel.Core;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.QuickDuel.Tests
{
    public class DuelSessionTests
    {
        private DuelController controller;
        private DuelSession session;

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
            yield return SceneManager.LoadSceneAsync("Duel", LoadSceneMode.Single);
            yield return null;
            controller = Object.FindFirstObjectByType<DuelController>();
            session = controller.GetComponent<DuelSession>();
            Assert.That(session, Is.Not.Null, "Duel scene has no DuelSession.");
            controller.Settings = DuelSceneTests.FastSettings();
        }

        [UnityTest]
        public IEnumerator TheAiStrikesAsTheRightPlayerWithinItsDifficultyRange()
        {
            session.SetMode(DuelMode.VsAiHard);
            Assert.That(controller.GetComponent<DuelInput>().RightIsHuman, Is.False);
            Assert.That(session.ModeText, Does.Contain("Hard"));

            controller.BeginRound();
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Resolved);

            var (min, max) = AiOpponent.ReactionRange(AiDifficulty.Hard);
            Assert.That(controller.Result.Winner, Is.EqualTo(Side.Right));
            Assert.That(controller.Result.RightReaction, Is.InRange(min, max));
            Assert.That(session.Match.WinsOf(Side.Right), Is.EqualTo(1));
            Assert.That(session.ScoreText(Side.Right), Does.StartWith("AI  1/2"));
        }

        [UnityTest]
        public IEnumerator AFasterHumanBeatsTheAi()
        {
            session.SetMode(DuelMode.VsAiEasy);
            controller.BeginRound();
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            controller.Strike(Side.Left, controller.Duel.CueAt + 0.150);
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Resolved);

            Assert.That(controller.Result.Winner, Is.EqualTo(Side.Left));
            Assert.That(session.Match.BestReactionOf(Side.Left), Is.EqualTo(0.150).Within(1e-9));
        }

        [UnityTest]
        public IEnumerator TwoWinsTakeTheMatchAndTheNextRoundStartsAFreshOne()
        {
            for (var round = 0; round < 2; round++)
            {
                controller.BeginRound();
                yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
                controller.Strike(Side.Left, controller.Duel.CueAt + 0.2 + round * 0.05);
                controller.Strike(Side.Right, controller.Duel.CueAt + 0.4);
            }

            Assert.That(session.Match.IsOver, Is.True);
            Assert.That(session.Match.Winner, Is.EqualTo(Side.Left));
            Assert.That(session.VerdictText, Is.EqualTo("Left takes the match 2-0"));
            Assert.That(session.ScoreText(Side.Left), Is.EqualTo("Left  2/2   best 200 ms"));
            for (var i = 0; i < 30; i++) yield return null;
            Screenshots.Capture("07-match");

            controller.BeginRound();
            Assert.That(session.Match.WinsOf(Side.Left), Is.EqualTo(0));
            Assert.That(session.VerdictText, Is.Empty);
        }

        [UnityTest]
        public IEnumerator ModeCyclesOnlyBetweenRoundsAndResetsTheMatch()
        {
            Assert.That(session.Mode, Is.EqualTo(DuelMode.LocalPvp));
            controller.BeginRound();
            Assert.That(session.CycleMode(), Is.False);
            controller.Strike(Side.Left, controller.Now);   // early strike ends the round
            Assert.That(session.Match.WinsOf(Side.Right), Is.EqualTo(1));

            Assert.That(session.CycleMode(), Is.True);
            Assert.That(session.Mode, Is.EqualTo(DuelMode.VsAiEasy));
            Assert.That(session.Match.WinsOf(Side.Right), Is.EqualTo(0));
            session.CycleMode();
            session.CycleMode();
            session.CycleMode();
            Assert.That(session.Mode, Is.EqualTo(DuelMode.LocalPvp));
            Assert.That(controller.GetComponent<DuelInput>().RightIsHuman, Is.True);
            yield return null;
        }
    }
}
