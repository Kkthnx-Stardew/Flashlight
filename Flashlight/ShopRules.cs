using System.Globalization;

namespace Flashlight
{
    /// <summary>
    /// The game-state queries that decide what the shop sells, built here with no game types so they can be tested. They are
    /// read by the game, which expects a full stop in a number whatever the player's language is, so numbers are always written
    /// with the invariant culture.
    /// </summary>
    internal static class ShopRules
    {
        /// <summary>The flashlight is only on sale to a player who does not already carry one.</summary>
        public static string OneFlashlight(string qualifiedFlashlightId) => $"!PLAYER_HAS_ITEM Current {qualifiedFlashlightId}";

        /// <summary>Cells are only on sale to a player who carries fewer than <paramref name="maxCarried"/>.</summary>
        public static string CarryLimit(string qualifiedBatteryId, int maxCarried) =>
            $"PLAYER_HAS_ITEM Current {qualifiedBatteryId} 0 {Math.Max(0, maxCarried - 1).ToString(CultureInfo.InvariantCulture)}";

        /// <summary>
        /// A chance that is rolled once a day and is the same for every player and every time the shop is opened, whatever else
        /// is for sale. <paramref name="percent"/> is 0 to 100.
        /// </summary>
        public static string DailyRoll(string rollName, int percent) =>
            $"SYNCED_RANDOM day {rollName} {(Math.Clamp(percent, 0, 100) / 100.0).ToString("0.##", CultureInfo.InvariantCulture)}";
    }
}
