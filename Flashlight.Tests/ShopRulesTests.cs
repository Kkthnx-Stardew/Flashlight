using System.Globalization;

namespace Flashlight.Tests;

public class ShopRulesTests
{
    [Theory]
    [InlineData(0, "0")]
    [InlineData(15, "0.15")]
    [InlineData(25, "0.25")]
    [InlineData(50, "0.5")]
    [InlineData(90, "0.9")]
    [InlineData(100, "1")]
    public void TheDailyRollWritesTheChanceWithAFullStop(int percent, string expected) =>
        Assert.Equal($"SYNCED_RANDOM day Kkthnx.Flashlight_Roll {expected}", ShopRules.DailyRoll("Kkthnx.Flashlight_Roll", percent));

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("pl-PL")]
    [InlineData("ru-RU")]
    public void TheQueriesDoNotChangeInALanguageThatWritesACommaForADecimalPoint(string culture)
    {
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo(culture);
            Assert.Equal("SYNCED_RANDOM day Roll 0.15", ShopRules.DailyRoll("Roll", 15));
            Assert.Equal("PLAYER_HAS_ITEM Current (O)Cell 0 5", ShopRules.CarryLimit("(O)Cell", 6));
        }
        finally
        {
            CultureInfo.CurrentCulture = before;
        }
    }

    [Fact]
    public void ThePercentIsKeptBetweenNoneAndCertain()
    {
        Assert.Equal("SYNCED_RANDOM day Roll 0", ShopRules.DailyRoll("Roll", -20));
        Assert.Equal("SYNCED_RANDOM day Roll 1", ShopRules.DailyRoll("Roll", 400));
    }

    [Fact]
    public void ACarryLimitOfOneStopsSellingToAnyoneWhoHasOne() =>
        Assert.Equal("PLAYER_HAS_ITEM Current (O)Cell 0 0", ShopRules.CarryLimit("(O)Cell", 1));

    [Theory]
    [InlineData(4, "!PLAYER_HAS_ITEM Current (T)Light, DAYS_PLAYED 4")]
    [InlineData(0, "!PLAYER_HAS_ITEM Current (T)Light, DAYS_PLAYED 1")]
    public void TheLetterIsOnlyForSomeoneWithoutAFlashlightAfterAFewDays(int days, string expected) =>
        Assert.Equal(expected, ShopRules.MailAd("(T)Light", days));

    [Fact]
    public void TheFlashlightIsOnlySoldToSomeoneWithoutOne() =>
        Assert.Equal("!PLAYER_HAS_ITEM Current (T)Light", ShopRules.OneFlashlight("(T)Light"));
}
