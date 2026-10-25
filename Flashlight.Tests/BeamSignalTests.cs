namespace Flashlight.Tests;

public class BeamSignalTests
{
    [Theory]
    [InlineData(0f)]
    [InlineData(1.81f)]
    [InlineData(7.23f)]
    [InlineData(8.68f)]
    public void ASizeComesBackTheWayItWentIn(float radius)
    {
        Assert.True(BeamSignal.TryDecode(BeamSignal.Encode(radius), out var back));
        Assert.Equal(radius, back, 2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]   // the lantern, which is what a light is first sent as
    [InlineData(4)]
    [InlineData(10)]
    [InlineData(9_999)]
    public void TheGamesOwnPictureNumbersAreNeverTakenForASize(int textureIndex)
    {
        Assert.False(BeamSignal.TryDecode(textureIndex, out _));
    }

    [Fact]
    public void ASizeIsAlwaysAboveTheGamesOwnPictureNumbers() =>
        Assert.True(BeamSignal.Encode(0f) > 10);

    [Fact]
    public void AbsurdSizesAreKeptInRange()
    {
        Assert.Equal(BeamSignal.Base, BeamSignal.Encode(-5f));
        Assert.Equal(BeamSignal.Base, BeamSignal.Encode(float.NaN));
        Assert.Equal(BeamSignal.Base + 100_000, BeamSignal.Encode(float.MaxValue));
        Assert.Equal(BeamSignal.Base + 100_000, BeamSignal.Encode(float.PositiveInfinity));
        Assert.Equal(BeamSignal.Base, BeamSignal.Encode(float.NegativeInfinity));
    }
}
