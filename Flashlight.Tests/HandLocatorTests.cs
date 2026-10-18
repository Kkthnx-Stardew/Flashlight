namespace Flashlight.Tests;

public class HandLocatorTests
{
    private static byte[] Cell(int width, int height, params (int x, int y)[] pixels)
    {
        var alpha = new byte[width * height];
        foreach (var (x, y) in pixels)
            alpha[y * width + x] = 255;
        return alpha;
    }

    [Fact]
    public void AnEmptyCellHasNoHand() => Assert.Null(HandLocator.Find(new byte[16 * 32], 16, 32));

    [Fact]
    public void ACellThatIsTooSmallForItsSizeHasNoHand() => Assert.Null(HandLocator.Find(new byte[10], 16, 32));

    [Fact]
    public void TheHandIsTheMiddleOfTheLowestRows()
    {
        // an arm two pixels wide, from row 4 down to row 10, with the hand in the last three rows
        var pixels = new List<(int, int)>();
        for (var y = 4; y <= 10; y++)
        {
            pixels.Add((7, y));
            pixels.Add((8, y));
        }
        var hand = HandLocator.Find(Cell(16, 32, pixels.ToArray()), 16, 32);
        Assert.NotNull(hand);
        Assert.Equal(8f, hand!.Value.X, 3);
        Assert.Equal(9.5f, hand.Value.Y, 3);
    }

    [Fact]
    public void ASinglePixelIsItsOwnHand()
    {
        var hand = HandLocator.Find(Cell(16, 32, (3, 20)), 16, 32);
        Assert.Equal(3.5f, hand!.Value.X, 3);
        Assert.Equal(20.5f, hand.Value.Y, 3);
    }

    [Fact]
    public void RowsAboveTheHandDoNotPullTheMiddleSideways()
    {
        // a wide shoulder high up must not move the hand, which is found from the bottom rows only
        var hand = HandLocator.Find(Cell(16, 32, (0, 2), (1, 2), (2, 2), (12, 10), (12, 11), (12, 12)), 16, 32);
        Assert.Equal(12.5f, hand!.Value.X, 3);
    }

    [Fact]
    public void LookingAtOneHalfFindsThatArmsHand()
    {
        // two arms, the left one reaching lower than the right
        var cell = Cell(16, 32, (3, 18), (3, 19), (3, 20), (12, 14), (12, 15), (12, 16));
        var left = HandLocator.Find(cell, 16, 32, 0, 8);
        var right = HandLocator.Find(cell, 16, 32, 8, 16);
        Assert.Equal(3.5f, left!.Value.X, 3);
        Assert.Equal(12.5f, right!.Value.X, 3);
        Assert.Equal(19.5f, left.Value.Y, 3);
        Assert.Equal(15.5f, right.Value.Y, 3);
    }

    [Fact]
    public void AHalfWithNothingInItHasNoHand() =>
        Assert.Null(HandLocator.Find(Cell(16, 32, (3, 18)), 16, 32, 8, 16));
}
