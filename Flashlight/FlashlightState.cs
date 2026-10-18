using StardewValley;

namespace Flashlight
{
    /// <summary>
    /// Reads and writes what a flashlight remembers: the charge of the cell in use (kept in its mod data, so it is saved with
    /// the item) and the cells loaded in its slots. Shared by the mod and by the drawing patches, which run every frame, so
    /// nothing here allocates.
    /// </summary>
    internal static class FlashlightState
    {
        public const string ChargeKey = ContentSetup.ModId + "/Charge";

        /// <summary>Present in a flashlight's mod data while it is switched on. It is saved with the item, so it is cleared when a day starts.</summary>
        public const string LitKey = ContentSetup.ModId + "/Lit";

        /// <summary>Ticks of light in one Dry Cell. Set from the settings.</summary>
        public static int Capacity { get; set; } = ChargeModel.CapacityTicks(4);

        public static bool IsFlashlight(Item? item) => item?.ItemId == ContentSetup.FlashlightId;

        /// <summary>
        /// The one flashlight that works: the first in the backpack. Any others are inert spares, so owning several can never
        /// store extra cells or give a second light.
        /// </summary>
        public static Tool? ActiveIn(Farmer? player)
        {
            var items = player?.Items;
            if (items is null)
                return null;
            for (var i = 0; i < items.Count; i++)
            {
                if (items[i] is Tool tool && IsFlashlight(tool))
                    return tool;
            }
            return null;
        }

        public static bool IsActive(Item? item) => item is Tool tool && ReferenceEquals(ActiveIn(Game1.player), tool);

        public static bool IsLit(Item? item) => item is not null && item.modData.ContainsKey(LitKey);

        public static void SetLit(Item flashlight, bool lit)
        {
            if (lit)
                flashlight.modData[LitKey] = "1";
            else
                flashlight.modData.Remove(LitKey);
        }

        public static int ReadCharge(Item tool) =>
            tool.modData.TryGetValue(ChargeKey, out var text) && int.TryParse(text, out var charge) ? Math.Max(0, charge) : 0;

        public static void WriteCharge(Item tool, int charge) => tool.modData[ChargeKey] = charge.ToString();

        /// <summary>The Dry Cells loaded in the flashlight's slots.</summary>
        public static int CountCells(Tool flashlight)
        {
            var total = 0;
            for (var i = 0; i < flashlight.attachments.Count; i++)
            {
                var cell = flashlight.attachments[i];
                if (cell?.QualifiedItemId == ContentSetup.QualifiedBatteryId)
                    total += cell.Stack;
            }
            return total;
        }

        /// <summary>Uses up one loaded cell, emptying its slot.</summary>
        public static void UseCell(Tool flashlight)
        {
            for (var i = 0; i < flashlight.attachments.Count; i++)
            {
                var cell = flashlight.attachments[i];
                if (cell?.QualifiedItemId != ContentSetup.QualifiedBatteryId)
                    continue;
                cell.Stack--;
                if (cell.Stack <= 0)
                    flashlight.attachments[i] = null;
                return;
            }
        }

        /// <summary>How much of a full load is left, from 0 to 1.</summary>
        public static float Reserve(Tool flashlight) =>
            ChargeModel.ReserveFraction(ReadCharge(flashlight), CountCells(flashlight), flashlight.AttachmentSlotsCount, Capacity);
    }
}
