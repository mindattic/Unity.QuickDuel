using System.Collections;
using NUnit.Framework;
using Unity.QuickDuel.Core;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Unity.QuickDuel.Tests
{
    public class DuelAudioTests
    {
        [UnityTest]
        public IEnumerator ARoundPlaysBeepsDrumrollUnsheathAndSlash()
        {
            yield return SceneManager.LoadSceneAsync("Duel", LoadSceneMode.Single);
            var controller = Object.FindFirstObjectByType<DuelController>();
            var audio = controller.GetComponent<DuelAudio>();
            Assert.That(audio, Is.Not.Null, "Duel scene has no DuelAudio.");
            controller.Settings = new DuelSettings
            {
                CountdownSeconds = 2.9, MinStandoffSeconds = 0.2, MaxStandoffSeconds = 0.3, StrikeTimeoutSeconds = 1
            };

            controller.BeginRound();
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Standoff);
            Assert.That(audio.DrumrollPlaying, Is.True);
            yield return DuelSceneTests.WaitUntil(() => controller.Phase == DuelPhase.Strike);
            Assert.That(audio.DrumrollPlaying, Is.False);
            controller.Strike(Side.Left, controller.Duel.CueAt + 0.2);
            controller.Strike(Side.Right, controller.Duel.CueAt + 0.25);

            Assert.That(audio.Played, Is.EqualTo(new[]
            {
                DuelAudio.Beep, DuelAudio.Beep, DuelAudio.Beep, DuelAudio.Drumroll, DuelAudio.Unsheath, DuelAudio.Slash
            }));
        }

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator TheSceneCarriesTheBushidoCodaClips()
        {
            yield return SceneManager.LoadSceneAsync("Duel", LoadSceneMode.Single);
            var sources = Object.FindFirstObjectByType<DuelAudio>();
            var so = new UnityEditor.SerializedObject(sources);
            foreach (var field in new[] { "beep", "drumroll", "unsheath", "slash" })
                Assert.That(so.FindProperty(field).objectReferenceValue, Is.Not.Null, field);
        }
#endif
    }
}
