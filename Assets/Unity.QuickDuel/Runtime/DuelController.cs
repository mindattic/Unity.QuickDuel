using System;
using Unity.QuickDuel.Core;
using UnityEngine;

namespace Unity.QuickDuel
{
    // Owns the current round. Every time it hands the Duel is on Time.realtimeSinceStartupAsDouble, the
    // same clock Input System event timestamps use, so strikes are judged by when the key went down,
    // not by which frame noticed it.
    public sealed class DuelController : MonoBehaviour
    {
        [SerializeField] private bool beginOnStart;

        // Replaced by tests to shorten a round; applies from the next BeginRound.
        public DuelSettings Settings { get; set; } = new DuelSettings();

        // The clock for Tick and for Begin. Leave it alone outside tests.
        public Func<double> Clock { get; set; } = () => Time.realtimeSinceStartupAsDouble;

        public Duel Duel { get; private set; }
        public int Round { get; private set; }

        public double Now => Clock();
        public DuelPhase Phase => Duel?.Phase ?? DuelPhase.Ready;
        public DuelResult Result => Duel?.Result;
        public double CountdownRemaining => Duel?.CountdownRemaining(Now) ?? 0;
        public bool CanBeginRound => Duel == null || Duel.Phase == DuelPhase.Resolved;

        public event Action RoundStarted;
        public event Action<DuelPhase> PhaseChanged;
        public event Action<DuelResult> Resolved;

        private void Start()
        {
            if (beginOnStart)
                BeginRound();
        }

        private void Update() => Duel?.Tick(Now);

        // Starts a fresh round; false while one is still running.
        public bool BeginRound()
        {
            if (!CanBeginRound)
                return false;

            Duel = new Duel(Settings);
            Duel.PhaseChanged += phase => PhaseChanged?.Invoke(phase);
            Duel.Resolved += result => Resolved?.Invoke(result);
            Round++;
            RoundStarted?.Invoke();
            Duel.Begin(Now);
            return true;
        }

        // `at` is the strike's own timestamp on the controller's clock (an input event time).
        public void Strike(Side side, double at) => Duel?.Strike(side, at);
    }
}
