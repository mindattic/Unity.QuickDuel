using System;
using Unity.QuickDuel.Core;
using UnityEngine;

namespace Unity.QuickDuel
{
    // The thin Unity-facing view of Core's MatchSession, the one authority over the round, mode and
    // match (UQD-T30): this only threads Unity's clock and tuning through to its commands and republishes
    // its events, the same way it hands the Duel Time.realtimeSinceStartupAsDouble, the same clock Input
    // System event timestamps use, so strikes are judged by when the key went down, not by which frame
    // noticed it.
    public sealed class DuelController : MonoBehaviour
    {
        [SerializeField] private bool beginOnStart;

        // Replaced by tests to shorten a round; applies from the next BeginRound.
        public DuelSettings Settings
        {
            get => settings ?? (settings = TuningStore.Profile.Duel.ToSettings());
            set => settings = value;
        }

        private DuelSettings settings;

        // The clock for Tick and for Begin. Leave it alone outside tests.
        public Func<double> Clock { get; set; } = () => Time.realtimeSinceStartupAsDouble;

        // The one authority over this round, the mode and the match; DuelSession is its only other reader.
        // Built lazily (like Settings) rather than in Awake: DuelView and DuelSession read it from their
        // own Awake, and Unity does not guarantee this component's Awake runs first.
        public MatchSession Session => session ?? (session = new MatchSession(TuningStore.Profile.Ai, TuningStore.Profile.Match.WinsNeeded));

        private MatchSession session;

        public Duel Duel => Session.Duel;
        public int Round => Session.Round;

        public double Now => Clock();
        public DuelPhase Phase => Session.Phase;
        public DuelResult Result => Session.Result;
        public double CountdownRemaining => Session.CountdownRemaining(Now);
        public int CountdownNumber => Session.CountdownNumber(Now);
        public bool CanBeginRound => Session.CanBeginRound;

        public event Action RoundStarted;
        public event Action<DuelPhase> PhaseChanged;
        public event Action<DuelResult> Resolved;

        private void Awake()
        {
            Session.RoundStarted += () => RoundStarted?.Invoke();
            Session.PhaseChanged += phase => PhaseChanged?.Invoke(phase);
            Session.Resolved += result => Resolved?.Invoke(result);
        }

        private void Start()
        {
            if (beginOnStart)
                BeginRound();
        }

        private void Update() => Session.Tick(Now);

        // Starts a fresh round on the local machine's behalf; false while one is still running.
        public bool BeginRound() => Session.BeginRound(Settings, Now);

        // `at` is the strike's own timestamp on the controller's clock (an input event time). Side maps
        // straight onto MatchSession's two fixed local seats (left = player 0, right = player 1).
        public void Strike(Side side, double at) =>
            Session.Strike(side == Side.Left ? MatchSession.LeftPlayerId : MatchSession.RightPlayerId, at);
    }
}
