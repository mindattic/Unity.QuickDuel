using Unity.QuickDuel.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.QuickDuel
{
    // Mocks whoever struck before the cue with one line from the old Android build of Bushido Coda.
    [RequireComponent(typeof(DuelController), typeof(DuelView))]
    public sealed class DuelTaunt : MonoBehaviour
    {
        // The Android build's taunt lines are tuning data (Settings/tuning.json, "taunts").
        public static string[] Lines => TuningStore.Profile.Taunts;

        private DuelController controller;
        private DuelView view;
        private Label text;

        public string Current => text.style.display != DisplayStyle.None ? text.text : "";

        private void Start()
        {
            controller = GetComponent<DuelController>();
            view = GetComponent<DuelView>();
            text = view.CreateLabel("Taunt", 72, new Vector2(0, 215f), new Color(0.75f, 0.08f, 0.08f));
            text.style.unityFontStyleAndWeight = FontStyle.BoldAndItalic;
            text.style.display = DisplayStyle.None;
            controller.RoundStarted += () => text.style.display = DisplayStyle.None;
            controller.Resolved += OnResolved;
        }

        private void OnResolved(DuelResult result)
        {
            if (result.Outcome != DuelOutcome.EarlyStrike || !result.Winner.HasValue)
                return;

            var loser = result.Winner.Value == Side.Left ? Side.Right : Side.Left;
            text.text = Lines[Random.Range(0, Lines.Length)];
            view.Reposition(text, loser == Side.Left ? -380f : 380f, 215f);
            text.style.display = DisplayStyle.Flex;
        }
    }
}
