namespace Flashlight.Tests;

public class ChargeModelTests
{
    private const int Capacity = 24; // 4 hours

    [Fact]
    public void CapacityIsSixTicksPerHour() => Assert.Equal(24, ChargeModel.CapacityTicks(4));

    [Fact]
    public void CannotTurnOnWithNoChargeAndNoCell()
    {
        var step = ChargeModel.TurnOn(0, 0, Capacity);
        Assert.False(step.LightOn);
        Assert.False(step.CellUsed);
    }

    [Fact]
    public void TurningOnWithNoChargeTakesALoadedCell()
    {
        var step = ChargeModel.TurnOn(0, 2, Capacity);
        Assert.True(step.LightOn);
        Assert.True(step.CellUsed);
        Assert.Equal(Capacity, step.Charge);
        Assert.Equal(1, step.CellsLeft);
    }

    [Fact]
    public void TurningOnWithChargeKeepsTheCells()
    {
        var step = ChargeModel.TurnOn(10, 2, Capacity);
        Assert.True(step.LightOn);
        Assert.False(step.CellUsed);
        Assert.Equal(10, step.Charge);
        Assert.Equal(2, step.CellsLeft);
    }

    [Fact]
    public void EachTickSpendsOneUnit()
    {
        var step = ChargeModel.Tick(10, 0, Capacity);
        Assert.Equal(9, step.Charge);
        Assert.True(step.LightOn);
    }

    [Fact]
    public void ARunOutChargeTakesTheNextLoadedCellByItself()
    {
        var step = ChargeModel.Tick(0, 1, Capacity);
        Assert.True(step.CellUsed);
        Assert.Equal(Capacity - 1, step.Charge);
        Assert.Equal(0, step.CellsLeft);
        Assert.True(step.LightOn);
    }

    [Fact]
    public void WithNothingLeftTheLightGoesOut()
    {
        var step = ChargeModel.Tick(0, 0, Capacity);
        Assert.False(step.LightOn);
    }

    [Fact]
    public void OneCellLastsExactlyItsCapacityInTicks() => Assert.Equal(Capacity, TicksOfLight(charge: Capacity, cells: 0));

    [Fact]
    public void TwoLoadedCellsLastTwiceAsLong() => Assert.Equal(Capacity * 2, TicksOfLight(charge: 0, cells: 2));

    [Fact]
    public void TheFractionIsOneWhenEverySlotHoldsAFreshCell() =>
        Assert.Equal(1f, ChargeModel.ReserveFraction(charge: 0, cells: 2, slots: 2, Capacity));

    [Fact]
    public void TheFractionFallsAsTheChargeIsSpent()
    {
        Assert.Equal(0.75f, ChargeModel.ReserveFraction(charge: 12, cells: 1, slots: 2, Capacity), 3);
        Assert.Equal(0.5f, ChargeModel.ReserveFraction(charge: 0, cells: 1, slots: 2, Capacity), 3);
        Assert.Equal(0f, ChargeModel.ReserveFraction(charge: 0, cells: 0, slots: 2, Capacity), 3);
    }

    [Fact]
    public void LoadingSpareCellsOnTopOfPartUsedChargeNeverShowsMoreThanFull() =>
        Assert.Equal(1f, ChargeModel.ReserveFraction(charge: 20, cells: 2, slots: 2, Capacity));

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(5, 1, 0)]
    public void NoSlotsOrNoCapacityGivesZero(int slots, int cells, int capacity) =>
        Assert.Equal(0f, ChargeModel.ReserveFraction(charge: 3, cells, slots, capacity));

    /// <summary>Runs the model until the light goes out and counts the ticks that had light.</summary>
    private static int TicksOfLight(int charge, int cells)
    {
        var lit = 0;
        for (var i = 0; i < Capacity * 10; i++)
        {
            var step = ChargeModel.Tick(charge, cells, Capacity);
            if (!step.LightOn)
                break;
            lit++;
            charge = step.Charge;
            cells = step.CellsLeft;
        }
        return lit;
    }
}
