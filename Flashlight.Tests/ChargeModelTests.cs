namespace Flashlight.Tests;

public class ChargeModelTests
{
    private const int Capacity = 24; // 4 hours

    [Fact]
    public void CapacityIsSixTicksPerHour() => Assert.Equal(24, ChargeModel.CapacityTicks(4));

    [Fact]
    public void CannotTurnOnWithNoChargeAndNoBattery()
    {
        var step = ChargeModel.TurnOn(0, 0, Capacity);
        Assert.False(step.LightOn);
        Assert.False(step.BatteryUsed);
    }

    [Fact]
    public void TurningOnWithNoChargeTakesABattery()
    {
        var step = ChargeModel.TurnOn(0, 2, Capacity);
        Assert.True(step.LightOn);
        Assert.True(step.BatteryUsed);
        Assert.Equal(Capacity, step.Charge);
        Assert.Equal(1, step.BatteriesLeft);
    }

    [Fact]
    public void TurningOnWithChargeKeepsTheBattery()
    {
        var step = ChargeModel.TurnOn(10, 2, Capacity);
        Assert.True(step.LightOn);
        Assert.False(step.BatteryUsed);
        Assert.Equal(10, step.Charge);
        Assert.Equal(2, step.BatteriesLeft);
    }

    [Fact]
    public void EachTickSpendsOneUnit()
    {
        var step = ChargeModel.Tick(10, 0, Capacity);
        Assert.Equal(9, step.Charge);
        Assert.True(step.LightOn);
    }

    [Fact]
    public void ARunOutChargeTakesTheNextBatteryByItself()
    {
        var step = ChargeModel.Tick(0, 1, Capacity);
        Assert.True(step.BatteryUsed);
        Assert.Equal(Capacity - 1, step.Charge);
        Assert.Equal(0, step.BatteriesLeft);
        Assert.True(step.LightOn);
    }

    [Fact]
    public void WithNothingLeftTheLightGoesOut()
    {
        var step = ChargeModel.Tick(0, 0, Capacity);
        Assert.False(step.LightOn);
    }

    [Fact]
    public void OneBatteryLastsExactlyItsCapacityInTicks() => Assert.Equal(Capacity, TicksOfLight(charge: Capacity, batteries: 0));

    [Fact]
    public void TwoBatteriesInTheBackpackLastTwiceAsLong() => Assert.Equal(Capacity * 2, TicksOfLight(charge: 0, batteries: 2));

    [Fact]
    public void LowIsOnlyReportedWhenThereIsNoSpareBattery()
    {
        Assert.False(ChargeModel.Tick(2, 1, Capacity).Low);
        Assert.True(ChargeModel.Tick(2, 0, Capacity).Low);
    }

    /// <summary>Runs the model until the light goes out and counts the ticks that had light.</summary>
    private static int TicksOfLight(int charge, int batteries)
    {
        var lit = 0;
        for (var i = 0; i < Capacity * 10; i++)
        {
            var step = ChargeModel.Tick(charge, batteries, Capacity);
            if (!step.LightOn)
                break;
            lit++;
            charge = step.Charge;
            batteries = step.BatteriesLeft;
        }
        return lit;
    }
}
