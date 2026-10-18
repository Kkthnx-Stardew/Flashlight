namespace Flashlight
{
    /// <summary>The kinds of cell a flashlight slot can hold.</summary>
    public enum CellKind
    {
        None = 0,

        /// <summary>The mod's own Dry Cell.</summary>
        Dry = 1,

        /// <summary>The game's Battery Pack: a longer burn and a slightly brighter, longer beam.</summary>
        Pack = 2,
    }

    /// <summary>How many ticks of light a matched pair of each kind of cell gives.</summary>
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
    /// The battery rules, kept apart from the game so they can be tested. Time moves in ticks of 10 in-game minutes. The
    /// flashlight has two slots and only works with both filled by the same kind of cell, like a real one. A matched pair is
    /// used up together and gives <see cref="CapacityTicks"/> of light. The flashlight keeps the charge of the pair in use, and
    /// when that runs out it takes the next matched pair if there is one.
    /// </summary>
    public static class ChargeModel
    {
        public const int MinutesPerTick = 10;
        public const int TicksPerHour = 60 / MinutesPerTick;

        public static int CapacityTicks(int batteryHours) => batteryHours * TicksPerHour;

        /// <summary>The result of one step of the model.</summary>
        /// <param name="Charge">Ticks of light left in the pair in use afterwards.</param>
        /// <param name="InUse">The kind of the pair in use afterwards. None when the flashlight is flat.</param>
        /// <param name="PairUsed">Whether a loaded pair was taken out of the slots to carry on.</param>
        /// <param name="LightOn">Whether the flashlight can shine after this step.</param>
        public readonly record struct Step(int Charge, CellKind InUse, bool PairUsed, bool LightOn)
        {
            public bool CellUsed => PairUsed;
        }

        /// <summary>
        /// The kind of the loaded pair: both slots hold the same kind of cell. None when a slot is empty, the two kinds differ, or
        /// there are not two slots.
        /// </summary>
        public static CellKind PairKind(ReadOnlySpan<CellKind> slots)
        {
            if (slots.Length != 2 || slots[0] == CellKind.None || slots[0] != slots[1])
                return CellKind.None;
            return slots[0];
        }

        /// <summary>Whether a cell of this kind may be loaded into a slot, given what the other slot holds.</summary>
        public static bool CanLoad(CellKind kind, CellKind other) => kind != CellKind.None && (other == CellKind.None || other == kind);

        /// <summary>Tries to switch the flashlight on. It needs some charge, or a matched pair to take one from.</summary>
        public static Step TurnOn(int charge, CellKind inUse, ReadOnlySpan<CellKind> slots, CellCapacities caps)
        {
            if (charge > 0)
                return new Step(charge, Known(inUse), false, true);

            var pair = PairKind(slots);
            if (pair == CellKind.None)
                return new Step(0, CellKind.None, false, false);
            return new Step(caps.Of(pair), pair, true, true);
        }

        /// <summary>Spends one tick of light while the flashlight is on.</summary>
        public static Step Tick(int charge, CellKind inUse, ReadOnlySpan<CellKind> slots, CellCapacities caps)
        {
            var used = false;
            if (charge <= 0)
            {
                var pair = PairKind(slots);
                if (pair == CellKind.None)
                    return new Step(0, CellKind.None, false, false);
                inUse = pair;
                charge = caps.Of(pair);
                used = true;
            }
            return new Step(charge - 1, Known(inUse), used, true);
        }

        /// <summary>A charge from before the kinds existed has no kind recorded, and was always a Dry Cell.</summary>
        private static CellKind Known(CellKind kind) => kind == CellKind.None ? CellKind.Dry : kind;

        /// <summary>
        /// How much of a full load is left, from 0 to 1. A full load is one fresh pair of the best kind in the flashlight (the Dry
        /// Cell at least), so a Dry Cell pair reads as full and a Battery Pack pair is measured against packs. A pair that is
        /// loaded while another is part used is capped at 1.
        /// </summary>
        public static float ReserveFraction(int charge, CellKind inUse, ReadOnlySpan<CellKind> slots, CellCapacities caps)
        {
            var pair = PairKind(slots);
            var best = caps.Dry;
            if (charge > 0)
                best = Math.Max(best, caps.Of(Known(inUse)));
            best = Math.Max(best, caps.Of(pair));
            if (best <= 0)
                return 0f;
            long left = Math.Max(0, charge) + caps.Of(pair);
            return Math.Clamp((float)left / best, 0f, 1f);
        }

        /// <summary>
        /// The share of the Battery Pack's extra brightness to give the beam, from 0 (Dry Cells) to 1 (Battery Packs).
        /// A flat flashlight that is not shining has none.
        /// </summary>
        public static float Boost(CellKind inUse, int charge) => charge > 0 && inUse == CellKind.Pack ? 1f : 0f;
    }
}
