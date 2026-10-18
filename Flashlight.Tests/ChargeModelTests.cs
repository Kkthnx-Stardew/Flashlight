namespace Flashlight.Tests;

public class ChargeModelTests
{
    private const int Dry = 24;   // 4 hours
    private const int Pack = 72;  // 12 hours
    private static readonly CellCapacities Caps = new(Dry, Pack);

    private static CellKind[] Slots(params CellKind[] kinds) => kinds;

    [Fact]
    public void CapacityIsSixTicksPerHour() => Assert.Equal(24, ChargeModel.CapacityTicks(4));

    [Fact]
    public void CannotTurnOnWithNoChargeAndNoCell()
    {
        var step = ChargeModel.TurnOn(0, CellKind.None, Slots(CellKind.None, CellKind.None), Caps);
        Assert.False(step.LightOn);
        Assert.False(step.CellUsed);
    }

    [Fact]
    public void TurningOnWithNoChargeTakesTheFirstLoadedCell()
    {
        var step = ChargeModel.TurnOn(0, CellKind.None, Slots(CellKind.None, CellKind.Dry), Caps);
        Assert.True(step.LightOn);
        Assert.Equal(1, step.SlotUsed);
        Assert.Equal(Dry, step.Charge);
        Assert.Equal(CellKind.Dry, step.InUse);
    }

    [Fact]
    public void TurningOnWithChargeKeepsTheCells()
    {
        var step = ChargeModel.TurnOn(10, CellKind.Pack, Slots(CellKind.Dry, CellKind.Dry), Caps);
        Assert.True(step.LightOn);
        Assert.False(step.CellUsed);
        Assert.Equal(10, step.Charge);
        Assert.Equal(CellKind.Pack, step.InUse);
    }

    [Fact]
    public void AChargeWithNoKindRecordedIsADryCell()
    {
        // a flashlight saved before Battery Packs existed
        var step = ChargeModel.Tick(10, CellKind.None, Slots(CellKind.None), Caps);
        Assert.Equal(CellKind.Dry, step.InUse);
        Assert.Equal(9, step.Charge);
    }

    [Fact]
    public void EachTickSpendsOneUnit()
    {
        var step = ChargeModel.Tick(10, CellKind.Dry, Slots(CellKind.None), Caps);
        Assert.Equal(9, step.Charge);
        Assert.True(step.LightOn);
        Assert.False(step.CellUsed);
    }

    [Fact]
    public void ARunOutChargeTakesTheNextLoadedCellByItself()
    {
        var step = ChargeModel.Tick(0, CellKind.Dry, Slots(CellKind.Pack), Caps);
        Assert.Equal(0, step.SlotUsed);
        Assert.Equal(Pack - 1, step.Charge);
        Assert.Equal(CellKind.Pack, step.InUse);
        Assert.True(step.LightOn);
    }

    [Fact]
    public void WithNothingLeftTheLightGoesOut()
    {
        var step = ChargeModel.Tick(0, CellKind.Dry, Slots(CellKind.None, CellKind.None), Caps);
        Assert.False(step.LightOn);
        Assert.Equal(CellKind.None, step.InUse);
    }

    [Fact]
    public void OneDryCellLastsExactlyItsCapacityInTicks() => Assert.Equal(Dry, TicksOfLight(0, CellKind.None, Slots(CellKind.Dry)));

    [Fact]
    public void OneBatteryPackLastsThreeTimesAsLongAsADryCell() => Assert.Equal(Dry * 3, TicksOfLight(0, CellKind.None, Slots(CellKind.Pack)));

    [Fact]
    public void AMixOfCellsLastsTheSumOfThem() => Assert.Equal(Dry + Pack, TicksOfLight(0, CellKind.None, Slots(CellKind.Dry, CellKind.Pack)));

    [Fact]
    public void CellsAreUsedInSlotOrder()
    {
        var slots = Slots(CellKind.Pack, CellKind.Dry);
        var first = ChargeModel.Tick(0, CellKind.None, slots, Caps);
        Assert.Equal(CellKind.Pack, first.InUse);
        Assert.Equal(0, first.SlotUsed);
    }

