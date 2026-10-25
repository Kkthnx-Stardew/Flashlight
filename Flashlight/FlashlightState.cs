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

        /// <summary>Present in a flashlight's mod data while the cell in use is a Battery Pack. Without it the cell is a Dry Cell, which is also what a flashlight saved by an older version has.</summary>
        public const string PackKey = ContentSetup.ModId + "/Pack";

        /// <summary>The game's Battery Pack.</summary>
        public const string QualifiedPackId = "(O)787";

        /// <summary>Ticks of light in each kind of cell. Set from the settings.</summary>
        public static CellCapacities Capacities { get; set; } = new(ChargeModel.CapacityTicks(4), ChargeModel.CapacityTicks(6));

        /// <summary>Whether a Battery Pack can be loaded. A pack that is already loaded keeps working if this is turned off.</summary>
        public static bool PacksAllowed { get; set; } = true;

        /// <summary>The most slots the tool reads, so the reading needs no allocation.</summary>
        public const int MaxSlots = 8;

        /// <summary>A flashlight always has two battery slots, like the bait slot of a fishing rod and no more.</summary>
        public const int Slots = 2;

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

        /// <summary>The kind of the cell in use. None when there is no charge.</summary>
        public static CellKind ReadInUse(Item tool)
        {
            if (ReadCharge(tool) <= 0)
                return CellKind.None;
            return tool.modData.ContainsKey(PackKey) ? CellKind.Pack : CellKind.Dry;
        }

        /// <summary>The kind of cell an item is, or None when it is not one the flashlight uses.</summary>
        public static CellKind KindOf(Item? item) => item?.QualifiedItemId switch
        {
            ContentSetup.QualifiedBatteryId => CellKind.Dry,
            QualifiedPackId => CellKind.Pack,
            _ => CellKind.None,
        };

        /// <summary>Whether a new cell of this item can be loaded into a slot.</summary>
        public static bool CanLoad(Item? item) => KindOf(item) switch
        {
            CellKind.Dry => true,
            CellKind.Pack => PacksAllowed,
            _ => false,
        };

        /// <summary>Reads the cell in each slot into <paramref name="into"/> and returns how many slots were read.</summary>
        public static int ReadSlots(Tool flashlight, Span<CellKind> into)
        {
            var count = Math.Min(flashlight.attachments.Count, into.Length);
            for (var i = 0; i < count; i++)
                into[i] = KindOf(flashlight.attachments[i]);
            return count;
        }

        /// <summary>Tries to switch the flashlight on. Nothing is changed: <see cref="Commit"/> applies the result.</summary>
        public static ChargeModel.Step TurnOn(Tool flashlight)
        {
            Span<CellKind> slots = stackalloc CellKind[MaxSlots];
            var count = ReadSlots(flashlight, slots);
            return ChargeModel.TurnOn(ReadCharge(flashlight), ReadInUse(flashlight), slots[..count], Capacities);
        }

        /// <summary>Spends one tick of light. Nothing is changed: <see cref="Commit"/> applies the result.</summary>
        public static ChargeModel.Step Tick(Tool flashlight)
        {
            Span<CellKind> slots = stackalloc CellKind[MaxSlots];
            var count = ReadSlots(flashlight, slots);
            return ChargeModel.Tick(ReadCharge(flashlight), ReadInUse(flashlight), slots[..count], Capacities);
        }

        /// <summary>Writes the result of a step: empties both slots when a pair was taken, and keeps the charge and its kind.</summary>
        public static void Commit(Tool flashlight, ChargeModel.Step step)
        {
            if (step.PairUsed)
            {
                for (var i = 0; i < flashlight.attachments.Count; i++)
                    flashlight.attachments[i] = null;
            }
            flashlight.modData[ChargeKey] = step.Charge.ToString();
            if (step.InUse == CellKind.Pack && step.Charge > 0)
                flashlight.modData[PackKey] = "1";
            else
                flashlight.modData.Remove(PackKey);
        }

        /// <summary>
        /// Whether this item may go into this slot: it is a cell the settings allow, and the other slot is empty or holds the same
        /// kind, because the two batteries work as a pair and cannot be mixed.
        /// </summary>
        public static bool CanLoadInto(Tool flashlight, Item? item, int slot)
        {
            if (!CanLoad(item))
                return false;
            var other = slot == 0 ? 1 : 0;
            var otherKind = other < flashlight.attachments.Count ? KindOf(flashlight.attachments[other]) : CellKind.None;
            return ChargeModel.CanLoad(KindOf(item), otherKind);
        }

        /// <summary>How much of a full load is left, from 0 to 1.</summary>
        public static float Reserve(Tool flashlight)
        {
            Span<CellKind> slots = stackalloc CellKind[MaxSlots];
            var count = ReadSlots(flashlight, slots);
            return ChargeModel.ReserveFraction(ReadCharge(flashlight), ReadInUse(flashlight), slots[..count], Capacities);
        }

        /// <summary>How much of a Battery Pack's extra brightness the beam has: 1 while a pack is in use, otherwise 0.</summary>
        public static float Boost(Tool flashlight) => ChargeModel.Boost(ReadInUse(flashlight), ReadCharge(flashlight));
    }
}
