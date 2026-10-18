namespace Flashlight
{
    /// <summary>The colour steps of the charge bar under the flashlight's picture, like the water bar of the watering can.</summary>
    public enum ChargeStage
    {
        Empty,
        Red,
        Orange,
        Yellow,
        Green,
    }

    public static class ChargeBar
    {
        /// <summary>The reserve under which the flashlight is "low": the bar is red and the beam starts to flicker more.</summary>
        public const float LowFraction = 0.10f;

        /// <summary>The colour step for a share of a full load. Steps go green, yellow, orange, red as it falls.</summary>
        public static ChargeStage StageFor(float fraction)
        {
            if (fraction <= 0f)
                return ChargeStage.Empty;
            if (fraction >= 0.5f)
                return ChargeStage.Green;
            if (fraction >= 0.25f)
                return ChargeStage.Yellow;
            if (fraction >= LowFraction)
                return ChargeStage.Orange;
            return ChargeStage.Red;
        }

        /// <summary>
        /// The width of the filled part of a bar that is <paramref name="fullWidth"/> wide. Any charge at all shows at least one
        /// pixel, so a nearly flat flashlight is not drawn as an empty one.
        /// </summary>
        public static int FilledWidth(float fraction, int fullWidth)
        {
            if (fraction <= 0f)
                return 0;
            return Math.Clamp((int)Math.Round(fraction * fullWidth), 1, fullWidth);
        }
    }
}
