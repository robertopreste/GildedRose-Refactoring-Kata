# Gilded Rose: testing strategy for the refactoring

Sources: `GildedRoseRequirements.md`, the original `GildedRose.cs`, and
`docs/behaviour-model.md` (IDs such as `Q5` or `P11` refer to that document).

## 1. Goal and principles

The tests must let us restructure `UpdateQuality()` freely while detecting
**any** change in observable behaviour, including the legacy quirks.

The suite has three layers, each with a distinct job:

| Layer | Job | Fails when… | Reader |
|---|---|---|---|
| **Golden master** (approval test) | Safety net: exhaustive, mechanical comparison against the original code's output | any input/day combination changes | the refactorer |
| **Business-rule tests** | Living documentation of the requirements, one test per rule | a specified rule is broken | anyone learning the domain |
| **Legacy-behaviour tests** | Make deliberate, non-obvious quirks visible and pinned | a quirk is "accidentally fixed" | whoever decides on bug fixes or adds Conjured |

Principles:

- The golden master is generated from the **original** code and committed
  **before** any production change (AGENTS.md rule 3). During refactoring it
  is never re-approved. A red golden master means revert (rule 6).
- Named tests are few and readable. Each one documents a rule, and the golden
  master gives the breadth.
- Business rules and legacy quirks live in **separate test classes**. If the
  team later decides to fix a quirk, the change shows up as an edit to
  `LegacyBehaviourTests`, never as a silent change to a "requirement" test.
- No tests for pathological inputs (Q11–Q14: `null` names or elements,
  `int.MinValue`, the same instance listed twice). None of them is a scenario
  the inn has. The golden master would catch changes there anyway if it
  contained them, and it doesn't need to.

## 2. Assessment of the existing tests

### `ApprovalTest.ThirtyDays`: keep unchanged

It runs `Program.Main` for 30 days and snapshots the console output. I checked
the approved file against the behaviour model. It covers more than it seems
to:

| Covered ✅ | Evidence in the snapshot |
|---|---|
| Normal −1, then −2 from `SellIn = 0` (Q4) | Vest `0:10 → -1:8` |
| Floor at 0, also mid-update | Elixir `0:2 → -1:0`, Conjured `-1:1 → -2:0` |
| Brie +1, then +2 after sell date (Q3) | `1:1 → 0:2 → -1:4` |
| Brie cap at 50 | `-23:48 → -24:50 → -25:50` |
| Backstage tiers at every boundary (11, 10, 6, 5, 1) | `11:24→10:25`, `10:25→9:27`, `6:33→5:35`, `5:35→4:38`, `1:47→0:50` |
| Backstage +2 and +3 truncated at 50 | `10:49 → 9:50`, `5:49 → 4:50` |
| Backstage drops to 0 after the concert | `0:50 → -1:0` |
| Sulfuras unchanged at `SellIn` 0 and −1 | constant `0, 80` / `-1, 80` |
| Conjured currently behaves as Normal (Q2) | `3:6 → 2:5 …` |

| Not covered ❌ | Why it matters for this refactoring |
|---|---|
| Quality outside 0..50 on non-Sulfuras items (Q5, Q6) | AGENTS.md suggests "shared quality clamping in one place". A naive `Math.Clamp(q, 0, 50)` would change these outputs, and nothing would catch it. |
| Negative quality, including the Backstage reset (Q7) | same as above |
| Brie +2 truncated at 50 from 49 (B7) | a different truncation path from the Backstage one |
| Sulfuras with quality ≠ 80 or `SellIn` > 0 (Q9) | a `SulfurasUpdater` that "sets 80" would pass |
| Near-miss names (Q1) | moving to a lookup by prefix or case-insensitive name would pass |

Other weaknesses (no change proposed, just to be aware):

- The inputs live in production code (`Program.cs`), not in the test.
- It also locks the console format of `Program`. That's fine, but it means
  the Conjured feature will legitimately change this snapshot later.
- It redirects `Console` globally and never restores it. That's harmless
  today, but it's a reason not to add more console-based tests.
- When it fails, the diff is spread over a 300-line, day-by-day file, which
  makes it hard to pinpoint the item.

### `ApprovalTest.Foo`: delete

It has never had an approved baseline (only an ignored `.received.txt`). It
covers one trivial case. Verify also leaves out `Quality: 0` from its output
because 0 is the default value, which makes the snapshot misleading. The new
golden master replaces it.

### `GildedRoseTest.foo`: replace

It's a kata placeholder that deliberately fails (`"fixme"`). The business-rule
tests below replace it (you approved this).

## 3. Layer 1: combinatorial golden master

**What it is.** A new `[Fact]` in `ApprovalTest` that builds one item for every
combination below. It runs `UpdateQuality()` a fixed number of days on a
single `GildedRose` instance and verifies a plain-text rendering with Verify,
the snapshot library already in the project. On the first run, Verify writes
`*.received.txt`. We review it and rename it to `*.verified.txt`, and that file
becomes the committed baseline.