    [Fact]
    public void TheFractionIsOneWhenEverySlotHoldsAFreshDryCell() =>
        Assert.Equal(1f, ChargeModel.ReserveFraction(0, CellKind.None, Slots(CellKind.Dry, CellKind.Dry), 2, Caps));

    [Fact]
    public void TheFractionFallsAsTheChargeIsSpent()
    {
        Assert.Equal(0.75f, ChargeModel.ReserveFraction(12, CellKind.Dry, Slots(CellKind.Dry, CellKind.None), 2, Caps), 3);
        Assert.Equal(0.5f, ChargeModel.ReserveFraction(0, CellKind.None, Slots(CellKind.Dry, CellKind.None), 2, Caps), 3);
        Assert.Equal(0f, ChargeModel.ReserveFraction(0, CellKind.None, Slots(CellKind.None, CellKind.None), 2, Caps), 3);
    }

    [Fact]
    public void LoadingSpareCellsOnTopOfPartUsedChargeNeverShowsMoreThanFull() =>
        Assert.Equal(1f, ChargeModel.ReserveFraction(20, CellKind.Dry, Slots(CellKind.Dry, CellKind.Dry), 2, Caps));

    [Fact]
    public void AFlashlightOfBatteryPacksIsMeasuredAgainstPacks()
    {
        Assert.Equal(1f, ChargeModel.ReserveFraction(0, CellKind.None, Slots(CellKind.Pack, CellKind.Pack), 2, Caps));
        // one pack of two is half, not three halves of a Dry Cell load
        Assert.Equal(0.5f, ChargeModel.ReserveFraction(0, CellKind.None, Slots(CellKind.Pack, CellKind.None), 2, Caps), 3);
    }

    [Fact]
    public void ADryCellAlongsideAPackCountsForItsOwnShare()
    {
        // pack in use at half, a dry cell waiting: (36 + 24) of 144
        Assert.Equal(60f / 144f, ChargeModel.ReserveFraction(36, CellKind.Pack, Slots(CellKind.Dry, CellKind.None), 2, Caps), 3);
    }

    [Fact]
    public void ADryCellLoadStaysMeasuredAgainstDryCellsWhenThePackIsGone()
    {
        // the pack ran out and a dry cell took over: back to the plain scale
        Assert.Equal(0.5f, ChargeModel.ReserveFraction(24, CellKind.Dry, Slots(CellKind.None, CellKind.None), 2, Caps), 3);
    }

    [Theory]
    [InlineData(0, 24)]
    [InlineData(5, 0)]
    public void NoSlotsOrNoCapacityGivesZero(int slotCount, int dry) =>
        Assert.Equal(0f, ChargeModel.ReserveFraction(3, CellKind.Dry, Slots(CellKind.None), slotCount, new CellCapacities(dry, 0)));

    [Fact]
    public void OnlyAPackInUseBoostsTheBeam()
    {
        Assert.Equal(1f, ChargeModel.Boost(CellKind.Pack, 5));
        Assert.Equal(0f, ChargeModel.Boost(CellKind.Dry, 5));
        Assert.Equal(0f, ChargeModel.Boost(CellKind.Pack, 0));
        Assert.Equal(0f, ChargeModel.Boost(CellKind.None, 5));
    }

    /// <summary>Runs the model until the light goes out and counts the ticks that had light. Used cells are removed from the slots.</summary>
    private static int TicksOfLight(int charge, CellKind inUse, CellKind[] slots)
    {
        var lit = 0;
        var step = ChargeModel.TurnOn(charge, inUse, slots, Caps);
        if (!step.LightOn)
            return 0;
        Apply(step, slots);
        for (var i = 0; i < 100_000; i++)
        {
            step = ChargeModel.Tick(step.Charge, step.InUse, slots, Caps);
            if (!step.LightOn)
                break;
            Apply(step, slots);
            lit++;
        }
        return lit;
    }

    private static void Apply(ChargeModel.Step step, CellKind[] slots)
    {
        if (step.CellUsed)
            slots[step.SlotUsed] = CellKind.None;
    }
}
