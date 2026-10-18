namespace Flashlight.Tests;

public class UsageStatsTests
{
    [Fact]
    public void ADayWithLightCountsAsANightWhenItEnds()
    {
        var stats = new UsageStats();
        stats.Tick();
        stats.Tick();
        stats.DayEnded();
        Assert.Equal(1, stats.NightsUsed);
        Assert.Equal(0, stats.LitToday);
        Assert.Equal(2, stats.TicksLit);
    }

    [Fact]
    public void ADayWithNoLightIsNotANight()
    {
        var stats = new UsageStats();
        stats.DayEnded();
        Assert.Equal(0, stats.NightsUsed);
    }

    [Fact]
    public void HoursAreSixTicksEach()
    {
        var stats = new UsageStats { TicksLit = 12, NightsUsed = 2 };
        Assert.Equal(2.0, stats.HoursLit, 3);
        Assert.Equal(1.0, stats.HoursPerNight, 3);
    }

    [Fact]
    public void AveragesAreZeroBeforeAnyNight()
    {
        var stats = new UsageStats { TicksLit = 5 };
        Assert.Equal(0, stats.HoursPerNight);
        Assert.Equal(0, stats.DryCellsPerNight);
    }

    [Fact]
    public void CellsAreCountedByKind()
    {
        var stats = new UsageStats();
        stats.CellUsed(CellKind.Dry);
        stats.CellUsed(CellKind.Dry);
        stats.CellUsed(CellKind.Pack);
        stats.CellUsed(CellKind.None);
        Assert.Equal(2, stats.DryCellsUsed);
        Assert.Equal(1, stats.PacksUsed);
    }
}

public class ReportTextTests
{
    [Fact]
    public void DefaultSettingsAreReportedAsDefaults()
    {
        Assert.Empty(ReportText.ChangedSettings(new ModConfig()));
        var text = ReportText.Build("1.0.0", "SMAPI 4.5.2", new ModConfig(), new UsageStats(), "none");
        Assert.Contains("Settings: all defaults", text);
    }

    [Fact]
    public void OnlyChangedSettingsAreListed()
    {
        var config = new ModConfig { BatteryHours = 6, SoldAtCarpenter = false };
        var changed = ReportText.ChangedSettings(config);
        Assert.Equal(new[] { "BatteryHours=6", "SoldAtCarpenter=False" }, changed.OrderBy(x => x, StringComparer.Ordinal));
    }

    [Fact]
    public void TheReportHasTheVersionTheUseAndTheFlashlight()
    {
        var stats = new UsageStats { NightsUsed = 4, TicksLit = 48, DryCellsUsed = 6, PacksUsed = 1, RanOut = 2 };
        var text = ReportText.Build("1.0.0", "SMAPI 4.5.2, Stardew Valley 1.6.15, Windows", new ModConfig(), stats, "charge 10, slots Dry, -");
        Assert.Contains("Flashlight 1.0.0 | SMAPI 4.5.2", text);
        Assert.Contains("4 nights, 8 hours lit (2 a night), 6 Dry Cells (1.5 a night), 1 Battery Packs, ran out 2 times", text);
        Assert.Contains("Flashlight now: charge 10, slots Dry, -", text);
    }

    [Fact]
    public void NumbersAreWrittenWithAFullStopWhateverTheLanguage()
    {
        var old = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var stats = new UsageStats { NightsUsed = 2, TicksLit = 9, DryCellsUsed = 3 };
            var text = ReportText.Build("1", "env", new ModConfig(), stats, "x");
            Assert.Contains("1.5 hours lit", text);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = old;
        }
    }
}
