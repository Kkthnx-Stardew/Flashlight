namespace Flashlight.Tests;

public class BeamShadowTests
{
    private static Func<int, int, float> Wall(params (int X, int Y)[] tiles) => (x, y) => tiles.Contains((x, y)) ? 0f : BeamShadow.Open;

    /// <summary>A thin thing standing in the middle of one tile, with this much of each side left open.</summary>
    private static Func<int, int, float> Trunk(int tileX, int tileY, float open) => (x, y) => x == tileX && y == tileY ? open : BeamShadow.Open;

    [Fact]
    public void AnOpenFieldIsClear()
    {
        Assert.True(BeamShadow.IsClear(0.5f, 0.5f, 9.5f, 3.5f, Wall()));
    }

    [Fact]
    public void ATileOnTheLineBlocksIt()
    {
        Assert.False(BeamShadow.IsClear(0.5f, 0.5f, 6.5f, 0.5f, Wall((3, 0))));
    }

    [Fact]
    public void ATileOffTheLineDoesNot()
    {
        Assert.True(BeamShadow.IsClear(0.5f, 0.5f, 6.5f, 0.5f, Wall((3, 1), (3, -1))));
    }

    [Fact]
    public void TheTileTheLineStartsInNeverBlocks()
    {
        Assert.True(BeamShadow.IsClear(0.5f, 0.5f, 4.5f, 0.5f, Wall((0, 0))));
    }

    [Fact]
    public void TheTileTheLineEndsInBlocksUnlessAskedToIgnoreIt()
    {
        var tree = Wall((4, 0));
        Assert.False(BeamShadow.IsClear(0.5f, 0.5f, 4.5f, 0.5f, tree));
        Assert.True(BeamShadow.IsClear(0.5f, 0.5f, 4.5f, 0.5f, tree, ignoreEnd: true));
    }

    [Fact]
    public void IgnoringTheEndStillSeesAnEarlierBlock()
    {
        Assert.False(BeamShadow.IsClear(0.5f, 0.5f, 4.5f, 0.5f, Wall((2, 0), (4, 0)), ignoreEnd: true));
    }

    [Theory]
    [InlineData(-6.5f, 0.5f)]   // west
    [InlineData(0.5f, -6.5f)]   // north
    [InlineData(0.5f, 7.5f)]    // south
    [InlineData(-5.5f, -5.5f)]  // diagonal
    public void ItWorksInEveryDirection(float toX, float toY)
    {
        var fromX = 0.5f;
        var fromY = 0.5f;
        var x = (int)Math.Floor((fromX + toX) / 2f);
        var y = (int)Math.Floor((fromY + toY) / 2f);
        Assert.False(BeamShadow.IsClear(fromX, fromY, toX, toY, Wall((x, y))));
        Assert.True(BeamShadow.IsClear(fromX, fromY, toX, toY, Wall()));
    }

    [Fact]
    public void ASteepLineFindsATileItPassesThroughTheCorner()
    {
        // from the middle of (0,0) to the middle of (2,4): it crosses (1,2)
        Assert.False(BeamShadow.IsClear(0.5f, 0.5f, 2.5f, 4.5f, Wall((1, 2))));
    }

    [Fact]
    public void SofteningLeavesFlatAreasAloneAndBlursAnEdge()
    {
        const int size = 8;
        var flat = Enumerable.Repeat((byte)255, size * size).ToArray();
        BeamShadow.Soften(flat, new ushort[size * size], size);
        Assert.All(flat, v => Assert.Equal(255, v));

        var edge = new byte[size * size];
        for (var y = 0; y < size; y++)
            for (var x = 4; x < size; x++)
                edge[y * size + x] = 255;
        BeamShadow.Soften(edge, new ushort[size * size], size);
        Assert.Equal(0, edge[3 * size + 1]);
        Assert.InRange(edge[3 * size + 3], 1, 254);
        Assert.InRange(edge[3 * size + 4], 1, 254);
        Assert.Equal(255, edge[3 * size + 6]);
    }

    [Fact]
    public void SofteningGivesTheSameResultAsAveragingTheNineAroundEachValue()
    {
        // a plain nine-value average, with the edge repeating its own value, is what the two passes must equal
        const int size = 11;
        var random = new Random(7);
        var values = new byte[size * size];
        random.NextBytes(values);
        var expected = new byte[size * size];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var sum = 0;
                for (var oy = -1; oy <= 1; oy++)
                    for (var ox = -1; ox <= 1; ox++)
                        sum += values[Math.Clamp(y + oy, 0, size - 1) * size + Math.Clamp(x + ox, 0, size - 1)];
                expected[y * size + x] = (byte)(sum / 9);
            }
        }

        BeamShadow.Soften(values, new ushort[size * size], size);
        Assert.Equal(expected, values);
    }

    [Fact]
    public void ALineThroughTheMiddleOfATrunkIsBlocked()
    {
        Assert.False(BeamShadow.IsClear(0.5f, 0.5f, 6.5f, 0.5f, Trunk(3, 0, 0.3f)));
    }

    [Fact]
    public void ALinePastTheSideOfATrunkWithinItsTileIsNot()
    {
        // the tile is 3.0 to 4.0 across and the trunk fills 3.3 to 3.7, so a line along 0.1 of the way down the tile misses it
        Assert.True(BeamShadow.IsClear(0.5f, 0.1f, 6.5f, 0.1f, Trunk(3, 0, 0.3f)));
    }

    [Fact]
    public void ALineClippingTheOuterCornerOfTheTileMissesTheTrunk()
    {
        Assert.True(BeamShadow.IsClear(2.5f, -0.5f, 4.5f, 0.2f, Trunk(3, 0, 0.3f)));
    }

    [Fact]
    public void ThePointJustBesideATrunkInTheSameTileIsLit()
    {
        Assert.True(BeamShadow.IsClear(0.5f, 0.15f, 3.15f, 0.15f, Trunk(3, 0, 0.3f)));
        Assert.False(BeamShadow.IsClear(0.5f, 0.5f, 3.5f, 0.5f, Trunk(3, 0, 0.3f)));
    }
}
