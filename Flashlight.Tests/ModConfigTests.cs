namespace Flashlight.Tests;

public class ModConfigTests
{
    [Fact]
    public void DefaultsAreTheBalancedValues()
    {
        var config = new ModConfig();
        Assert.Equal("F", config.ToggleKey);
        Assert.Equal(10, config.BeamLengthTiles);
        Assert.Equal(10, config.BatteryHours);
        Assert.True(config.UseBatteryPacks);
        Assert.Equal(12, config.BatteryPackHours);
        Assert.Equal(120, config.BatteryPackBeamPercent);
        Assert.True(config.ShowInHand);
        Assert.True(config.BeamShadows);
        Assert.Equal(600, config.FlashlightPrice);
        Assert.Equal(30, config.BatteryPrice);
        Assert.True(config.SoldAtCarpenter);
        Assert.False(config.SoldAtBlacksmith);
        Assert.True(config.SendMail);
        Assert.Equal(15, config.BatteryOutOfStockPercent);
        Assert.Equal(25, config.BatteryLowStockPercent);
        Assert.Equal(16, config.BatteryDailyStock);
        Assert.Equal(16, config.MaxBatteriesCarried);
        Assert.Equal(40, config.FlickerSecondsBetween);
    }

    [Fact]
    public void ClampKeepsValuesInRange()
    {
        var config = new ModConfig { BeamLengthTiles = 99, BatteryHours = 0, FlashlightPrice = -5, BatteryPrice = 1_000_000, ToggleKey = " " };
        config.Clamp();
        Assert.Equal(16, config.BeamLengthTiles);
        Assert.Equal(1, config.BatteryHours);
        Assert.Equal(0, config.FlashlightPrice);
        Assert.Equal(10_000, config.BatteryPrice);
        Assert.Equal("F", config.ToggleKey);
    }

    [Theory]
    [InlineData(0, 1, 50, 100)]
    [InlineData(4, 4, 120, 120)]
    [InlineData(500, 48, 900, 200)]
    public void BatteryPackSettingsAreKeptInRange(int hours, int expectedHours, int percent, int expectedPercent)
    {
        var config = new ModConfig { BatteryPackHours = hours, BatteryPackBeamPercent = percent };
        config.Clamp();
        Assert.Equal(expectedHours, config.BatteryPackHours);
        Assert.Equal(expectedPercent, config.BatteryPackBeamPercent);
    }

    [Fact]
    public void TheDailyStockCanNeverBeMoreThanAPlayerMayCarry()
    {
        var config = new ModConfig { MaxBatteriesCarried = 3, BatteryDailyStock = 50 };
        config.Clamp();
        Assert.Equal(3, config.BatteryDailyStock);
    }


    [Fact]
    public void TheCarryLimitIsKeptInRange()
    {
        var config = new ModConfig { MaxBatteriesCarried = 500 };
        config.Clamp();
        Assert.Equal(20, config.MaxBatteriesCarried);
        config.MaxBatteriesCarried = 0;
        config.Clamp();
        Assert.Equal(1, config.MaxBatteriesCarried);
    }

    [Theory]
    [InlineData(-3, 0)]
    [InlineData(0, 0)]
    [InlineData(1, 5)]
    [InlineData(40, 40)]
    [InlineData(5000, 600)]
    public void TheFlickerSettingIsZeroOrBetweenFiveAndSixHundred(int set, int expected)
    {
        var config = new ModConfig { FlickerSecondsBetween = set };
        config.Clamp();
        Assert.Equal(expected, config.FlickerSecondsBetween);
    }
}

public class ModConfigCopyTests
{
    [Fact]
    public void CopyFromCopiesEverySettingSoTheMenuResetCannotMissOne()
    {
        var changed = new ModConfig
        {
            ToggleKey = "G", BeamLengthTiles = 5, BatteryHours = 7, FlashlightPrice = 111, BatteryPrice = 22,
            SoldAtCarpenter = false, SoldAtBlacksmith = true, SendMail = false, BatteryDailyStock = 7, BatteryOutOfStockPercent = 33, BatteryLowStockPercent = 44,
            MaxBatteriesCarried = 9, FlickerSecondsBetween = 77, UseBatteryPacks = false, BatteryPackHours = 20, BatteryPackBeamPercent = 150, ShowInHand = false, BeamShadows = false, ConfigVersion = 1,
        };
        var target = new ModConfig();
        target.CopyFrom(changed);
        foreach (var property in typeof(ModConfig).GetProperties())
            Assert.Equal(property.GetValue(changed), property.GetValue(target));
    }

    [Fact]
    public void EverySettingIsDifferentFromItsDefaultInTheTestAbove()
    {
        // guards the test above: a new setting that it does not change would make the copy check pass for nothing
        var changed = new ModConfig
        {
            ToggleKey = "G", BeamLengthTiles = 5, BatteryHours = 7, FlashlightPrice = 111, BatteryPrice = 22,
            SoldAtCarpenter = false, SoldAtBlacksmith = true, SendMail = false, BatteryDailyStock = 7, BatteryOutOfStockPercent = 33, BatteryLowStockPercent = 44,
            MaxBatteriesCarried = 9, FlickerSecondsBetween = 77, UseBatteryPacks = false, BatteryPackHours = 20, BatteryPackBeamPercent = 150, ShowInHand = false, BeamShadows = false, ConfigVersion = 1,
        };
        var defaults = new ModConfig();
        foreach (var property in typeof(ModConfig).GetProperties())
            Assert.NotEqual(property.GetValue(defaults), property.GetValue(changed));
    }
}

public class ModConfigUpgradeTests
{
    [Fact]
    public void AFileFromBeforeVersionsMovesTheOldDefaultsToTheNewOnes()
    {
        var config = new ModConfig { ConfigVersion = 0, BeamLengthTiles = 7, BatteryHours = 4, BatteryPackHours = 6, BatteryPrice = 15 };
        Assert.True(config.Upgrade());
        Assert.Equal(10, config.BeamLengthTiles);
        Assert.Equal(10, config.BatteryHours);
        Assert.Equal(12, config.BatteryPackHours);
        Assert.Equal(30, config.BatteryPrice);
        Assert.Equal(ModConfig.CurrentVersion, config.ConfigVersion);
    }

    [Fact]
    public void AValueThePlayerChangedIsKept()
    {
        // 1.0.0 players kept 2 and 4, and someone who picked 8 tiles chose it
        var config = new ModConfig { ConfigVersion = 0, BeamLengthTiles = 8, BatteryHours = 2, BatteryPackHours = 4, BatteryPrice = 20 };
        config.Upgrade();
        Assert.Equal(8, config.BeamLengthTiles);
        Assert.Equal(2, config.BatteryHours);
        Assert.Equal(4, config.BatteryPackHours);
        Assert.Equal(20, config.BatteryPrice);
    }

    [Fact]
    public void ACurrentFileIsLeftAloneEvenWithOldDefaultValues()
    {
        // after the upgrade, 7 tiles is a choice and must not be moved again
        var config = new ModConfig { ConfigVersion = ModConfig.CurrentVersion, BeamLengthTiles = 7, BatteryHours = 4, BatteryPackHours = 6 };
        Assert.False(config.Upgrade());
        Assert.Equal(7, config.BeamLengthTiles);
        Assert.Equal(4, config.BatteryHours);
        Assert.Equal(6, config.BatteryPackHours);
    }

    [Fact]
    public void TheVersionIsNotReportedAsAChangedSetting()
    {
        var config = new ModConfig { ConfigVersion = ModConfig.CurrentVersion };
        Assert.Empty(ReportText.ChangedSettings(config));
    }
}
