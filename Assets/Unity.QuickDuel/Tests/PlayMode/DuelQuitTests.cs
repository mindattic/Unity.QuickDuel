using System.Collections;
using NUnit.Framework;
using Unity.QuickDuel.Core;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.QuickDuel.Tests
{
    // Escape (or gamepad Select+Start) should quit the standalone player, but only between rounds —
    // the original's obj_mainmenu Escape and Quit button both called game_end() (UQD-T23).
    public class DuelQuitTests : InputTestFixture
    {
        private DuelController controller;
        private DuelQuit quit;
        private Keyboard keyboard;
        private bool quitCalled;

        private IEnumerator Load()
        {
            yield return SceneManager.LoadSceneAsync("Duel", LoadSceneMode.Single);
            keyboard = InputSystem.AddDevice<Keyboard>();
            controller = Object.FindFirstObjectByType<DuelController>();
            quit = controller.GetComponent<DuelQuit>();
            Assert.That(quit, Is.Not.Null, "Duel scene has no DuelQuit.");
            controller.Settings = DuelSceneTests.FastSettings();
            quitCalled = false;
            quit.OnQuit = () => quitCalled = true;
        }

        [UnityTest]
        public IEnumerator EscapeQuitsBeforeARoundStarts()
        {
            yield return Load();
            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Ready));
            Press(keyboard.escapeKey);
            yield return null;

            Assert.That(quitCalled, Is.True);
        }

        [UnityTest]
        public IEnumerator EscapeQuitsAfterARoundResolves()
        {
            yield return Load();
            controller.BeginRound();
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Resolved, 3f);

            Press(keyboard.escapeKey);
            yield return null;

            Assert.That(quitCalled, Is.True);
        }

        [UnityTest]
        public IEnumerator EscapeDoesNothingMidRound()
        {
            yield return Load();
            controller.BeginRound();
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Standoff);

            Press(keyboard.escapeKey);
            yield return null;

            Assert.That(quitCalled, Is.False);
            Assert.That(controller.Phase, Is.Not.EqualTo(DuelPhase.Resolved));
        }

        [UnityTest]
        public IEnumerator GamepadSelectAndStartQuitsWithoutAlsoBeginningARoundOrCyclingTheMode()
        {
            yield return Load();
            var session = controller.GetComponent<DuelSession>();
            var pad = InputSystem.AddDevice<Gamepad>();
            Press(pad.selectButton);
            yield return null;
            Press(pad.startButton);
            yield return null;
            Release(pad.selectButton);   // DuelSession would otherwise cycle the mode on Select's release
            Release(pad.startButton);
            yield return null;

            Assert.That(quitCalled, Is.True);
            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Ready), "Select+Start quits; it shouldn't also begin a round.");
            Assert.That(session.Mode, Is.EqualTo(DuelMode.LocalPvp), "Select+Start quits; it shouldn't also cycle the mode.");
        }

        [UnityTest]
        public IEnumerator GamepadStartAloneStillBeginsARound()
        {
            yield return Load();
            var pad = InputSystem.AddDevice<Gamepad>();
            Press(pad.startButton);
            yield return null;

            Assert.That(quitCalled, Is.False);
            Assert.That(controller.Phase, Is.EqualTo(DuelPhase.Countdown));
        }

        [UnityTest]
        public IEnumerator GamepadSelectTapAloneStillCyclesTheMode()
        {
            yield return Load();
            var session = controller.GetComponent<DuelSession>();
            var pad = InputSystem.AddDevice<Gamepad>();
            Press(pad.selectButton);
            yield return null;
            Release(pad.selectButton);
            yield return null;

            Assert.That(quitCalled, Is.False);
            Assert.That(session.Mode, Is.EqualTo(DuelMode.VsAiEasy), "A plain Select tap should still cycle the mode.");
        }
    }
}
