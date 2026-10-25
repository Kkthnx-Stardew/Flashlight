namespace Flashlight
{
    /// <summary>
    /// The geometry of the beam's shadows. This file has no game dependencies so the tests can compile it.
    /// A point is lit when a straight line from the lens to it passes through nothing that blocks light.
    /// </summary>
    internal static class BeamShadow
    {
        /// <summary>Returned by the lookup for a tile that blocks nothing.</summary>
        public const float Open = -1f;

        /// <summary>
        /// Whether a line from one point to another (both in tiles, so a world position divided by 64) runs clear. The lookup says,
        /// for a tile, how much of each side of it is left open: <see cref="Open"/> for a tile that blocks nothing, 0 for one that is
        /// blocked right to its edges (a wall), and a fraction for a thin thing standing in the middle of its tile (a trunk).
        /// The tile the line starts in never blocks, so a farmer standing next to a tree is not shut in by it. The tile the line ends
        /// in does, so a point inside a blocking thing is not lit, unless <paramref name="ignoreEnd"/> is set, which asks only whether
        /// the way to that tile is clear.
        /// </summary>
        public static bool IsClear(float fromX, float fromY, float toX, float toY, Func<int, int, float> inset, bool ignoreEnd = false)
        {
            var x = (int)Math.Floor(fromX);
            var y = (int)Math.Floor(fromY);
            var endX = (int)Math.Floor(toX);
            var endY = (int)Math.Floor(toY);
            if (x == endX && y == endY)
                return true;

            var dx = toX - fromX;
            var dy = toY - fromY;
            var stepX = dx > 0 ? 1 : -1;
            var stepY = dy > 0 ? 1 : -1;
            // how far along the line (0 to 1) it is when it reaches the next tile edge, and how far apart the edges are
            var nextX = dx == 0 ? float.PositiveInfinity : ((dx > 0 ? x + 1 : x) - fromX) / dx;
            var nextY = dy == 0 ? float.PositiveInfinity : ((dy > 0 ? y + 1 : y) - fromY) / dy;
            var deltaX = dx == 0 ? float.PositiveInfinity : stepX / dx;
            var deltaY = dy == 0 ? float.PositiveInfinity : stepY / dy;

            var steps = Math.Abs(endX - x) + Math.Abs(endY - y) - (ignoreEnd ? 1 : 0);
            for (var i = 0; i < steps; i++)
            {
                if (nextX < nextY)
                {
                    x += stepX;
                    nextX += deltaX;
                }
                else
                {
                    y += stepY;
                    nextY += deltaY;
                }

                var margin = inset(x, y);
                if (margin >= 0f && Hits(fromX, fromY, dx, dy, x + margin, y + margin, x + 1 - margin, y + 1 - margin))
                    return false;
            }
            return true;
        }

        /// <summary>Whether the line from a point, going by (dx, dy), touches a box at some point along its length.</summary>
        private static bool Hits(float fromX, float fromY, float dx, float dy, float left, float top, float right, float bottom)
        {
            var enter = 0f;
            var leave = 1f;
            return Slab(fromX, dx, left, right, ref enter, ref leave) && Slab(fromY, dy, top, bottom, ref enter, ref leave);
        }

        private static bool Slab(float from, float d, float low, float high, ref float enter, ref float leave)
        {
            if (Math.Abs(d) < 1e-6f)
                return from >= low && from <= high;
            var a = (low - from) / d;
            var b = (high - from) / d;
            if (a > b)
                (a, b) = (b, a);
            enter = Math.Max(enter, a);
            leave = Math.Min(leave, b);
            return enter <= leave;
        }

        /// <summary>
        /// Softens the edge of a shadow: each value becomes the average of the nine around it, so a shadow fades over about a
        /// pixel and a half of the beam picture instead of ending in a staircase of tiles.
        /// </summary>
        public static void Soften(byte[] values, ushort[] sums, int size)
        {
            if (size < 2)
                return;

            // a 3 by 3 average is a sum of three across, then three down, and the edge repeats its own value
            for (var y = 0; y < size; y++)
            {
                var row = y * size;
                sums[row] = (ushort)(2 * values[row] + values[row + 1]);
                for (var x = 1; x < size - 1; x++)
                    sums[row + x] = (ushort)(values[row + x - 1] + values[row + x] + values[row + x + 1]);
                sums[row + size - 1] = (ushort)(values[row + size - 2] + 2 * values[row + size - 1]);
            }
            for (var y = 0; y < size; y++)
            {
                var above = Math.Max(y - 1, 0) * size;
                var row = y * size;
                var below = Math.Min(y + 1, size - 1) * size;
                for (var x = 0; x < size; x++)
                    values[row + x] = (byte)((sums[above + x] + sums[row + x] + sums[below + x]) / 9);
            }
        }
    }
}
