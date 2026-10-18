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
}
