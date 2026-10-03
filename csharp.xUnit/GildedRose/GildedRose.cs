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
            if (item.Name == "Sulfuras, Hand of Ragnaros")
            {
                continue;
            }

            if (item.Name != AgedBrie && item.Name != BackstagePass)
            {
                DecreaseQuality(item);
            }
            else
            {
                IncreaseQuality(item);

                if (item.Name == BackstagePass)
                {
                    if (item.SellIn < 11)
                    {
                        IncreaseQuality(item);
                    }

                    if (item.SellIn < 6)
                    {
                        IncreaseQuality(item);
                    }
                }
            }

            item.SellIn = item.SellIn - 1;

            if (item.SellIn < 0)
            {
                if (item.Name != AgedBrie)
                {
                    if (item.Name != BackstagePass)
                    {
                        DecreaseQuality(item);
                    }
                    else
                    {
                        item.Quality = item.Quality - item.Quality;
                    }
                }
                else
                {
                    IncreaseQuality(item);
                }
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