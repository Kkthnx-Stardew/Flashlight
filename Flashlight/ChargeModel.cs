namespace Flashlight
{
    /// <summary>The kinds of cell a flashlight slot can hold.</summary>
    public enum CellKind
    {
        None = 0,

        /// <summary>The mod's own Dry Cell.</summary>
        Dry = 1,

        /// <summary>The game's Battery Pack: a longer burn and a brighter, longer beam.</summary>
        Pack = 2,
    }

    /// <summary>How many ticks of light each kind of cell holds.</summary>
    public readonly record struct CellCapacities(int Dry, int Pack)
    {
        public int Of(CellKind kind) => kind switch
        {
            CellKind.Dry => Dry,
            CellKind.Pack => Pack,
            _ => 0,
        };
    }

    /// <summary>
    /// The battery rules, kept apart from the game so they can be tested. Time moves in ticks of 10 in-game minutes, and each
    /// kind of cell holds a number of ticks of light (see <see cref="CapacityTicks"/>). The flashlight keeps the charge of the
    /// cell in use and has slots for spare cells. When the charge runs out the next loaded cell, in slot order, is used by itself.
    /// </summary>
    public static class ChargeModel
    {
        public const int MinutesPerTick = 10;
        public const int TicksPerHour = 60 / MinutesPerTick;

        public static int CapacityTicks(int batteryHours) => batteryHours * TicksPerHour;

        /// <summary>The result of one step of the model.</summary>
        /// <param name="Charge">Ticks of light left in the cell in use afterwards.</param>
        /// <param name="InUse">The kind of the cell in use afterwards. None when the flashlight is flat.</param>
        /// <param name="SlotUsed">The slot a cell was taken from to carry on, or -1 when none was.</param>
        /// <param name="LightOn">Whether the flashlight can shine after this step.</param>
        public readonly record struct Step(int Charge, CellKind InUse, int SlotUsed, bool LightOn)
        {
            public bool CellUsed => SlotUsed >= 0;
        }

        /// <summary>The first slot that holds a cell, or -1.</summary>
        public static int FirstLoaded(ReadOnlySpan<CellKind> slots)
        {
            for (var i = 0; i < slots.Length; i++)
            {
                if (slots[i] != CellKind.None)
                    return i;
            }
            return -1;
        }

        /// <summary>Tries to switch the flashlight on. It needs some charge, or a loaded cell to take one from.</summary>
        public static Step TurnOn(int charge, CellKind inUse, ReadOnlySpan<CellKind> slots, CellCapacities caps)
        {
            if (charge > 0)
                return new Step(charge, Known(inUse), -1, true);

            var slot = FirstLoaded(slots);
            if (slot < 0)
                return new Step(0, CellKind.None, -1, false);
            return new Step(caps.Of(slots[slot]), slots[slot], slot, true);
        }

        /// <summary>Spends one tick of light while the flashlight is on.</summary>
        public static Step Tick(int charge, CellKind inUse, ReadOnlySpan<CellKind> slots, CellCapacities caps)
        {
            var slot = -1;
            if (charge <= 0)
            {
                slot = FirstLoaded(slots);
                if (slot < 0)
                    return new Step(0, CellKind.None, -1, false);
                inUse = slots[slot];
                charge = caps.Of(inUse);
            }
            return new Step(charge - 1, Known(inUse), slot, true);
        }

        /// <summary>A charge from before the kinds existed has no kind recorded, and was always a Dry Cell.</summary>
        private static CellKind Known(CellKind kind) => kind == CellKind.None ? CellKind.Dry : kind;

        /// <summary>
        /// How much of a full load is left, from 0 to 1. A full load is every slot holding a fresh cell of the best kind that is
        /// in the flashlight (the Dry Cell at least), so a flashlight of Dry Cells reads as full when every slot has one, and a
        /// mix that includes a Battery Pack is measured against a load of packs. Loading spare cells while one is part used is capped at 1.
        /// </summary>
        public static float ReserveFraction(int charge, CellKind inUse, ReadOnlySpan<CellKind> slots, int slotCount, CellCapacities caps)
        {
            var best = caps.Dry;
            if (charge > 0)
                best = Math.Max(best, caps.Of(Known(inUse)));
            long left = Math.Max(0, charge);
            for (var i = 0; i < slots.Length; i++)
            {
                best = Math.Max(best, caps.Of(slots[i]));
                left += caps.Of(slots[i]);
            }

            var full = (long)slotCount * best;
            if (full <= 0)
                return 0f;
            return Math.Clamp((float)left / full, 0f, 1f);
        }

        /// <summary>
        /// The share of the Battery Pack's extra brightness to give the beam, from 0 (a Dry Cell) to 1 (a Battery Pack).
        /// A flat flashlight that is not shining has none.
        /// </summary>
        public static float Boost(CellKind inUse, int charge) => charge > 0 && inUse == CellKind.Pack ? 1f : 0f;
    }
}
