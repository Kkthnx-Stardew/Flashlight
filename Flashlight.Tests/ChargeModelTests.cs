namespace Flashlight.Tests;

public class ChargeModelTests
{
    private const int Dry = 12;   // a pair of Dry Cells: 2 hours
    private const int Pack = 24;  // a pair of Battery Packs: 4 hours
    private static readonly CellCapacities Caps = new(Dry, Pack);

    private static CellKind[] Slots(CellKind a, CellKind b) => new[] { a, b };

    private static CellKind[] Empty() => Slots(CellKind.None, CellKind.None);

    [Fact]
    public void CapacityIsSixTicksPerHour() => Assert.Equal(12, ChargeModel.CapacityTicks(2));

    [Theory]
    [InlineData(CellKind.Dry, CellKind.Dry, CellKind.Dry)]
    [InlineData(CellKind.Pack, CellKind.Pack, CellKind.Pack)]
    [InlineData(CellKind.Dry, CellKind.Pack, CellKind.None)]
    [InlineData(CellKind.Pack, CellKind.Dry, CellKind.None)]
    [InlineData(CellKind.Dry, CellKind.None, CellKind.None)]
    [InlineData(CellKind.None, CellKind.Pack, CellKind.None)]
    [InlineData(CellKind.None, CellKind.None, CellKind.None)]
    public void OnlyTwoMatchingCellsMakeAPair(CellKind a, CellKind b, CellKind expected) =>
        Assert.Equal(expected, ChargeModel.PairKind(Slots(a, b)));

    [Fact]
    public void ThereMustBeExactlyTwoSlots()
    {
        Assert.Equal(CellKind.None, ChargeModel.PairKind(new[] { CellKind.Dry }));
        Assert.Equal(CellKind.None, ChargeModel.PairKind(new[] { CellKind.Dry, CellKind.Dry, CellKind.Dry }));
    }

    [Theory]
    [InlineData(CellKind.Dry, CellKind.None, true)]
    [InlineData(CellKind.Dry, CellKind.Dry, true)]
    [InlineData(CellKind.Dry, CellKind.Pack, false)]
    [InlineData(CellKind.Pack, CellKind.Dry, false)]
    [InlineData(CellKind.Pack, CellKind.Pack, true)]
    [InlineData(CellKind.None, CellKind.None, false)]
    public void ACellCanOnlyJoinOneOfItsOwnKind(CellKind kind, CellKind other, bool allowed) =>
        Assert.Equal(allowed, ChargeModel.CanLoad(kind, other));

    [Fact]
    public void CannotTurnOnWithNoChargeAndNoPair()
    {
        Assert.False(ChargeModel.TurnOn(0, CellKind.None, Empty(), Caps).LightOn);
        Assert.False(ChargeModel.TurnOn(0, CellKind.None, Slots(CellKind.Dry, CellKind.None), Caps).LightOn);
        Assert.False(ChargeModel.TurnOn(0, CellKind.None, Slots(CellKind.Dry, CellKind.Pack), Caps).LightOn);
    }

    [Fact]
    public void TurningOnWithNoChargeTakesTheWholePair()
    {
        var step = ChargeModel.TurnOn(0, CellKind.None, Slots(CellKind.Dry, CellKind.Dry), Caps);
        Assert.True(step.LightOn);
        Assert.True(step.PairUsed);
        Assert.Equal(Dry, step.Charge);
        Assert.Equal(CellKind.Dry, step.InUse);
    }

    [Fact]
    public void TurningOnWithChargeKeepsTheLoadedPair()
    {
        var step = ChargeModel.TurnOn(5, CellKind.Pack, Slots(CellKind.Dry, CellKind.Dry), Caps);
        Assert.True(step.LightOn);
        Assert.False(step.PairUsed);
        Assert.Equal(5, step.Charge);
        Assert.Equal(CellKind.Pack, step.InUse);
    }

    [Fact]
    public void AChargeWithNoKindRecordedIsADryPair()
    {
        // a flashlight saved before Battery Packs existed
        var step = ChargeModel.Tick(10, CellKind.None, Empty(), Caps);
        Assert.Equal(CellKind.Dry, step.InUse);
        Assert.Equal(9, step.Charge);
    }

