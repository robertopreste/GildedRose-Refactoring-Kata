using GildedRoseKata;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VerifyXunit;

using Xunit;

namespace GildedRoseTests;

public class ApprovalTest
{
    [Fact]
    public Task GoldenMaster()
    {
        string[] names =
        {
            "+5 Dexterity Vest",
            "Conjured Mana Cake",
            "Aged Brie",
            "Backstage passes to a TAFKAL80ETC concert",
            "Sulfuras, Hand of Ragnaros",
            "aged brie",
            "Backstage passes to another concert",
            "Sulfuras"
        };
        int[] sellIns = { -1, 0, 1, 5, 6, 10, 11, 12 };
        int[] qualities = { -1, 0, 1, 2, 48, 49, 50, 80 };
        const int days = 14;

        var items = new List<Item>();
        foreach (var name in names)
        foreach (var sellIn in sellIns)
        foreach (var quality in qualities)
        {
            items.Add(new Item { Name = name, SellIn = sellIn, Quality = quality });
        }

        var trajectories = items
            .Select(item => new StringBuilder($"{item.Name} | sellIn {item.SellIn} | quality {item.Quality} ->"))
            .ToList();

        var app = new GildedRose(items);
        for (var day = 0; day < days; day++)
        {
            app.UpdateQuality();
            for (var i = 0; i < items.Count; i++)
            {
                trajectories[i].Append($" {items[i].SellIn}/{items[i].Quality}");
            }
        }

        return Verifier.Verify(string.Join("\n", trajectories));
    }
    
    [Fact]
    public Task ThirtyDays()
    {
        var fakeoutput = new StringBuilder();
        Console.SetOut(new StringWriter(fakeoutput));
        Console.SetIn(new StringReader($"a{Environment.NewLine}"));

        Program.Main(new string[] { "30" });
        var output = fakeoutput.ToString();

        return Verifier.Verify(output);
    }
}