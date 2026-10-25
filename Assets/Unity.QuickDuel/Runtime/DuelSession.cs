using Unity.QuickDuel.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

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
        private OutlinedText modeText;
        private OutlinedText leftScore;
        private OutlinedText rightScore;
        private OutlinedText verdict;

        private MatchSession Session => controller.Session;

        public DuelMode Mode => Session.Mode;

        // Read-only: nothing outside MatchSession may Record a round (UQD-T30). Online it is this peer's copy
        // of the host's match (UQD-T21).
        public IMatchView Match => controller.Match;

        // Online the right seat is always the other player, never the AI, whatever local mode is set.
        public bool AiPlaysRight => !controller.IsOnline && Session.AiPlaysRight;
        public double? AiStrikeAt => Session.AiStrikeAt;

        public string ModeText => modeText.text;
        public string ScoreText(Side side) => (side == Side.Left ? leftScore : rightScore).text;
        public string VerdictText => verdict.style.display != DisplayStyle.None ? verdict.text : "";

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
                if (UiFocus.IsTyping)
                    return;
                CycleMode();
            };

            controller.RoundStarted += OnRoundStarted;
            controller.Resolved += OnResolved;
            controller.OnlineChanged += Redraw;
        }

        private void Start()
        {
            view = GetComponent<DuelView>();
            // The HUD in the original's basic face; the match verdict as a banner (UQD-T36).
            modeText = view.CreateLabel("Mode", OriginalType.Hud, new Vector2(0, 490f));
            leftScore = view.CreateEdgeLabel("LeftScore", OriginalType.Hud, 0f, 490f);
            rightScore = view.CreateEdgeLabel("RightScore", OriginalType.Hud, 1f, 490f);
            verdict = view.CreateLabel("Verdict", OriginalType.Banner, new Vector2(0, -385f), new Color(0.62f, 0.12f, 0.10f));
            verdict.style.display = DisplayStyle.None;

            Session.ModeChanged += OnModeChanged;
            Redraw();
        }

        private void OnEnable() => cycleMode.Enable();
        private void OnDisable() => cycleMode.Disable();
        private void OnDestroy() => cycleMode.Dispose();

        // Between rounds only: PvP -> AI Easy -> Normal -> Hard -> PvP. Issued on the local machine's behalf.
        // Refused online: the mode is the local game's, and an online duel is always player against player.
        public bool CycleMode() => !controller.IsOnline && Session.CycleMode(MatchSession.LeftPlayerId);

        // False (and no-op) while a round is running, between rounds only -- the caller can tell it apart
        // from a mode that was actually applied, unlike the old unconditional void SetMode. Refused online.
        public bool SetMode(DuelMode mode) => !controller.IsOnline && Session.SetMode(MatchSession.LeftPlayerId, mode);

        // Session already applied the mode (and reset the match, and wired up the AI) by the time this
        // fires; this only has to redraw the labels from its new state.
        private void OnModeChanged(DuelMode mode) => Redraw();

        // Redraws the labels from the session's state, or online from the online duel's (UQD-T21).
        private void Redraw()
        {
            if (input != null) input.RightIsHuman = !AiPlaysRight;
            if (view != null) view.RightName = AiPlaysRight ? "AI" : "Right";
            if (modeText == null) return;
            modeText.text = controller.IsOnline ? OnlineModeText()
                : (Session.AiPlaysRight ? $"vs AI ({MatchSession.DifficultyOf(Session.Mode)})" : "Local PvP")
                  + "  -  Tab / Select: mode";
            verdict.style.display = DisplayStyle.None;
            RefreshScore();
        }

        private string OnlineModeText()
        {
            var online = controller.Online;
            var role = online.IsHost ? "hosting" : "joined";
            return online.Seated ? $"Online PvP ({role})" : $"Online PvP ({role}) - waiting for a player";
        }

        private void OnRoundStarted()
        {
            if (verdict != null) verdict.style.display = DisplayStyle.None;
            RefreshScore();
        }

        private void OnResolved(DuelResult result)
        {
            RefreshScore();
            var match = Match;
            if (match.IsOver && verdict != null)
            {
                var winner = match.Winner == Side.Left ? "Left" : AiPlaysRight ? "The AI" : "Right";
                verdict.text = $"{winner} takes the match {match.WinsOf(match.Winner.Value)}-{match.WinsOf(Other(match.Winner.Value))}";
                verdict.style.display = DisplayStyle.Flex;
            }
        }

        private void RefreshScore()
        {
            if (leftScore == null) return;
            leftScore.text = Score(SeatName("Left", Side.Left), Side.Left);
            rightScore.text = Score(SeatName(AiPlaysRight ? "AI" : "Right", Side.Right), Side.Right);
        }

        // Online, the seat this machine plays is marked as the player's own.
        private string SeatName(string name, Side side) => controller.LocalSide == side ? name + " (you)" : name;

        private string Score(string name, Side side)
        {
            var match = Match;
            var best = match.BestReactionOf(side);
            return $"{name}  {match.WinsOf(side)}/{match.WinsNeeded}   best {(best.HasValue ? DuelView.FormatReaction(best) : "-")}";
        }

        private static Side Other(Side side) => side == Side.Left ? Side.Right : Side.Left;
    }
}
