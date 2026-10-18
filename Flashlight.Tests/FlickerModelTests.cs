namespace Flashlight.Tests;

public class FlickerModelTests
{
    [Fact]
    public void NeverStartsWhenSwitchedOff()
    {
        var random = new Random(1);
        for (var i = 0; i < 100_000; i++)
            Assert.False(FlickerModel.ShouldStart(random, 0));
    }

    [Fact]
    public void StartsAboutOnceEveryConfiguredNumberOfSeconds()
    {
        var random = new Random(42);
        const int seconds = 40;
        const int updates = FlickerModel.TicksPerSecond * seconds * 200; // 200 intervals' worth of play
        var starts = 0;
        for (var i = 0; i < updates; i++)
        {
            if (FlickerModel.ShouldStart(random, seconds))
                starts++;
        }
        Assert.InRange(starts, 140, 270); // 200 expected
    }

    [Fact]
    public void ALowBatteryFlickersMuchMoreOftenThanTheSetting()
    {
        Assert.Equal(FlickerModel.LowBatterySecondsBetween, FlickerModel.Interval(40, low: true));
        Assert.Equal(40, FlickerModel.Interval(40, low: false));
        Assert.True(FlickerModel.LowBatterySecondsBetween < 40);
    }

    [Fact]
    public void AFlickerLastsBetweenAboutAFewTenthsOfASecond()
    {
        var random = new Random(7);
        for (var i = 0; i < 1000; i++)
            Assert.InRange(FlickerModel.NewLength(random), 6, 30);
    }

    [Fact]
    public void StrengthIsFullOutsideAFlickerAndAlwaysBetweenZeroAndOne()
    {
        var random = new Random(3);
        Assert.Equal(1f, FlickerModel.Strength(0, random));
        for (var left = 1; left <= 30; left++)
        {
            for (var i = 0; i < 50; i++)
                Assert.InRange(FlickerModel.Strength(left, random), 0f, 1f);
        }
    }

    [Fact]
    public void AFlickerSettlesBackNearFullAtTheEnd()
    {
        var random = new Random(5);
        for (var left = 1; left <= 3; left++)
            Assert.True(FlickerModel.Strength(left, random) >= 0.85f);
    }

    [Fact]
    public void AFlickerReallyDropsOutNowAndThen()
    {
        var random = new Random(9);
        var sawDark = false;
        for (var i = 0; i < 500; i++)
            sawDark |= FlickerModel.Strength(20, random) == 0f;
        Assert.True(sawDark);
    }
}
