using System.Collections.Generic;

namespace GildedRoseKata;

public class GildedRose
{
    private const string AgedBrie = "Aged Brie";
    private const string BackstagePass = "Backstage passes to a TAFKAL80ETC concert";

    IList<Item> Items;

    public GildedRose(IList<Item> Items)
    {
        this.Items = Items;
    }

    public void UpdateQuality()
    {
        foreach (var item in Items)
        {
            switch (item.Name)
            {
                case "Sulfuras, Hand of Ragnaros":
                    break;

                case AgedBrie:
                    IncreaseQuality(item);
                    item.SellIn = item.SellIn - 1;
                    if (item.SellIn < 0)
                    {
                        IncreaseQuality(item);
                    }
                    break;

                case BackstagePass:
                    IncreaseQuality(item);
                    if (item.SellIn < 11)
                    {
                        IncreaseQuality(item);
                    }
                    if (item.SellIn < 6)
                    {
                        IncreaseQuality(item);
                    }
                    item.SellIn = item.SellIn - 1;
                    if (item.SellIn < 0)
                    {
                        item.Quality = 0;
                    }
                    break;

                default:
                    DecreaseQuality(item);
                    item.SellIn = item.SellIn - 1;
                    if (item.SellIn < 0)
                    {
                        DecreaseQuality(item);
                    }
                    break;
            }
        }
    }

    private static void IncreaseQuality(Item item)
    {
        if (item.Quality < 50)
        {
            item.Quality = item.Quality + 1;
        }
    }

    private static void DecreaseQuality(Item item)
    {
        if (item.Quality > 0)
        {
            item.Quality = item.Quality - 1;
        }
    }
}