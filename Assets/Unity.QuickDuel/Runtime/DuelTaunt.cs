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
        private OutlinedText text;

        public string Current => text.style.display != DisplayStyle.None ? text.text : "";

        private void Start()
        {
            controller = GetComponent<DuelController>();
            view = GetComponent<DuelView>();
            // A banner like the original's own "Premature Strike!" (UQD-T36).
            text = view.CreateLabel("Taunt", OriginalType.Banner, new Vector2(0, 215f), new Color(0.75f, 0.08f, 0.08f));
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
