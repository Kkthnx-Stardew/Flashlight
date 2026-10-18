namespace Flashlight
{
    /// <summary>
    /// The battery rules, kept apart from the game so they can be tested. Time moves in ticks of 10 in-game minutes, and one
    /// Dry Cell holds <see cref="CapacityTicks"/> ticks of light. The flashlight keeps the charge of the cell in use and has
    /// slots for spare cells; when the charge runs out the next loaded cell is used by itself.
    /// </summary>
    public static class ChargeModel
    {
        public const int MinutesPerTick = 10;
        public const int TicksPerHour = 60 / MinutesPerTick;

        public static int CapacityTicks(int batteryHours) => batteryHours * TicksPerHour;

        /// <summary>The result of one step of the model.</summary>
        /// <param name="Charge">Ticks of light left in the cell in use afterwards.</param>
        /// <param name="CellsLeft">Loaded cells left afterwards.</param>
        /// <param name="CellUsed">Whether a loaded cell was used to carry on.</param>
        /// <param name="LightOn">Whether the flashlight can shine after this step.</param>
        public readonly record struct Step(int Charge, int CellsLeft, bool CellUsed, bool LightOn);

        /// <summary>Tries to switch the flashlight on. It needs some charge, or a loaded cell to take one from.</summary>
        public static Step TurnOn(int charge, int cells, int capacity)
        {
            if (charge <= 0)
            {
                if (cells <= 0)
                    return new Step(0, cells, false, false);
                return new Step(capacity, cells - 1, true, true);
            }
            return new Step(charge, cells, false, true);
        }

        /// <summary>Spends one tick of light while the flashlight is on.</summary>
        public static Step Tick(int charge, int cells, int capacity)
        {
            var used = false;
            if (charge <= 0)
            {
                if (cells <= 0)
                    return new Step(0, cells, false, false);
                cells--;
                charge = capacity;
                used = true;
            }
            return new Step(charge - 1, cells, used, true);
        }

        /// <summary>
        /// How much of a full load is left, from 0 to 1. A full load is every slot holding a fresh cell, so the figure can
        /// only reach 1 by loading all the slots, and loading spare cells while one is part used is capped at 1.
        /// </summary>
        public static float ReserveFraction(int charge, int cells, int slots, int capacity)
        {
            var full = (long)slots * capacity;
            if (full <= 0)
                return 0f;
            var left = (long)Math.Max(0, charge) + (long)Math.Max(0, cells) * capacity;
            return Math.Clamp((float)left / full, 0f, 1f);
        }
    }
}
