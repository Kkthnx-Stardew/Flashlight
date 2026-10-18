namespace Flashlight
{
    /// <summary>
    /// The battery rules, kept apart from the game so they can be tested. Time moves in ticks of 10 in-game minutes, and one
    /// battery holds <see cref="CapacityTicks"/> ticks of light. When the charge runs out the next battery from the backpack
    /// is used by itself.
    /// </summary>
    public static class ChargeModel
    {
        public const int MinutesPerTick = 10;
        public const int TicksPerHour = 60 / MinutesPerTick;

        /// <summary>The beam flickers when this many ticks or fewer are left.</summary>
        public const int LowTicks = 1;

        public static int CapacityTicks(int batteryHours) => batteryHours * TicksPerHour;

        /// <summary>The result of one step of the model.</summary>
        /// <param name="Charge">Ticks of light left in the flashlight afterwards.</param>
        /// <param name="BatteriesLeft">Batteries left in the backpack afterwards.</param>
        /// <param name="BatteryUsed">Whether a battery was taken from the backpack to carry on.</param>
        /// <param name="LightOn">Whether the flashlight can shine after this step.</param>
        /// <param name="Low">Whether the charge is nearly gone.</param>
        public readonly record struct Step(int Charge, int BatteriesLeft, bool BatteryUsed, bool LightOn, bool Low);

        /// <summary>Tries to switch the flashlight on. It needs some charge, or a battery to take one from.</summary>
        public static Step TurnOn(int charge, int batteries, int capacity)
        {
            if (charge <= 0)
            {
                if (batteries <= 0)
                    return new Step(0, batteries, false, false, false);
                return new Step(capacity, batteries - 1, true, true, capacity <= LowTicks);
            }
            return new Step(charge, batteries, false, true, charge <= LowTicks);
        }

        /// <summary>Spends one tick of light while the flashlight is on.</summary>
        public static Step Tick(int charge, int batteries, int capacity)
        {
            var used = false;
            if (charge <= 0)
            {
                if (batteries <= 0)
                    return new Step(0, batteries, false, false, false);
                batteries--;
                charge = capacity;
                used = true;
            }
            charge--;
            if (charge <= 0 && batteries <= 0)
                return new Step(0, batteries, used, true, true); // this tick was the last, the next one finds it empty
            return new Step(charge, batteries, used, true, charge <= LowTicks && batteries <= 0);
        }
    }
}
