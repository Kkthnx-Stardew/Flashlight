namespace Flashlight
{
    /// <summary>
    /// Carries the true size of a beam to the other players. The game sends a light's radius to them, but holds a new value back
    /// until the old one has settled, so a flicker that changes the radius every few updates never arrives. The texture index is
    /// sent at once, and the game ignores a number it does not know, so the beam's size rides in it. A player without this mod
    /// keeps the lantern picture and the plain radius, which is harmless. This file has no game dependencies so the tests can compile it.
    /// </summary>
    internal static class BeamSignal
    {
        /// <summary>The game's own picture numbers are below ten. Anything from here up is a beam size in hundredths of a tile.</summary>
        public const int Base = 10_000;

        private const int Largest = 100_000;

        public static int Encode(float radius) =>
            float.IsNaN(radius) ? Base : Base + (int)Math.Clamp(MathF.Round(radius * 100f), 0f, Largest);

        public static bool TryDecode(int textureIndex, out float radius)
        {
            if (textureIndex < Base)
            {
                radius = 0f;
                return false;
            }
            radius = (textureIndex - Base) / 100f;
            return true;
        }
    }
}
