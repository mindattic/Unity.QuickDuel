using Unity.QuickDuel.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Unity.QuickDuel
{
    public enum DuelMode { LocalPvp, VsAiEasy, VsAiNormal, VsAiHard }

    // The session around the rounds: who plays the right side (a second human or the AI) and the
    // match score. Tab or gamepad Select cycles the mode between rounds, which starts a fresh match.
    // Runs before DuelController so the AI's strike is registered before the round's Tick can settle.
    [DefaultExecutionOrder(-100)]
    [RequireComponent(typeof(DuelController), typeof(DuelView))]
    public sealed class DuelSession : MonoBehaviour
    {
        [SerializeField] private int winsNeeded = 2;   // best of 3

        private DuelController controller;
        private DuelInput input;
        private DuelView view;
        private AiOpponent ai;
        private double? aiStrikeAt;
        private InputAction cycleMode;
        private Text modeText;
        private Text leftScore;
        private Text rightScore;
        private Text verdict;

        public DuelMode Mode { get; private set; } = DuelMode.LocalPvp;
        public Match Match { get; private set; }
        public bool AiPlaysRight => Mode != DuelMode.LocalPvp;
        public double? AiStrikeAt => aiStrikeAt;

        public string ModeText => modeText.text;
        public string ScoreText(Side side) => (side == Side.Left ? leftScore : rightScore).text;
        public string VerdictText => verdict.enabled ? verdict.text : "";

        private void Awake()
        {
            controller = GetComponent<DuelController>();
            input = GetComponent<DuelInput>();
            Match = new Match(winsNeeded);

            cycleMode = new InputAction("CycleMode", InputActionType.Button);
            cycleMode.AddBinding("<Keyboard>/tab");
            cycleMode.AddBinding("<Gamepad>/select");
            cycleMode.performed += _ => CycleMode();

            controller.PhaseChanged += OnPhaseChanged;
            controller.Resolved += OnResolved;
            controller.RoundStarted += OnRoundStarted;
        }

        private void Start()
        {
            view = GetComponent<DuelView>();
            var ink = new Color(0.10f, 0.08f, 0.08f);
            modeText = view.Label("Mode", 40, new Vector2(0, 490f), ink);
            leftScore = EdgeLabel(view, "LeftScore", 0f, ink);
            rightScore = EdgeLabel(view, "RightScore", 1f, ink);
            verdict = view.Label("Verdict", 50, new Vector2(0, -385f), new Color(0.62f, 0.12f, 0.10f), tint: false);
            verdict.enabled = false;
            SetMode(Mode);
        }

        // Pinned to the left (x = 0) or right (x = 1) screen edge so it stays visible at any aspect.
        private static Text EdgeLabel(DuelView view, string name, float edge, Color color)
        {
            var text = view.Label(name, 40, Vector2.zero, color);
            text.alignment = edge < 0.5f ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
            var rect = text.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(edge, 0.5f);
            rect.pivot = new Vector2(edge, 0.5f);
            rect.sizeDelta = new Vector2(600, 48);
            rect.anchoredPosition = new Vector2(edge < 0.5f ? 40f : -40f, 490f);
            return text;
        }

        private void OnEnable() => cycleMode.Enable();
        private void OnDisable() => cycleMode.Disable();
        private void OnDestroy() => cycleMode.Dispose();

        private void Update()
        {
            if (!aiStrikeAt.HasValue || controller.Duel == null)
                return;
            var phase = controller.Phase;
            if ((phase == DuelPhase.Standoff || phase == DuelPhase.Strike) && controller.Now >= aiStrikeAt.Value)
            {
                // Strike at the drawn time itself, not at this frame's time, so frame rate can't help or hurt the AI.
                var at = aiStrikeAt.Value;
                aiStrikeAt = null;
                controller.Strike(Side.Right, at);
            }
        }

        // Between rounds only: PvP -> AI Easy -> Normal -> Hard -> PvP.
        public bool CycleMode()
        {
            if (!controller.CanBeginRound)
                return false;
            SetMode((DuelMode)(((int)Mode + 1) % 4));
            return true;
        }

        public void SetMode(DuelMode mode)
        {
            Mode = mode;
            ai = AiPlaysRight ? new AiOpponent(Difficulty(mode)) : null;
            if (input != null) input.RightIsHuman = !AiPlaysRight;
            Match = new Match(winsNeeded);
            if (modeText == null) return;
            modeText.text = (AiPlaysRight ? $"vs AI ({Difficulty(mode)})" : "Local PvP") + "  -  Tab / Select: mode";
            verdict.enabled = false;
            RefreshScore();
        }

        private static AiDifficulty Difficulty(DuelMode mode) =>
            mode == DuelMode.VsAiEasy ? AiDifficulty.Easy : mode == DuelMode.VsAiHard ? AiDifficulty.Hard : AiDifficulty.Normal;

        private void OnRoundStarted()
        {
            if (Match.IsOver)
                Match = new Match(winsNeeded);
            aiStrikeAt = null;
            if (verdict != null) verdict.enabled = false;
            RefreshScore();
        }

        private void OnPhaseChanged(DuelPhase phase)
        {
            if (phase == DuelPhase.Countdown && ai != null)
                aiStrikeAt = ai.StrikeTime(controller.Duel);
        }

        private void OnResolved(DuelResult result)
        {
            aiStrikeAt = null;
            Match.Record(result);
            RefreshScore();
            if (Match.IsOver && verdict != null)
            {
                var winner = Match.Winner == Side.Left ? "Left" : AiPlaysRight ? "The AI" : "Right";
                verdict.text = $"{winner} takes the match {Match.WinsOf(Match.Winner.Value)}-{Match.WinsOf(Other(Match.Winner.Value))}";
                verdict.enabled = true;
            }
        }

        private void RefreshScore()
        {
            if (leftScore == null) return;
            leftScore.text = Score("Left", Side.Left);
            rightScore.text = Score(AiPlaysRight ? "AI" : "Right", Side.Right);
        }

        private string Score(string name, Side side)
        {
            var best = Match.BestReactionOf(side);
            return $"{name}  {Match.WinsOf(side)}/{Match.WinsNeeded}   best {(best.HasValue ? DuelView.FormatReaction(best) : "-")}";
        }

        private static Side Other(Side side) => side == Side.Left ? Side.Right : Side.Left;
    }
}
