namespace Flashlight
{
    /// <summary>
    /// A small tally of how the flashlight is used, kept on the player's own computer and never sent anywhere. It exists so that
    /// someone giving feedback on the balance can paste real numbers (see <see cref="ReportText"/>) instead of a feeling.
    /// This file has no game dependencies so the tests can compile it.
    /// </summary>
    public class UsageStats
    {
        /// <summary>Days the flashlight was on for at least one tick of dark time.</summary>
        public int NightsUsed { get; set; }

        /// <summary>Ticks of light spent, ten in-game minutes each.</summary>
        public int TicksLit { get; set; }

        public int DryCellsUsed { get; set; }

        public int PacksUsed { get; set; }

        /// <summary>How many times the flashlight went dark because nothing was left in it.</summary>
        public int RanOut { get; set; }

        /// <summary>Ticks of light spent today. Counted as a night when the day ends.</summary>
        public int LitToday { get; set; }

        public void Tick()
        {
            TicksLit++;
            LitToday++;
        }

        /// <summary>A matched pair of cells was used up, two of the kind.</summary>
        public void PairUsed(CellKind kind)
        {
            if (kind == CellKind.Dry)
                DryCellsUsed += 2;
            else if (kind == CellKind.Pack)
                PacksUsed += 2;
        }

        public void DayEnded()
        {
            if (LitToday > 0)
                NightsUsed++;
            LitToday = 0;
        }

        public double HoursLit => TicksLit * ChargeModel.MinutesPerTick / 60.0;

        public double HoursPerNight => NightsUsed == 0 ? 0 : HoursLit / NightsUsed;

        public double DryCellsPerNight => NightsUsed == 0 ? 0 : DryCellsUsed / (double)NightsUsed;
    }
}
