namespace Flashlight
{
    /// <summary>
    /// The random flicker of an old flashlight with a loose cell, kept apart from the game so it can be tested.
    /// The game runs 60 updates a second. A flicker is a short run of updates in which the beam dips or drops out.
    /// </summary>
    public static class FlickerModel
    {
        public const int TicksPerSecond = 60;

        /// <summary>The shortest gap between flickers, reached when the reserve is nearly gone.</summary>
        public const int LowestSecondsBetween = 3;

        /// <summary>The gap used as the starting point of the low-battery ramp when random flicker is switched off.</summary>
        public const int FallbackSecondsBetween = 40;

        /// <summary>Whether a flicker should start on this update, given the average seconds between flickers (0 means never).</summary>
        public static bool ShouldStart(Random random, int secondsBetween)
        {
            if (secondsBetween <= 0)
                return false;
            return random.NextDouble() < 1.0 / (secondsBetween * TicksPerSecond);
        }

        /// <summary>How many updates a new flicker lasts: between a tenth of a second and half a second.</summary>
        public static int NewLength(Random random) => random.Next(6, 31);

        /// <summary>
        /// The average seconds between flickers to use. With a healthy reserve it is the setting (0 for none). Once the reserve
        /// is under <see cref="ChargeBar.LowFraction"/> the gap shrinks quickly, from the setting at 10 percent (about 12 seconds at 5 percent) down to
        /// <see cref="LowestSecondsBetween"/> at none, and it applies even when the random flicker is switched off, as a warning.
        /// </summary>
        public static int Interval(int configured, float reserveFraction)
        {
            if (reserveFraction >= ChargeBar.LowFraction)
                return configured;

            var start = configured > 0 ? configured : FallbackSecondsBetween;
            var share = Math.Clamp(reserveFraction / ChargeBar.LowFraction, 0f, 1f); // 1 at the edge of "low", 0 when empty
            var seconds = LowestSecondsBetween + (start - LowestSecondsBetween) * share * share; // steeper than a straight line
            return Math.Max(LowestSecondsBetween, (int)Math.Round(seconds));
        }

        /// <summary>
        /// The share of the beam to show on an update of a flicker, from 0 (dark) to 1 (full). It stutters: some updates
        /// are dark, some dim, and the last few return to full.
        /// </summary>
        public static float Strength(int ticksLeft, Random random)
        {
            if (ticksLeft <= 0)
                return 1f;
            if (ticksLeft <= 3)
                return 0.85f; // settling back
            return random.Next(4) switch
            {
                0 => 0f,
                1 => 0.25f,
                2 => 0.5f,
                _ => 0.75f,
            };
        }
    }
}
