# Gilded Rose starting position in C# xUnit

## Build the project

Use your normal build tools to build the projects in Debug mode.
For example, you can use the `dotnet` command line tool:

``` cmd
dotnet build GildedRose.sln -c Debug
```

## Run the Gilded Rose Command-Line program

For e.g. 10 days:

``` cmd
GildedRose/bin/Debug/net8.0/GildedRose 10
```

## Run all the unit tests

``` cmd
dotnet test
```

## Known legacy behaviour

> **This section describes current behaviour that is deliberately preserved
> during the refactoring.** It is not a recommendation for future design.
> Some of these behaviours contradict or go beyond `GildedRoseRequirements.md`.
> They are kept because the refactoring must not change observable behaviour.
> Changing any of them is a separate product decision, made outside the
> refactoring.

Each behaviour is pinned by a named test in
`GildedRoseTests/LegacyBehaviourTests.cs`, and also by the golden master
(`ApprovalTest.GoldenMaster`).

| Current behaviour | Example (`SellIn`, `Quality` → after one day) | Pinned by |
|---|---|---|
| Item categories are matched by exact, case-sensitive full name. Variants such as `aged brie`, `Sulfuras` or `Backstage passes to another concert` are treated as normal items. | `Sulfuras`: (5, 10) → (4, 9) | `ItemCategory_IsMatchedByExactFullName` |
| Aged Brie gains 2 quality per day once the sell-by date has passed. The requirements only say it "increases". | `Aged Brie`: (0, 10) → (-1, 12) | `AgedBrie_QualityIncreasesTwiceAsFast_OnceSellByDateHasPassed` |
| The 0 and 50 limits only stop quality from crossing them. A quality that is already outside 0..50 is never corrected. | Normal item: (5, 80) → (4, 79); (5, -1) → (4, -1). Aged Brie and Backstage passes: (5, 80) → (4, 80) | `QualityOutsideValidRange_IsNotCorrected` |
| Sulfuras keeps whatever quality it has. 80 is not enforced. | `Sulfuras, Hand of Ragnaros`: (5, 10) → (5, 10) | `Sulfuras_KeepsQualityOtherThan80` |

For the full analysis, including edge cases covered only by the golden master,
see `docs/ai/behaviour-model.md`.