**Inputs** (8 × 8 × 8 = 512 starting items):

| Dimension | Values | Why |
|---|---|---|
| Name | `+5 Dexterity Vest`, `Conjured Mana Cake`, `Aged Brie`, `Backstage passes to a TAFKAL80ETC concert`, `Sulfuras, Hand of Ragnaros`, `aged brie`, `Backstage passes to another concert`, `Sulfuras` | every category, the future Conjured name, and the near-misses (Q1) |
| SellIn | −1, 0, 1, 5, 6, 10, 11, 12 | both sides of every threshold (`< 0` after ageing, `< 6`, `< 11`) |
| Quality | −1, 0, 1, 2, 48, 49, 50, 80 | floor, double-step floor (1, 2), cap and truncation (48, 49, 50), and out-of-range values (−1, 80) |
| Days | 14 updates | an item starting at `SellIn = 12` crosses 11→10, 6→5 and 0→−1 |

This is a superset of the AGENTS.md minimum (negative, 0, 1, 5, 6, 10, 11 ×
0, 1, 49, 50, 80).

**Format: one line per starting item, showing its trajectory.**

```
Aged Brie | sellIn 0 | quality 49 -> -1/50 -2/50 -3/50 …
```

This avoids the Verify default-value issue, because we render our own text.
More importantly, a failing diff points straight at the input combination
that changed behaviour.

**What it protects:** every rule and quirk Q1–Q10, for every combination, over
multiple days. It also exercises the loop over many items in one call. That
loop is easy to break with an early `return` or `break` when you introduce a
`switch`.

## 4. Layer 2: business-rule tests (`GildedRoseTest.cs`)

These replace the placeholder. Each test documents **one** rule from
`GildedRoseRequirements.md`. They use a shared helper
`Item UpdateOnce(string name, int sellIn, int quality)` and name constants
(`const string AgedBrie = "Aged Brie";`). xUnit attribute arguments must be
compile-time constants, which is why these are `const`.

xUnit primer:
- `[Fact]` is a test without parameters.
- `[Theory]` is a parameterised test, and each `[InlineData(...)]` row runs as
  a separate test case with its own pass/fail in the output.

I use `[Theory]` only when the rows express **the same rule** at different
points, typically on both sides of a boundary.

| # | Test | Rule documented (requirement) | Rows `(name, sellIn, quality → expected)` | Model IDs |
|---|---|---|---|---|
| R1 | `SellIn_DecreasesByOneEachDay` | "At the end of each day our system lowers both values for every item" (SellIn part) | `[Theory]` over names: Vest, Brie, Backstage; `sellIn 5 → 4` | N1, B1, P1 |
| R2 | `NormalItem_QualityDegradesByOne_BeforeSellByDate` | "lowers both values" (Quality part) | `(10, 20 → 19)`, `(1, 20 → 19)`; 1 is the last day before the date | N1, N2 |
| R3 | `NormalItem_QualityDegradesTwiceAsFast_OnceSellByDateHasPassed` | "Once the sell by date has passed, Quality degrades twice as fast" | `(0, 20 → 18)`, `(-1, 20 → 18)` | N3, N4 |
| R4 | `Quality_IsNeverNegative` | "The Quality of an item is never negative" | `(5, 0 → 0)`, `(0, 1 → 0)`: the second shows the floor applies within the double step | N5, N6 |
| R5 | `AgedBrie_QualityIncreasesWithAge` | "Aged Brie actually increases in Quality the older it gets" | `(5, 10 → 11)`, `(1, 10 → 11)` | B1, B2 |
| R6 | `Quality_NeverIncreasesAbove50` | "The Quality of an item is never more than 50" | `(Brie, 5, 50 → 50)`, `(Brie, 0, 49 → 50)`, `(Backstage, 10, 49 → 50)`, `(Backstage, 5, 48 → 50)` | B6, B7, P9, P11 |
| R7 | `BackstagePass_QualityIncrease_DependsOnDaysUntilConcert` | "increases by 2 when there are 10 days or less and by 3 when there are 5 days or less" | `(sellIn → increase)`: `11 → 1`, `10 → 2`, `6 → 2`, `5 → 3`, `1 → 3` | P2–P6 |
| R8 | `BackstagePass_QualityDropsToZero_AfterConcert` | "Quality drops to 0 after the concert" | `(0, 20 → 0)`, `(0, 50 → 0)` | P7 |
| R9 | `Sulfuras_NeverChanges` | "Sulfuras … never has to be sold or decreases in Quality … its Quality is 80 and it never alters" | `(sellIn 5, 0, -1; quality 80)`: both `SellIn` and `Quality` unchanged | S1–S3 |

R6 is deliberately **rule-centred** rather than item-centred. "Never above 50"
is a single rule enforced across two item types, and the rows show every way
the cap can be reached: a plain step, Brie's double step, and Backstage's +2
and +3.

