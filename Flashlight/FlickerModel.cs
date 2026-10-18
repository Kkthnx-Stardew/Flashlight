namespace Flashlight
{
    /// <summary>
    /// The random flicker of an old flashlight with a loose cell, kept apart from the game so it can be tested.
    /// The game runs 60 updates a second. A flicker is a short run of updates in which the beam dips or drops out.
    /// </summary>
    public static class FlickerModel
    {
        public const int TicksPerSecond = 60;

        /// <summary>While the charge is nearly gone, flickers come about this often (in seconds).</summary>
        public const int LowBatterySecondsBetween = 3;

        /// <summary>Whether a flicker should start on this update, given the average seconds between flickers (0 means never).</summary>
        public static bool ShouldStart(Random random, int secondsBetween)
        {
            if (secondsBetween <= 0)
                return false;
            return random.NextDouble() < 1.0 / (secondsBetween * TicksPerSecond);
        }

        /// <summary>How many updates a new flicker lasts: between a tenth of a second and half a second.</summary>
        public static int NewLength(Random random) => random.Next(6, 31);

        /// <summary>The seconds between flickers to use: shorter when the battery is nearly flat.</summary>
        public static int Interval(int configured, bool low) => low ? LowBatterySecondsBetween : configured;

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
