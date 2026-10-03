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
                if (item.Quality > 0)
                {
                    item.Quality = item.Quality - 1;
                }
            }
            else
            {
                if (item.Quality < 50)
                {
                    item.Quality = item.Quality + 1;

                    if (item.Name == BackstagePass)
                    {
                        if (item.SellIn < 11)
                        {
                            if (item.Quality < 50)
                            {
                                item.Quality = item.Quality + 1;
                            }
                        }

                        if (item.SellIn < 6)
                        {
                            if (item.Quality < 50)
                            {
                                item.Quality = item.Quality + 1;
                            }
                        }
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
                        if (item.Quality > 0)
                        {
                            item.Quality = item.Quality - 1;
                        }
                    }
                    else
                    {
                        item.Quality = item.Quality - item.Quality;
                    }
                }
                else
                {
                    if (item.Quality < 50)
                    {
                        item.Quality = item.Quality + 1;
                    }
                }
            }
        }
    }
}