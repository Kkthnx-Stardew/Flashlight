namespace Flashlight.Tests;

public class ChargeBarTests
{
    [Theory]
    [InlineData(0f, ChargeStage.Empty)]
    [InlineData(0.01f, ChargeStage.Red)]
    [InlineData(0.09f, ChargeStage.Red)]
    [InlineData(0.10f, ChargeStage.Orange)]
    [InlineData(0.24f, ChargeStage.Orange)]
    [InlineData(0.25f, ChargeStage.Yellow)]
    [InlineData(0.49f, ChargeStage.Yellow)]
    [InlineData(0.50f, ChargeStage.Green)]
    [InlineData(1f, ChargeStage.Green)]
    public void TheColourStepFollowsTheReserve(float fraction, ChargeStage expected) =>
        Assert.Equal(expected, ChargeBar.StageFor(fraction));

    [Fact]
    public void TheStepsRunInOrderAsTheReserveFalls()
    {
        var last = ChargeStage.Green;
        for (var f = 1.0f; f >= 0f; f -= 0.01f)
        {
            var stage = ChargeBar.StageFor(f);
            Assert.True(stage <= last, $"The stage went back up at {f}.");
            last = stage;
        }
    }

    [Theory]
    [InlineData(0f, 48, 0)]
    [InlineData(1f, 48, 48)]
    [InlineData(0.5f, 48, 24)]
    [InlineData(0.001f, 48, 1)] // a nearly flat flashlight is not drawn as an empty one
    [InlineData(2f, 48, 48)]
    public void TheFilledWidthIsProportionalButNeverHidesASmallCharge(float fraction, int full, int expected) =>
        Assert.Equal(expected, ChargeBar.FilledWidth(fraction, full));
}
