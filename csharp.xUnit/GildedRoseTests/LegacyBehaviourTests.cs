using GildedRoseKata;

using Xunit;

namespace GildedRoseTests;

/// <summary>
/// Pins behaviour of the legacy implementation that the requirements do not state or contradict.
/// These are not requirements: changing any of them is a product decision, not a refactoring.
/// </summary>
public class LegacyBehaviourTests
{
    private const string NormalItem = "+5 Dexterity Vest";
    private const string AgedBrie = "Aged Brie";
    private const string BackstagePass = "Backstage passes to a TAFKAL80ETC concert";
    private const string Sulfuras = "Sulfuras, Hand of Ragnaros";

    [Theory]
    [InlineData(0, 10, 12)]
    [InlineData(-1, 10, 12)]
    public void AgedBrie_QualityIncreasesTwiceAsFast_OnceSellByDateHasPassed(int sellIn, int quality, int expectedQuality)
    {
        var item = UpdateOnce(AgedBrie, sellIn, quality);

        Assert.Equal(expectedQuality, item.Quality);
    }

    [Theory]
    [InlineData("aged brie")]
    [InlineData("Backstage passes to another concert")]
    [InlineData("Sulfuras")]
    public void ItemCategory_IsMatchedByExactFullName(string name)
    {
        var item = UpdateOnce(name, sellIn: 5, quality: 10);

        Assert.Equal(4, item.SellIn);
        Assert.Equal(9, item.Quality);
    }

    [Theory]
    [InlineData(NormalItem, 80, 79)]
    [InlineData(NormalItem, -1, -1)]
    [InlineData(AgedBrie, 80, 80)]
    [InlineData(BackstagePass, 80, 80)]
    public void QualityOutsideValidRange_IsNotCorrected(string name, int quality, int expectedQuality)
    {
        var item = UpdateOnce(name, sellIn: 5, quality);

        Assert.Equal(expectedQuality, item.Quality);
    }

    [Fact]
    public void Sulfuras_KeepsQualityOtherThan80()
    {
        var item = UpdateOnce(Sulfuras, sellIn: 5, quality: 10);

        Assert.Equal(10, item.Quality);
    }

    private static Item UpdateOnce(string name, int sellIn, int quality)
    {
        var item = new Item { Name = name, SellIn = sellIn, Quality = quality };
        new GildedRose(new[] { item }).UpdateQuality();
        return item;
    }
}