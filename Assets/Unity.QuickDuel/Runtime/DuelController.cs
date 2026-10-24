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

        // The one authority over this round, the mode and the match; DuelSession is its only other reader,
        // and `internal` (rather than `public`) keeps it that way -- nothing outside this assembly can reach
        // MatchSession.Duel or .Match to Strike, Tick or Record behind the player-id checks (UQD-T30).
        // Built lazily (like Settings) rather than in Awake: DuelView and DuelSession read it from their
        // own Awake, and Unity does not guarantee this component's Awake runs first. The tuning is a
        // provider, not a value read once, so a profile swapped later still reaches the next mode change.
        internal MatchSession Session => session ?? (session = new MatchSession(() => TuningStore.Profile.Ai, TuningStore.Profile.Match.WinsNeeded));

        private MatchSession session;

        // The online duel driving the scene instead of the local MatchSession (UQD-T21), or null for local play.
        // While set, the round, its phase, the verdict and the match all come from it; the local session stands
        // idle (and its AI with it) and is picked up again, unchanged, once this is cleared.
        public IOnlineDuelSource Online
        {
            get => online;
            set
            {
                if (online == value)
                    return;
                if (online != null)
                {
                    online.LocalRoundBegan -= OnOnlineRoundBegan;
                    online.Judged -= OnOnlineJudged;
                    online.SeatsChanged -= OnOnlineSeatsChanged;
                    UnhookOnlineRound();
                }
                online = value;
                if (online != null)
                {
                    online.LocalRoundBegan += OnOnlineRoundBegan;
                    online.Judged += OnOnlineJudged;
                    online.SeatsChanged += OnOnlineSeatsChanged;
                    if (online.Round != null)
                        HookOnlineRound(online.Round);
                }
                OnlineChanged?.Invoke();
            }
        }

        private IOnlineDuelSource online;
        private Duel onlineRound;

        public bool IsOnline => online != null;

        // This machine's seat online; null in local play (both seats are on this machine) or for a spectator.
        public Side? LocalSide => online?.LocalSide;

        public int Round => online != null ? online.RoundNumber : Session.Round;

        public double Now => Clock();

        // Online, a round shows Resolved only once the host's verdict for it is in; until then it shows the local
        // round's own phase, or Resolved with no Result yet (AwaitingVerdict) once this side's part of it is over.
        public DuelPhase Phase
        {
            get
            {
                if (online == null)
                    return Session.Phase;
                if (online.Round == null)
                    return DuelPhase.Ready;
                return HasOnlineVerdict ? DuelPhase.Resolved : online.Round.Phase;
            }
        }

        public DuelResult Result => online == null ? Session.Result : HasOnlineVerdict ? online.Verdict : null;

        // Online only: this side's round is over and its report is with the host, whose verdict has not come back.
        public bool AwaitingVerdict => online != null && online.Round != null && !HasOnlineVerdict
            && online.Round.Phase == DuelPhase.Resolved;

        private bool HasOnlineVerdict => online.RoundNumber > 0 && online.JudgedRound >= online.RoundNumber;

        // The match shown on the scoreboard: the local session's, or online the copy built from the host's verdicts.
        public IMatchView Match => online != null ? online.Match : Session.Match;

        // The cue timestamp of the round in progress (or last played); 0 before the first round. The live
        // Duel itself stays inside MatchSession so nothing outside it can Strike or Tick it directly.
        public double CueAt => CurrentDuel?.CueAt ?? 0;
        public double CountdownRemaining => CurrentDuel?.CountdownRemaining(Now) ?? 0;
        public int CountdownNumber => CurrentDuel?.CountdownNumber(Now) ?? 0;

        // Between rounds. Online that means no round is being played here and the last one has its verdict, so a
        // client can still quit between rounds even though only the host may begin the next one.
        public bool CanBeginRound => online == null
            ? Session.CanBeginRound
            : online.Round == null || HasOnlineVerdict;

        // Online: whether this peer can begin the next round now (the host, both seats taken, between rounds).
        public bool CanBeginOnlineRound => online != null && online.IsHost && online.Seated && CanBeginRound;

        private Duel CurrentDuel => online != null ? online.Round : Session.Duel;

        public event Action RoundStarted;
        public event Action<DuelPhase> PhaseChanged;
        public event Action<DuelResult> Resolved;

        // Online play began or ended, or its seats changed: the labels and prompts need redrawing.
        public event Action OnlineChanged;

        private void Awake()
        {
            Session.RoundStarted += () => { if (online == null) RoundStarted?.Invoke(); };
            Session.PhaseChanged += phase => { if (online == null) PhaseChanged?.Invoke(phase); };
            Session.Resolved += result => { if (online == null) Resolved?.Invoke(result); };
        }

        private void Start()
        {
            if (beginOnStart)
                BeginRound();
        }

        private void Update()
        {
            // An online round is ticked by the OnlineDuel that owns it, on the same clock.
            if (online == null)
                Session.Tick(Now);
        }

        private void OnDestroy() => Online = null;

        // Starts a fresh round on the local machine's behalf; false while one is still running. Online only the
        // host can, once both seats are taken; a client's Space / Start does nothing.
        public bool BeginRound() => online != null ? online.BeginRound() : Session.BeginRound(Settings, Now);

        // `at` is the strike's own timestamp on the controller's clock (an input event time). Side maps
        // straight onto MatchSession's two fixed local seats (left = player 0, right = player 1). Online this
        // machine holds one seat only, so every strike key strikes for it, whichever side it names.
        public void Strike(Side side, double at)
        {
            if (online != null)
            {
                online.Strike(at);
                return;
            }
            Session.Strike(side == Side.Left ? MatchSession.LeftPlayerId : MatchSession.RightPlayerId, at);
        }

        private void OnOnlineRoundBegan(Duel duel)
        {
            HookOnlineRound(duel);
            RoundStarted?.Invoke();
            PhaseChanged?.Invoke(duel.Phase);
        }

        private void HookOnlineRound(Duel duel)
        {
            UnhookOnlineRound();
            onlineRound = duel;
            onlineRound.PhaseChanged += OnOnlineRoundPhaseChanged;
        }

        private void UnhookOnlineRound()
        {
            if (onlineRound != null)
                onlineRound.PhaseChanged -= OnOnlineRoundPhaseChanged;
            onlineRound = null;
        }

        // The local round's own Resolved is not the verdict (it saw only this side's strike), so it is held back
        // until the host's arrives (OnOnlineJudged).
        private void OnOnlineRoundPhaseChanged(DuelPhase phase)
        {
            if (phase != DuelPhase.Resolved && !HasOnlineVerdict)
                PhaseChanged?.Invoke(phase);
        }

        private void OnOnlineJudged(int round, DuelResult verdict)
        {
            if (round != online.RoundNumber)
                return;
            PhaseChanged?.Invoke(DuelPhase.Resolved);
            Resolved?.Invoke(verdict);
        }

        private void OnOnlineSeatsChanged() => OnlineChanged?.Invoke();
    }
}
