using Unity.QuickDuel.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Unity.QuickDuel
{
    // Mocks whoever struck before the cue with one line from the old Android build of Bushido Coda.
    [RequireComponent(typeof(DuelController), typeof(DuelView))]
    public sealed class DuelTaunt : MonoBehaviour
    {
        public static readonly string[] Lines =
        {
            "Premature Strike", "Stumbled", "Impetuous Attack", "Clumsy Move", "Hurried Action",
            "Jumped the Gun", "Overzealous", "Too Soon", "Too Early", "Fail"
        };

        private DuelController controller;
        private Text text;

        public string Current => text.enabled ? text.text : "";

        private void Start()
        {
            controller = GetComponent<DuelController>();
            var view = GetComponent<DuelView>();
            text = view.Label("Taunt", 72, new Vector2(0, 215f), new Color(0.75f, 0.08f, 0.08f), tint: false);
            text.fontStyle = FontStyle.BoldAndItalic;
            text.enabled = false;
            controller.RoundStarted += () => text.enabled = false;
            controller.Resolved += OnResolved;
        }

        private void OnResolved(DuelResult result)
        {
            if (result.Outcome != DuelOutcome.EarlyStrike || !result.Winner.HasValue)
                return;

            var loser = result.Winner.Value == Side.Left ? Side.Right : Side.Left;
            text.text = Lines[Random.Range(0, Lines.Length)];
            text.rectTransform.anchoredPosition = new Vector2(loser == Side.Left ? -380f : 380f, 215f);
            text.enabled = true;
        }
    }
}
