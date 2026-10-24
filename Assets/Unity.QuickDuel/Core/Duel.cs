using System;

namespace Unity.QuickDuel.Core
{
    public enum Side { Left, Right }

    public enum DuelPhase
    {
        Ready,
        Countdown,
        Standoff,
        Strike,
        Resolved
    }

    public enum DuelOutcome
    {
        FasterStrike,
        EarlyStrike,
        Tie,
        NoStrike,
        Forfeit
    }

    public sealed class DuelResult
    {
        public DuelResult(DuelOutcome outcome, Side? winner, double? leftReaction, double? rightReaction)
        {
            Outcome = outcome;
            Winner = winner;
            LeftReaction = leftReaction;
            RightReaction = rightReaction;
        }

        public DuelOutcome Outcome { get; }
        public Side? Winner { get; }

        // Seconds from the cue to the strike; null for no strike, negative for a strike before the cue.
        public double? LeftReaction { get; }
        public double? RightReaction { get; }

        public double? ReactionOf(Side side) => side == Side.Left ? LeftReaction : RightReaction;
    }

    public sealed class DuelSettings
    {
        // The figures are tuning data (Settings/tuning.json, DuelTuning.ToSettings), not defaults in code.
        public double CountdownSeconds { get; set; }
        public double MinStandoffSeconds { get; set; }
        public double MaxStandoffSeconds { get; set; }
        public double StrikeTimeoutSeconds { get; set; }

        // How long to wait after the first strike for a later-delivered input event that happened earlier.
        public double SettleSeconds { get; set; }
    }

    // One round: wait, the cue comes, the first to strike wins; striking before the cue loses.
    // Every time is a caller-supplied timestamp in seconds, so input event times can be used directly.
    public sealed class Duel
    {
        private readonly DuelSettings settings;
        private readonly Random random;
        private double countdownEndsAt;
        private double? leftStrikeAt;
        private double? rightStrikeAt;
        private double? firstStrikeAt;

        public Duel(DuelSettings settings, Random random = null)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.random = random ?? new Random();
            if (this.settings.MinStandoffSeconds > this.settings.MaxStandoffSeconds)
                throw new ArgumentException("MinStandoffSeconds must not exceed MaxStandoffSeconds.");
        }

        public DuelPhase Phase { get; private set; } = DuelPhase.Ready;
        public double CueAt { get; private set; }
        public DuelResult Result { get; private set; }

        public event Action<DuelPhase> PhaseChanged;
        public event Action<DuelResult> Resolved;

        public void Begin(double now)
        {
            var span = settings.MaxStandoffSeconds - settings.MinStandoffSeconds;
            Begin(now, settings.MinStandoffSeconds + random.NextDouble() * span);
        }

        // Begins with a standoff length chosen elsewhere: an online client uses the one its host drew.
        public void Begin(double now, double standoffSeconds)
        {
            if (Phase != DuelPhase.Ready)
                throw new InvalidOperationException("A duel can only begin once.");
            if (double.IsNaN(standoffSeconds) || double.IsInfinity(standoffSeconds) || standoffSeconds < 0)
                throw new ArgumentOutOfRangeException(nameof(standoffSeconds), "The standoff must be a finite, non-negative length.");

            countdownEndsAt = now + settings.CountdownSeconds;
            CueAt = countdownEndsAt + standoffSeconds;
            SetPhase(DuelPhase.Countdown);
            Tick(now);
        }

        // Seconds left in the countdown, for display.
        public double CountdownRemaining(double now) => Math.Max(0, countdownEndsAt - now);

        // The number the countdown shows: obj_gameclock's countdown is "Get Ready" for its first quarter, then 3, 2, 1
        // as each further quarter begins (gameclock_countdown.gml). 0 means Get Ready, nothing numbered yet.
        public int CountdownNumber(double now)
        {
            var length = settings.CountdownSeconds;
            if (length <= 0)
                return 0;
            var quarter = (int)Math.Floor((length - CountdownRemaining(now)) / (length / 4));
            return quarter < 1 ? 0 : Math.Max(1, 4 - Math.Min(quarter, 3));
        }

        public void Tick(double now)
        {
            if (Phase == DuelPhase.Countdown && now >= countdownEndsAt)
                SetPhase(DuelPhase.Standoff);

            if (Phase == DuelPhase.Standoff && now >= CueAt)
                SetPhase(DuelPhase.Strike);

            if (Phase != DuelPhase.Strike)
                return;

            if (firstStrikeAt.HasValue && now >= firstStrikeAt.Value + settings.SettleSeconds)
                ResolveByReaction();
            else if (!firstStrikeAt.HasValue && now >= CueAt + settings.StrikeTimeoutSeconds)
                Resolve(new DuelResult(DuelOutcome.NoStrike, null, null, null));
        }

        public void Strike(Side side, double at)
        {
            if (Phase == DuelPhase.Ready || Phase == DuelPhase.Resolved)
                return;
            if ((side == Side.Left ? leftStrikeAt : rightStrikeAt).HasValue)
                return;

            // A strike timestamped during the 3-2-1 countdown is too early to mean anything and is ignored,
            // not punished: only a strike during the standoff (after the countdown, before the cue) loses
            // the round outright (BIBLE §2). Timestamp, not Phase, decides this for the same reason the
            // CueAt check below does: an input event can be timestamped past a boundary before Tick sees it.
            if (at < countdownEndsAt)
                return;

            if (at < CueAt)
            {
                var reaction = at - CueAt;
                Resolve(DuelJudge.Judge(
                    side == Side.Left ? reaction : (double?)null,
                    side == Side.Right ? reaction : (double?)null,
                    0));
                return;
            }

            if (Phase != DuelPhase.Strike)
                SetPhase(DuelPhase.Strike);

            if (side == Side.Left) leftStrikeAt = at; else rightStrikeAt = at;
            if (!firstStrikeAt.HasValue || at < firstStrikeAt.Value)
                firstStrikeAt = at;

            if (leftStrikeAt.HasValue && rightStrikeAt.HasValue)
                ResolveByReaction();
        }

        // Local strikes are real input events, so the online human-floor check does not apply.
        private void ResolveByReaction() => Resolve(DuelJudge.Judge(leftStrikeAt - CueAt, rightStrikeAt - CueAt, 0));

        private void Resolve(DuelResult result)
        {
            Result = result;
            SetPhase(DuelPhase.Resolved);
            Resolved?.Invoke(result);
        }

        private void SetPhase(DuelPhase phase)
        {
            Phase = phase;
            PhaseChanged?.Invoke(phase);
        }
    }
}
