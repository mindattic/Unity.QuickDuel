using System;

namespace Unity.QuickDuel.Core
{
    public enum DuelMode { LocalPvp, VsAiEasy, VsAiNormal, VsAiHard }

    // The one authority over a local duel's round, mode and match: Strike, BeginRound and SetMode are the
    // only way to change anything here, each named by the seat issuing it, and every change is announced
    // only as an event, so a Runtime view can present it without ever mutating session or match state
    // itself. PlayerId 0 is the left seat, 1 the right -- the seam Unity.Multiplayer's NetEntities will
    // plug a real player id through once the Duel scene goes online (AGENTS.md rule 9); locally,
    // DuelController issues every command on the one machine's behalf.
    public sealed class MatchSession
    {
        public const int LeftPlayerId = 0;
        public const int RightPlayerId = 1;

        // A provider, not a snapshot: read fresh on every mode change (as DuelSession always used to), so a
        // tuning profile swapped after construction still reaches the AI on the next SetMode/CycleMode.
        private readonly Func<AiTuning> aiTuning;
        private readonly int winsNeeded;
        private readonly Random random;
        private AiOpponent ai;
        private double? aiStrikeAt;

        public MatchSession(Func<AiTuning> aiTuning, int winsNeeded, DuelMode mode = DuelMode.LocalPvp, Random random = null)
        {
            this.aiTuning = aiTuning ?? throw new ArgumentNullException(nameof(aiTuning));
            this.winsNeeded = winsNeeded;
            this.random = random ?? new Random();
            ArenaRoll = ArenaDraw.Roll(this.random);
            ApplyMode(mode);
        }

        public DuelMode Mode { get; private set; }

        // The arena roll (ArenaDraw) of the round in progress or last played; drawn afresh by every BeginRound,
        // and once on construction so the screen before the first round has an arena too.
        public int ArenaRoll { get; private set; }

        // The round in progress (or last played); null before the first BeginRound.
        public Duel Duel { get; private set; }
        public Match Match { get; private set; }

        // The current (or last) round's number, from 1; 0 before the first round.
        public int Round { get; private set; }

        public bool AiPlaysRight => Mode != DuelMode.LocalPvp;

        // The AI's drawn strike time for this round, for diagnostics; null once it has struck or in PvP.
        public double? AiStrikeAt => aiStrikeAt;

        public DuelPhase Phase => Duel?.Phase ?? DuelPhase.Ready;
        public DuelResult Result => Duel?.Result;
        public bool CanBeginRound => Duel == null || Duel.Phase == DuelPhase.Resolved;

        public event Action RoundStarted;
        public event Action<DuelPhase> PhaseChanged;
        public event Action<DuelResult> Resolved;
        public event Action<DuelMode> ModeChanged;

        // The seat a command's player id names, or null for an id that holds no seat.
        public static Side? SideOf(int playerId) =>
            playerId == LeftPlayerId ? Side.Left : playerId == RightPlayerId ? (Side?)Side.Right : null;

        // The AI difficulty a mode plays at; meaningless (but harmless) for LocalPvp, which plays no AI.
        public static AiDifficulty DifficultyOf(DuelMode mode) =>
            mode == DuelMode.VsAiEasy ? AiDifficulty.Easy : mode == DuelMode.VsAiHard ? AiDifficulty.Hard : AiDifficulty.Normal;

        public double CountdownRemaining(double now) => Duel?.CountdownRemaining(now) ?? 0;
        public int CountdownNumber(double now) => Duel?.CountdownNumber(now) ?? 0;

        // Starts a fresh round on `settings`, on behalf of the seat `playerId` names; false while one is
        // still running, or for a player id that holds no seat. A finished match is replaced first.
        public bool BeginRound(DuelSettings settings, double now, int playerId = LeftPlayerId)
        {
            if (!SideOf(playerId).HasValue || !CanBeginRound)
                return false;

            if (Match.IsOver)
                Match = new Match(winsNeeded);

            Duel = new Duel(settings);
            Duel.PhaseChanged += OnDuelPhaseChanged;
            Duel.Resolved += OnDuelResolved;
            Round++;
            ArenaRoll = ArenaDraw.Roll(random);
            aiStrikeAt = null;
            RoundStarted?.Invoke();
            Duel.Begin(now);
            return true;
        }

        // `at` is the strike's own input-event timestamp. Refused for a player id with no seat, or for
        // the seat the AI holds in a vs-AI mode -- only the AI may strike on its own behalf there.
        public void Strike(int playerId, double at)
        {
            var side = SideOf(playerId);
            if (!side.HasValue || (side == Side.Right && AiPlaysRight))
                return;
            Duel?.Strike(side.Value, at);
        }

        // Between rounds only (like the cue card says: Tab / Select cycles the mode between rounds);
        // false while a round is running, or for a player id that holds no seat. Always starts a fresh match.
        public bool SetMode(int playerId, DuelMode mode)
        {
            if (!SideOf(playerId).HasValue || !CanBeginRound)
                return false;
            ApplyMode(mode);
            return true;
        }

        // PvP -> AI Easy -> Normal -> Hard -> PvP, on behalf of the seat `playerId` names.
        public bool CycleMode(int playerId) => SetMode(playerId, (DuelMode)(((int)Mode + 1) % 4));

        // Advances the round clock and, in vs-AI modes, strikes on the AI's own behalf once its drawn
        // time arrives -- checked before the tick, so a settle window closing on the very same tick the
        // AI's strike lands still sees both sides struck (DuelSession used to run this ahead of the
        // controller's own tick for the same reason).
        public void Tick(double now)
        {
            if (aiStrikeAt.HasValue && Duel != null)
            {
                var phase = Duel.Phase;
                if ((phase == DuelPhase.Standoff || phase == DuelPhase.Strike) && now >= aiStrikeAt.Value)
                {
                    var at = aiStrikeAt.Value;
                    aiStrikeAt = null;
                    Duel.Strike(Side.Right, at);
                }
            }

            Duel?.Tick(now);
        }

        private void ApplyMode(DuelMode mode)
        {
            Mode = mode;
            ai = AiPlaysRight ? new AiOpponent(DifficultyOf(mode), aiTuning()) : null;
            aiStrikeAt = null;
            Match = new Match(winsNeeded);
            ModeChanged?.Invoke(mode);
        }

        private void OnDuelPhaseChanged(DuelPhase phase)
        {
            PhaseChanged?.Invoke(phase);
            if (phase == DuelPhase.Countdown && ai != null)
                aiStrikeAt = ai.StrikeTime(Duel);
        }

        private void OnDuelResolved(DuelResult result)
        {
            aiStrikeAt = null;
            Match.Record(result);
            Resolved?.Invoke(result);
        }
    }
}
