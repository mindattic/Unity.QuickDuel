using System.Collections;
using NUnit.Framework;
using Unity.QuickDuel.Core;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.QuickDuel.Tests
{
    // Drives the real scene through fake Input System devices; strikes are queued with explicit event
    // timestamps, so the asserted reactions come from those times, not from when a frame ran.
    public class DuelInputTests : InputTestFixture
    {
        private DuelController controller;
        private Keyboard keyboard;

        private IEnumerator Load()
        {
            // Devices go in after the load; the scene's actions pick them up.
            yield return SceneManager.LoadSceneAsync("Duel", LoadSceneMode.Single);
            keyboard = InputSystem.AddDevice<Keyboard>();
            controller = Object.FindFirstObjectByType<DuelController>();
            Assert.That(controller.GetComponent<DuelInput>(), Is.Not.Null, "Duel scene has no DuelInput.");
            controller.Settings = DuelSceneTests.FastSettings();
        }

        [UnityTest]
        public IEnumerator SpaceBeginsARoundAndKeysStrikeByEventTime()
        {
            yield return Load();
            Press(keyboard.spaceKey);
            Release(keyboard.spaceKey);
            yield return null;
            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Countdown));

            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            var cue = controller.CueAt;
            // One keyboard delivers its events in time order (the Input System drops older ones).
            Press(keyboard.aKey, cue + 0.120);
            Press(keyboard.lKey, cue + 0.200);
            yield return null;

            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Resolved));
            Assert.That(controller.Result.Winner, Is.EqualTo(Side.Left));
            Assert.That(controller.Result.LeftReaction, Is.EqualTo(0.120).Within(1e-6));
            Assert.That(controller.Result.RightReaction, Is.EqualTo(0.200).Within(1e-6));
        }

        // UQD-T42: Space over the aftermath does nothing; once the loading fade has reset the battle it begins the next round.
        [UnityTest]
        public IEnumerator SpaceWaitsForTheLoadingFadeToResetTheBattle()
        {
            yield return Load();
            controller.BeginRound();
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            var cue = controller.CueAt;
            controller.Strike(Side.Left, cue + 0.1);
            controller.Strike(Side.Right, cue + 0.2);
            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Resolved));

            Press(keyboard.spaceKey);
            Release(keyboard.spaceKey);
            yield return null;
            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Resolved), "not over the aftermath");

            var reset = controller.EndedAt.Value + (RoundFade.ResetTick + 1.0) / RoundFade.TicksPerSecond;
            controller.Clock = () => reset;
            yield return null;
            Assert.That(controller.GetComponent<DuelFade>().HasReset, Is.True);
            Press(keyboard.spaceKey);
            Release(keyboard.spaceKey);
            yield return null;
            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Countdown));
        }

        [UnityTest]
        public IEnumerator SecondGamepadStrikesForTheRight()
        {
            yield return Load();
            var pad1 = InputSystem.AddDevice<Gamepad>();
            var pad2 = InputSystem.AddDevice<Gamepad>();
            Press(pad1.startButton);
            Release(pad1.startButton);
            yield return null;

            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            var cue = controller.CueAt;
            Press(pad2.buttonSouth, cue + 0.150);
            Press(pad1.buttonSouth, cue + 0.300);
            yield return null;

            Assert.That(controller.Result.Winner, Is.EqualTo(Side.Right));
            Assert.That(controller.Result.RightReaction, Is.EqualTo(0.150).Within(1e-6));
        }

        [UnityTest]
        public IEnumerator MouseClickStrikesForTheLeftByEventTime()
        {
            yield return Load();
            var mouse = InputSystem.AddDevice<Mouse>();
            Press(keyboard.spaceKey);
            Release(keyboard.spaceKey);
            yield return null;
            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Countdown));

            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            var cue = controller.CueAt;
            Press(mouse.leftButton, cue + 0.180);
            yield return null;

            // Nobody struck for the right, so the round only resolves once it settles.
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Resolved);
            Assert.That(controller.Result.Winner, Is.EqualTo(Side.Left));
            Assert.That(controller.Result.LeftReaction, Is.EqualTo(0.180).Within(1e-6));
        }

        [UnityTest]
        public IEnumerator AKeyDuringTheStandoffIsAnEarlyStrike()
        {
            yield return Load();
            controller.BeginRound();
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Standoff);
            Press(keyboard.lKey, controller.CueAt - 0.05);
            yield return null;

            Assert.That(controller.Result.Outcome, Is.EqualTo(DuelOutcome.EarlyStrike));
            Assert.That(controller.Result.Winner, Is.EqualTo(Side.Left));
            Assert.That(controller.Result.RightReaction, Is.EqualTo(-0.05).Within(1e-6));
        }
    }
}