R7 phrases the rows as "increase by N" rather than absolute values. That
makes the tier table in the requirement readable directly from the test.

## 5. Layer 3: legacy-behaviour tests (`LegacyBehaviourTests.cs`)

These pin behaviour that the code has but the requirements don't state, or
contradict. They're worth capturing explicitly because the refactoring
direction in AGENTS.md (an updater chosen by name, with shared clamping)
makes each of them **easy to change by accident**. The golden master would
catch such a change, but these tests say *why* the output is what it is. The
class gets a short summary comment explaining that these are pinned legacy
behaviours, not requirements, and that changing them is a product decision.
The individual tests carry no comments: their names are the documentation.

| # | Test | Legacy behaviour pinned | Why it's at risk in this refactoring | Rows | Model IDs |
|---|---|---|---|---|---|
| L1 | `AgedBrie_QualityIncreasesTwiceAsFast_OnceSellByDateHasPassed` | Brie gains +2 after the sell date (the spec only says "increases") | A `BrieUpdater` written from the spec would do +1 | `(0, 10 → 12)`, `(-1, 10 → 12)` | Q3, B3, B4 |
| L2 | `ItemCategory_IsMatchedByExactFullName` | Only the exact, case-sensitive full names are special. Variants degrade like normal items. | A dictionary with a case-insensitive comparer, or `StartsWith("Backstage passes")`, is a tempting "improvement". The Conjured feature will add name matching too. | `aged brie`, `Backstage passes to another concert`, `Sulfuras`: all `(5, 10 → 4, 9)` | Q1, M1–M3 |
| L3 | `ConjuredItem_CurrentlyDegradesLikeNormalItem` | Conjured isn't implemented yet | Makes the future feature a visible, deliberate change: that commit **edits this test** | `(3, 6 → 5)`, `(0, 6 → 4)` | Q2, M4 |
| L4 | `QualityOutsideValidRange_IsNotCorrected` | The 0 and 50 limits only stop a step. They never pull an out-of-range value back into range. | "Shared clamping in one place" with `Math.Clamp` would change every row | `(Vest, 5, 80 → 79)`, `(Vest, 5, -1 → -1)`, `(Brie, 5, 80 → 80)`, `(Backstage, 5, 80 → 80)` | Q5, Q6, N8, N9, B9, P13 |
| L5 | `Sulfuras_KeepsQualityOtherThan80` | Quality 80 isn't enforced | A `SulfurasUpdater` that "sets 80" would match the spec but not the code | `(5, 10 → 10)` | Q9, S4 |

Intentionally **not** given named tests (the golden master still covers them):

| Behaviour | Reason |
|---|---|
| Q4: expiry starts at `SellIn = 0` | Already shown by the `sellIn 0` rows in R3 and R8. It's the natural reading of the spec, not a quirk worth its own test. |
| Q7: Backstage reset turns a negative quality into 0 | A clamp-style refactoring would produce the same result. Low risk, and the golden master covers it via `quality = -1`. |
| Q8: Backstage tiers use the pre-decrement `SellIn` | That's exactly what R7's boundary rows pin. |
| Q10: Sulfuras `SellIn` frozen | Part of R9. |
| Q11–Q14 | Pathological (see principles). |

## 6. Mutation-testing outlook (Stryker.NET, end of refactoring)

The boundary rows were chosen so that typical mutants die in a **named** test,
not only in the golden master. That makes survivors easier to interpret.

| Mutant in the comparison | Killed by |
|---|---|
| `SellIn < 11` → `<= 11` | R7 row `11 → 1` |
| `SellIn < 6` → `<= 6` | R7 row `6 → 2` |
| `SellIn < 0` → `<= 0` | R2 row `(1, 20 → 19)` |
| `Quality > 0` → `>= 0` | R4 row `(5, 0 → 0)` |
| `Quality < 50` → `<= 50` | R6 row `(Brie, 5, 50 → 50)` |
| Removed truncation guard in Backstage | R6 rows `(10, 49)`, `(5, 48)` |
| `Quality = 0` → removed | R8 |

## 7. Proposed implementation steps (one diff each, per AGENTS.md rule 4)

| Step | Change | Expected `dotnet test` result |
|---|---|---|
| T1 | Add the combinatorial golden master to `ApprovalTest`, remove `ApprovalTest.Foo`, approve the baseline generated from the **original** code | Golden master and ThirtyDays green. `GildedRoseTest.foo` still red (placeholder). |
| T2 | Replace `GildedRoseTest.foo` with the business-rule tests R1–R9 | All green |
| T3 | Add `LegacyBehaviourTests` L1–L5 | All green |

All three must be green and committed before the first production-code
change. Final size: 2 approval tests and 14 named tests (≈ 40 test cases in
the runner output, since each `InlineData` row is reported separately).
