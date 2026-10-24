using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.QuickDuel.Core;

namespace Unity.QuickDuel.Core.Tests
{
    public class DuelTests
    {
        private static Duel StartedDuel(out double cue, int seed = 7)
        {
            var duel = new Duel(TestTuning.Profile.Duel.ToSettings(), new Random(seed));
            duel.Begin(0);
            cue = duel.CueAt;
            return duel;
        }

        [Test]
        public void Cue_falls_after_countdown_inside_the_standoff_window()
        {
            for (var seed = 0; seed < 200; seed++)
            {
                StartedDuel(out var cue, seed);
                Assert.That(cue, Is.InRange(4.0 + 3.0, 4.0 + 6.5));
            }
        }

        [Test]
        public void Phases_advance_countdown_standoff_strike()
        {
            var duel = StartedDuel(out var cue);
            Assert.That(duel.Phase, Is.EqualTo(DuelPhase.Countdown));
            duel.Tick(4.0);
            Assert.That(duel.Phase, Is.EqualTo(DuelPhase.Standoff));
            duel.Tick(cue);
            Assert.That(duel.Phase, Is.EqualTo(DuelPhase.Strike));
        }

        [Test]
        public void The_countdown_is_Get_Ready_for_a_quarter_then_3_2_1_as_in_gameclock_countdown()
        {
            var duel = StartedDuel(out _);
            var shown = new[] { 0.0, 0.99, 1.0, 1.99, 2.0, 2.99, 3.0, 3.99 };
            Assert.That(Array.ConvertAll(shown, duel.CountdownNumber), Is.EqualTo(new[] { 0, 0, 3, 3, 2, 2, 1, 1 }));
        }

        [Test]
        public void Faster_strike_wins()
        {
            var duel = StartedDuel(out var cue);
            duel.Tick(cue);
            duel.Strike(Side.Right, cue + 0.21);
            duel.Strike(Side.Left, cue + 0.30);

            Assert.That(duel.Phase, Is.EqualTo(DuelPhase.Resolved));
            Assert.That(duel.Result.Outcome, Is.EqualTo(DuelOutcome.FasterStrike));
            Assert.That(duel.Result.Winner, Is.EqualTo(Side.Right));
            Assert.That(duel.Result.RightReaction, Is.EqualTo(0.21).Within(1e-9));
        }

        [Test]
        public void Lone_strike_wins_once_the_settle_window_passes()
        {
            var duel = StartedDuel(out var cue);
            duel.Tick(cue);
            duel.Strike(Side.Left, cue + 0.25);
            duel.Tick(cue + 0.26);
            Assert.That(duel.Phase, Is.EqualTo(DuelPhase.Strike));

            duel.Tick(cue + 0.31);
            Assert.That(duel.Result.Winner, Is.EqualTo(Side.Left));
            Assert.That(duel.Result.RightReaction, Is.Null);
        }

        [Test]
        public void Events_delivered_out_of_order_are_judged_by_timestamp()
        {
            var duel = StartedDuel(out var cue);
            duel.Tick(cue);
            duel.Strike(Side.Left, cue + 0.30);
            duel.Strike(Side.Right, cue + 0.29);
            Assert.That(duel.Result.Winner, Is.EqualTo(Side.Right));
        }

        [Test]
        public void Striking_before_the_cue_loses()
        {
            var duel = StartedDuel(out var cue);
            duel.Tick(4.0);
            duel.Strike(Side.Left, 4.0);

            Assert.That(duel.Result.Outcome, Is.EqualTo(DuelOutcome.EarlyStrike));
            Assert.That(duel.Result.Winner, Is.EqualTo(Side.Right));
            Assert.That(duel.Result.LeftReaction, Is.EqualTo(4.0 - cue).Within(1e-9));
        }

        [Test]
        public void Striking_during_the_countdown_is_ignored()
        {
            var duel = StartedDuel(out var cue);
            duel.Strike(Side.Right, 1.0);

            Assert.That(duel.Phase, Is.EqualTo(DuelPhase.Countdown));
            Assert.That(duel.Result, Is.Null);

            // The ignored press didn't consume Right's strike, so the round still plays out normally.
            duel.Tick(cue);
            duel.Strike(Side.Left, cue + 0.2);
            duel.Strike(Side.Right, cue + 0.3);
            Assert.That(duel.Result.Winner, Is.EqualTo(Side.Left));
            Assert.That(duel.Result.Outcome, Is.EqualTo(DuelOutcome.FasterStrike));
        }

        [Test]
        public void Strike_timestamped_after_the_cue_counts_even_before_the_tick_sees_it()
        {
            var duel = StartedDuel(out var cue);
            duel.Tick(cue - 0.01);
            duel.Strike(Side.Left, cue + 0.18);
            duel.Strike(Side.Right, cue + 0.40);
            Assert.That(duel.Result.Winner, Is.EqualTo(Side.Left));
        }

        [Test]
        public void Identical_reactions_tie()
        {
            var duel = StartedDuel(out var cue);
            duel.Tick(cue);
            duel.Strike(Side.Left, cue + 0.2);
            duel.Strike(Side.Right, cue + 0.2);
            Assert.That(duel.Result.Outcome, Is.EqualTo(DuelOutcome.Tie));
            Assert.That(duel.Result.Winner, Is.Null);
        }

        [Test]
        public void Nobody_striking_ends_with_no_winner()
        {
            var duel = StartedDuel(out var cue);
            duel.Tick(cue);
            duel.Tick(cue + 3.0);
            Assert.That(duel.Result.Outcome, Is.EqualTo(DuelOutcome.NoStrike));
            Assert.That(duel.Result.Winner, Is.Null);
        }

        [Test]
        public void Strikes_after_resolution_are_ignored()
        {
            var duel = StartedDuel(out var cue);
            duel.Tick(4.0);
            duel.Strike(Side.Left, 4.0);   // during the standoff, before the cue: an early strike resolves the round
            var result = duel.Result;
            duel.Strike(Side.Right, cue + 0.1);
            Assert.That(duel.Result, Is.SameAs(result));
        }

        [Test]
        public void Phase_changes_and_resolution_are_announced()
        {
            var duel = new Duel(TestTuning.Profile.Duel.ToSettings(), new Random(1));
            var phases = new List<DuelPhase>();
            DuelResult resolved = null;
            duel.PhaseChanged += phases.Add;
            duel.Resolved += r => resolved = r;

            duel.Begin(0);
            duel.Tick(duel.CueAt);
            duel.Strike(Side.Left, duel.CueAt + 0.2);
            duel.Strike(Side.Right, duel.CueAt + 0.3);

            Assert.That(phases, Is.EqualTo(new[]
            {
                DuelPhase.Countdown, DuelPhase.Standoff, DuelPhase.Strike, DuelPhase.Resolved
            }));
            Assert.That(resolved, Is.SameAs(duel.Result));
        }

        [Test]
        public void Begin_twice_throws()
        {
            var duel = StartedDuel(out _);
            Assert.Throws<InvalidOperationException>(() => duel.Begin(0));
        }
    }
}
