using System;

namespace Unity.QuickDuel.Core
{
    public enum AiDifficulty { Easy, Normal, Hard }

    // A computer duelist: it reacts to the cue after a reaction time drawn from its difficulty's range.
    public sealed class AiOpponent
    {
        private readonly Random random;

        public AiOpponent(AiDifficulty difficulty, Random random = null)
        {
            Difficulty = difficulty;
            this.random = random ?? new Random();
        }

        public AiDifficulty Difficulty { get; }

        public static (double Min, double Max) ReactionRange(AiDifficulty difficulty)
        {
            switch (difficulty)
            {
                case AiDifficulty.Easy: return (0.45, 0.70);
                case AiDifficulty.Normal: return (0.30, 0.45);
                case AiDifficulty.Hard: return (0.20, 0.30);
                default: throw new ArgumentOutOfRangeException(nameof(difficulty));
            }
        }

        public double NextReaction()
        {
            var (min, max) = ReactionRange(Difficulty);
            return min + random.NextDouble() * (max - min);
        }

        // Draws a fresh reaction for this round; strike with duel.Strike(side, that time) once the clock reaches it.
        public double StrikeTime(Duel duel)
        {
            if (duel == null)
                throw new ArgumentNullException(nameof(duel));
            if (duel.Phase == DuelPhase.Ready)
                throw new InvalidOperationException("The duel has not begun, so it has no cue time yet.");
            return duel.CueAt + NextReaction();
        }
    }
}
