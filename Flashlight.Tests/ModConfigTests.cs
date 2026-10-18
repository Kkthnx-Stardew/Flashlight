namespace Flashlight.Tests;

public class ModConfigTests
{
    [Fact]
    public void DefaultsAreTheBalancedValues()
    {
        var config = new ModConfig();
        Assert.Equal("F", config.ToggleKey);
        Assert.Equal(7, config.BeamLengthTiles);
        Assert.Equal(4, config.BatteryHours);
        Assert.Equal(600, config.FlashlightPrice);
        Assert.Equal(60, config.BatteryPrice);
        Assert.True(config.SoldAtCarpenter);
        Assert.Equal(2, config.BatterySlots);
        Assert.Equal(15, config.BatteryOutOfStockPercent);
        Assert.Equal(25, config.BatteryLowStockPercent);
        Assert.Equal(6, config.BatteryDailyStock);
        Assert.Equal(6, config.MaxBatteriesCarried);
        Assert.Equal(40, config.FlickerSecondsBetween);
    }

    [Fact]
    public void ClampKeepsValuesInRange()
    {
        var config = new ModConfig { BeamLengthTiles = 99, BatteryHours = 0, FlashlightPrice = -5, BatteryPrice = 1_000_000, ToggleKey = " " };
        config.Clamp();
        Assert.Equal(12, config.BeamLengthTiles);
        Assert.Equal(1, config.BatteryHours);
        Assert.Equal(0, config.FlashlightPrice);
        Assert.Equal(10_000, config.BatteryPrice);
        Assert.Equal("F", config.ToggleKey);
    }

    [Fact]
    public void TheDailyStockCanNeverBeMoreThanAPlayerMayCarry()
    {
        var config = new ModConfig { MaxBatteriesCarried = 3, BatteryDailyStock = 50 };
        config.Clamp();
        Assert.Equal(3, config.BatteryDailyStock);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 2)]
    [InlineData(9, 4)]
    public void TheNumberOfBatterySlotsIsKeptInRange(int set, int expected)
    {
        var config = new ModConfig { BatterySlots = set };
        config.Clamp();
        Assert.Equal(expected, config.BatterySlots);
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
