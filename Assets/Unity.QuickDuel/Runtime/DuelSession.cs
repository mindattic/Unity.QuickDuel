using Unity.QuickDuel.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Unity.QuickDuel
{
    // The on-screen session labels: who plays the right side (a second human or the AI), the match score
    // and the verdict banner. The mode, the match and the AI's own wiring belong to Core's MatchSession
    // (DuelController.Session, UQD-T30) -- this only reads its state, plays its events, and turns Tab /
    // gamepad Select into its SetMode command on the local machine's behalf. Mode cycling between rounds
    // still starts a fresh match; that reset happens in MatchSession, not here.
    [RequireComponent(typeof(DuelController), typeof(DuelView))]
    public sealed class DuelSession : MonoBehaviour
    {
        private DuelController controller;
        private DuelInput input;
        private DuelQuit quit;
        private DuelView view;
        private InputAction cycleMode;
        private Text modeText;
        private Text leftScore;
        private Text rightScore;
        private Text verdict;

        private MatchSession Session => controller.Session;

        public DuelMode Mode => Session.Mode;
        public Match Match => Session.Match;
        public bool AiPlaysRight => Session.AiPlaysRight;
        public double? AiStrikeAt => Session.AiStrikeAt;

        public string ModeText => modeText.text;
        public string ScoreText(Side side) => (side == Side.Left ? leftScore : rightScore).text;
        public string VerdictText => verdict.enabled ? verdict.text : "";

        private void Awake()
        {
            controller = GetComponent<DuelController>();
            input = GetComponent<DuelInput>();
            quit = GetComponent<DuelQuit>();

            cycleMode = new InputAction("CycleMode", InputActionType.Button);
            cycleMode.AddBinding("<Keyboard>/tab");
            cycleMode.AddBinding("<Gamepad>/select");
            // Fires on release, not press, so a Select held into the Select+Start quit chord (DuelQuit)
            // can be told apart from a plain tap and skip cycling the mode on its way out.
            cycleMode.canceled += _ =>
            {
                if (quit != null && quit.ConsumeChord())
                    return;
                CycleMode();
            };

            controller.RoundStarted += OnRoundStarted;
            controller.Resolved += OnResolved;
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

            Session.ModeChanged += OnModeChanged;
            OnModeChanged(Session.Mode);
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

        // Between rounds only: PvP -> AI Easy -> Normal -> Hard -> PvP. Issued on the local machine's behalf.
        public bool CycleMode() => Session.CycleMode(MatchSession.LeftPlayerId);

        public void SetMode(DuelMode mode) => Session.SetMode(MatchSession.LeftPlayerId, mode);

        // Session already applied the mode (and reset the match, and wired up the AI) by the time this
        // fires; this only has to redraw the labels from its new state.
        private void OnModeChanged(DuelMode mode)
        {
            if (input != null) input.RightIsHuman = !Session.AiPlaysRight;
            if (view != null) view.RightName = Session.AiPlaysRight ? "AI" : "Right";
            if (modeText == null) return;
            modeText.text = (Session.AiPlaysRight ? $"vs AI ({MatchSession.DifficultyOf(mode)})" : "Local PvP")
                + "  -  Tab / Select: mode";
            verdict.enabled = false;
            RefreshScore();
        }

        private void OnRoundStarted()
        {
            if (verdict != null) verdict.enabled = false;
            RefreshScore();
        }

        private void OnResolved(DuelResult result)
        {
            RefreshScore();
            var match = Session.Match;
            if (match.IsOver && verdict != null)
            {
                var winner = match.Winner == Side.Left ? "Left" : Session.AiPlaysRight ? "The AI" : "Right";
                verdict.text = $"{winner} takes the match {match.WinsOf(match.Winner.Value)}-{match.WinsOf(Other(match.Winner.Value))}";
                verdict.enabled = true;
            }
        }

        private void RefreshScore()
        {
            if (leftScore == null) return;
            leftScore.text = Score("Left", Side.Left);
            rightScore.text = Score(Session.AiPlaysRight ? "AI" : "Right", Side.Right);
        }

        private string Score(string name, Side side)
        {
            var match = Session.Match;
            var best = match.BestReactionOf(side);
            return $"{name}  {match.WinsOf(side)}/{match.WinsNeeded}   best {(best.HasValue ? DuelView.FormatReaction(best) : "-")}";
        }

        private static Side Other(Side side) => side == Side.Left ? Side.Right : Side.Left;
    }
}
