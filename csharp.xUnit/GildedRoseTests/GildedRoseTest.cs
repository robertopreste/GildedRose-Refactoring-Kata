using GildedRoseKata;

using Xunit;

namespace GildedRoseTests;

public class GildedRoseTest
{
    private const string NormalItem = "+5 Dexterity Vest";
    private const string AgedBrie = "Aged Brie";
    private const string BackstagePass = "Backstage passes to a TAFKAL80ETC concert";
    private const string Sulfuras = "Sulfuras, Hand of Ragnaros";

    [Theory]
    [InlineData(NormalItem)]
    [InlineData(AgedBrie)]
    [InlineData(BackstagePass)]
    public void SellIn_DecreasesByOneEachDay(string name)
    {
        var item = UpdateOnce(name, sellIn: 5, quality: 10);

        Assert.Equal(4, item.SellIn);
    }

    [Theory]
    [InlineData(10, 20, 19)]
    [InlineData(1, 20, 19)]
    public void NormalItem_QualityDegradesByOne_BeforeSellByDate(int sellIn, int quality, int expectedQuality)
    {
        var item = UpdateOnce(NormalItem, sellIn, quality);

        Assert.Equal(expectedQuality, item.Quality);
    }

    [Theory]
    [InlineData(0, 20, 18)]
    [InlineData(-1, 20, 18)]
    public void NormalItem_QualityDegradesTwiceAsFast_OnceSellByDateHasPassed(int sellIn, int quality, int expectedQuality)
    {
        var item = UpdateOnce(NormalItem, sellIn, quality);

        Assert.Equal(expectedQuality, item.Quality);
    }

    [Theory]
    [InlineData(5, 0)]
    [InlineData(0, 1)]
    public void Quality_IsNeverNegative(int sellIn, int quality)
    {
        var item = UpdateOnce(NormalItem, sellIn, quality);

        Assert.Equal(0, item.Quality);
    }

    [Theory]
    [InlineData(5, 10, 11)]
    [InlineData(1, 10, 11)]
    public void AgedBrie_QualityIncreasesWithAge(int sellIn, int quality, int expectedQuality)
    {
        var item = UpdateOnce(AgedBrie, sellIn, quality);

        Assert.Equal(expectedQuality, item.Quality);
    }

    [Theory]
    [InlineData(AgedBrie, 5, 50)]
    [InlineData(AgedBrie, 0, 49)]
    [InlineData(BackstagePass, 10, 49)]
    [InlineData(BackstagePass, 5, 48)]
    public void Quality_NeverIncreasesAbove50(string name, int sellIn, int quality)
    {
        var item = UpdateOnce(name, sellIn, quality);

        Assert.Equal(50, item.Quality);
    }

    [Theory]
    [InlineData(11, 1)]
    [InlineData(10, 2)]
    [InlineData(6, 2)]
    [InlineData(5, 3)]
    [InlineData(1, 3)]
    public void BackstagePass_QualityIncrease_DependsOnDaysUntilConcert(int sellIn, int expectedIncrease)
    {
        var item = UpdateOnce(BackstagePass, sellIn, quality: 20);

        Assert.Equal(20 + expectedIncrease, item.Quality);
    }

    [Theory]
    [InlineData(20)]
    [InlineData(50)]
    public void BackstagePass_QualityDropsToZero_AfterConcert(int quality)
    {
        var item = UpdateOnce(BackstagePass, sellIn: 0, quality);

        Assert.Equal(0, item.Quality);
    }

    [Theory]
    [InlineData(5)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Sulfuras_NeverChanges(int sellIn)
    {
        var item = UpdateOnce(Sulfuras, sellIn, quality: 80);

        Assert.Equal(sellIn, item.SellIn);
        Assert.Equal(80, item.Quality);
    }

    private static Item UpdateOnce(string name, int sellIn, int quality)
    {
        var item = new Item { Name = name, SellIn = sellIn, Quality = quality };
        new GildedRose(new[] { item }).UpdateQuality();
        return item;
    }
}