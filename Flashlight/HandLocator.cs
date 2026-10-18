namespace Flashlight
{
    /// <summary>
    /// Finds the hand in one cell of the game's farmer sheet. The sheet draws each arm as its own picture, and the hand is the
    /// patch at the bottom of it, so the hand is the middle of the lowest few rows of pixels that are not see-through.
    /// This takes plain alpha values so it can be tested without the game.
    /// </summary>
    public static class HandLocator
    {
        /// <summary>How many rows up from the lowest pixel count as the hand.</summary>
        public const int HandRows = 3;

        /// <summary>Where the hand is, in pixels from the top left of the cell. The middle of a pixel is its coordinate plus a half.</summary>
        public readonly record struct Hand(float X, float Y);

        /// <summary>
        /// Looks for the hand in a cell of <paramref name="width"/> by <paramref name="height"/> pixels, given the alpha of each
        /// pixel row by row. Returns null when the cell has nothing drawn in it.
        /// </summary>
        public static Hand? Find(ReadOnlySpan<byte> alpha, int width, int height) => Find(alpha, width, height, 0, width);

        /// <summary>
        /// The same, looking only at the columns from <paramref name="fromX"/> up to but not including <paramref name="toX"/>. The
        /// views from the front and the back draw both arms in one cell, so one hand is found by looking at one half.
        /// </summary>
        public static Hand? Find(ReadOnlySpan<byte> alpha, int width, int height, int fromX, int toX)
        {
            if (width <= 0 || height <= 0 || alpha.Length < width * height)
                return null;
            fromX = Math.Clamp(fromX, 0, width);
            toX = Math.Clamp(toX, fromX, width);

            var lowest = -1;
            for (var y = height - 1; y >= 0 && lowest < 0; y--)
            {
                for (var x = fromX; x < toX; x++)
                {
                    if (alpha[y * width + x] > 0)
                    {
                        lowest = y;
                        break;
                    }
                }
            }
            if (lowest < 0)
                return null;

            double sumX = 0, sumY = 0;
            var count = 0;
            for (var y = Math.Max(0, lowest - HandRows + 1); y <= lowest; y++)
            {
                for (var x = fromX; x < toX; x++)
                {
                    if (alpha[y * width + x] == 0)
                        continue;
                    sumX += x + 0.5;
                    sumY += y + 0.5;
                    count++;
                }
            }
            return new Hand((float)(sumX / count), (float)(sumY / count));
        }
    }
}
