using System.Globalization;

namespace Flashlight
{
    /// <summary>Whose voice the one letter about the flashlight is in, which follows the shops that sell it.</summary>
    public enum LetterSender
    {
        None,
        Robin,
        RobinAndClint,
        Clint,
    }

    /// <summary>
    /// The game-state queries that decide what the shop sells, built here with no game types so they can be tested. They are
    /// read by the game, which expects a full stop in a number whatever the player's language is, so numbers are always written
    /// with the invariant culture.
    /// </summary>
    internal static class ShopRules
    {
        /// <summary>
        /// The letter is only for a player a shop actually sells to, so it follows the settings: Robin's when she sells it, Robin's
        /// with a line about Clint when both do, and Clint's when only he does.
        /// </summary>
        public static LetterSender LetterFor(bool sendMail, bool soldAtCarpenter, bool soldAtBlacksmith)
        {
            if (!sendMail)
                return LetterSender.None;
            if (soldAtCarpenter)
                return soldAtBlacksmith ? LetterSender.RobinAndClint : LetterSender.Robin;
            return soldAtBlacksmith ? LetterSender.Clint : LetterSender.None;
        }

        /// <summary>
        /// The game counts a shop's daily stock for each shop on its own, so two shops would each sell the full number. The daily
        /// limit is shared out between them instead, rounded up so a small limit still leaves each shop something.
        /// </summary>
        public static int StockPerShop(int dailyStock, int shops) =>
            Math.Max(1, (dailyStock + Math.Max(1, shops) - 1) / Math.Max(1, shops));

        /// <summary>The flashlight is only on sale to a player who does not already carry one.</summary>
        public static string OneFlashlight(string qualifiedFlashlightId) => $"!PLAYER_HAS_ITEM Current {qualifiedFlashlightId}";

        /// <summary>Robin's letter is only for a player who has no flashlight yet and has played at least <paramref name="minDays"/> days.</summary>
        public static string MailAd(string qualifiedFlashlightId, int minDays) =>
            $"{OneFlashlight(qualifiedFlashlightId)}, DAYS_PLAYED {Math.Max(1, minDays).ToString(CultureInfo.InvariantCulture)}";

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