    [Fact]
    public void EachTickSpendsOneUnit()
    {
        var step = ChargeModel.Tick(10, CellKind.Dry, Empty(), Caps);
        Assert.Equal(9, step.Charge);
        Assert.True(step.LightOn);
        Assert.False(step.PairUsed);
    }

    [Fact]
    public void ARunOutChargeTakesTheNextMatchedPairByItself()
    {
        var step = ChargeModel.Tick(0, CellKind.Dry, Slots(CellKind.Pack, CellKind.Pack), Caps);
        Assert.True(step.PairUsed);
        Assert.Equal(Pack - 1, step.Charge);
        Assert.Equal(CellKind.Pack, step.InUse);
        Assert.True(step.LightOn);
    }

    [Fact]
    public void ALoneCellOrAMixedPairDoesNotKeepTheLightOn()
    {
        Assert.False(ChargeModel.Tick(0, CellKind.Dry, Slots(CellKind.Dry, CellKind.None), Caps).LightOn);
        Assert.False(ChargeModel.Tick(0, CellKind.Dry, Slots(CellKind.Dry, CellKind.Pack), Caps).LightOn);
    }

    [Fact]
    public void WithNothingLeftTheLightGoesOut()
    {
        var step = ChargeModel.Tick(0, CellKind.Dry, Empty(), Caps);
        Assert.False(step.LightOn);
        Assert.Equal(CellKind.None, step.InUse);
    }

    [Fact]
    public void AMatchedPairOfDryCellsLastsItsCapacityInTicks() => Assert.Equal(Dry, TicksOfLight(Slots(CellKind.Dry, CellKind.Dry)));

    [Fact]
    public void AMatchedPairOfPacksLastsTwoHoursLongerThanDryCells() => Assert.Equal(Dry + 12, TicksOfLight(Slots(CellKind.Pack, CellKind.Pack)));

    [Fact]
    public void ALoadedPairReadsAsFull() =>
        Assert.Equal(1f, ChargeModel.ReserveFraction(0, CellKind.None, Slots(CellKind.Dry, CellKind.Dry), Caps));

    [Fact]
    public void TheFractionFallsAsTheChargeIsSpent()
    {
        Assert.Equal(0.5f, ChargeModel.ReserveFraction(6, CellKind.Dry, Empty(), Caps), 3);
        Assert.Equal(0f, ChargeModel.ReserveFraction(0, CellKind.None, Empty(), Caps), 3);
    }

    [Fact]
    public void ALoneCellCountsForNothing() =>
        Assert.Equal(0f, ChargeModel.ReserveFraction(0, CellKind.None, Slots(CellKind.Dry, CellKind.None), Caps));

    [Fact]
    public void LoadingAPairWhileAnotherIsPartUsedNeverShowsMoreThanFull() =>
        Assert.Equal(1f, ChargeModel.ReserveFraction(9, CellKind.Dry, Slots(CellKind.Dry, CellKind.Dry), Caps));

    [Fact]
    public void APairOfPacksIsMeasuredAgainstPacks()
    {
        Assert.Equal(1f, ChargeModel.ReserveFraction(0, CellKind.None, Slots(CellKind.Pack, CellKind.Pack), Caps));
        Assert.Equal(0.5f, ChargeModel.ReserveFraction(12, CellKind.Pack, Empty(), Caps), 3);
    }

    [Fact]
    public void NoCapacityGivesZero() =>
        Assert.Equal(0f, ChargeModel.ReserveFraction(3, CellKind.Dry, Empty(), new CellCapacities(0, 0)));

    [Fact]
    public void OnlyAPackInUseBoostsTheBeam()
    {
        Assert.Equal(1f, ChargeModel.Boost(CellKind.Pack, 5));
        Assert.Equal(0f, ChargeModel.Boost(CellKind.Dry, 5));
        Assert.Equal(0f, ChargeModel.Boost(CellKind.Pack, 0));
        Assert.Equal(0f, ChargeModel.Boost(CellKind.None, 5));
    }

    /// <summary>Runs the model until the light goes out and counts the ticks that had light. A used pair is removed from the slots.</summary>
    private static int TicksOfLight(CellKind[] slots)
    {
        var step = ChargeModel.TurnOn(0, CellKind.None, slots, Caps);
        if (!step.LightOn)
            return 0;
        Apply(step, slots);
        var lit = 0;
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
        if (step.PairUsed)
            Array.Fill(slots, CellKind.None);
    }
}
