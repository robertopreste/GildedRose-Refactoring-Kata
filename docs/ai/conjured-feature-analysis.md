# Conjured items: feature analysis

- **Status:** analysis complete, decisions recorded. No production or test code changed yet.
- **Code at the time of analysis:** commit `c468a41` (refactoring complete, mutation testing documented).
- **Date:** 2026-10-03

Conjured support is a **feature**, not part of the refactoring (AGENTS.md).
The characterised behaviour for Conjured items, which today degrade like normal
items, will change on purpose. The resulting test and golden-master changes
are expected and are **not regressions**.

## 1. What the sources say

| Source | Statement |
|---|---|
| `GildedRoseRequirements.md:26-28` | "We have recently signed a supplier of conjured items… **"Conjured" items degrade in `Quality` twice as fast as normal items**" |
| `GildedRoseRequirements.md` (general rules, still apply) | `SellIn` decreases daily; "Once the sell by date has passed, Quality degrades twice as fast"; "The Quality of an item is never negative" |
| `AGENTS.md` | Conjured is a feature: test-first, in separate commits, only after the refactoring is complete and green |
| `csharp.xUnit/GildedRose/Program.cs:37-38` | The only example name is `Conjured Mana Cake`, with the comment "this conjured item does not work properly yet" |
| `docs/behaviour-model.md` (Q2, M4), `csharp.xUnit/README.md`, `LegacyBehaviourTests` (L3) | Pin the current behaviour: Conjured degrades like a normal item |

That one requirement line is the entire Conjured specification. It defines
Conjured items only **relative to normal items**.

## 2. Expected quality behaviour

"Twice as fast as normal items", applied to each normal-item rule:

| Condition (start of day) | Normal item today | Conjured | Example (`SellIn`, `Quality` → after one day) |
|---|---|---|---|
| Before the sell-by date (`SellIn >= 1`) | −1 | **−2** | (10, 20) → (9, 18) |
| Sell-by date passed (`SellIn <= 0`, same as normal items, Q4) | −2 | **−4** | (0, 20) → (−1, 16) |
| Close to 0 | stops at 0 | **stops at 0** | (5, 1) → (4, 0); (0, 3) → (−1, 0) |
| `SellIn` | −1 | **−1** (Conjured items are not legendary) | |

**Implementation consequence.** Call the existing guarded `DecreaseQuality`
twice where a normal item calls it once. This gives the 0 floor with no new
limit logic. It also means Conjured items follow the same "a guard, not a
clamp" behaviour as normal items when quality is outside 0..50 (see §4).

Sketch:

```csharp
case string name when name.StartsWith("Conjured", StringComparison.Ordinal):
    DecreaseQuality(item);
    DecreaseQuality(item);
    item.SellIn = item.SellIn - 1;
    if (item.SellIn < 0)
    {
        DecreaseQuality(item);
        DecreaseQuality(item);
    }
    break;
```

This `case` goes after the three exact-name cases and before `default`.

## 3. Which items count as "Conjured"

The spec says **"Conjured" items**, a category from a supplier, which suggests
many names. The existing categories match exact full names (Q1).

| Option | Matches | Effect |
|---|---|---|
| **A. Name starts with `"Conjured"`, case-sensitive** | `Conjured Mana Cake`, `Conjured Sword`, … | Fits "a supplier of conjured items" |
| B. Exact name `"Conjured Mana Cake"` | only that item | Same style as the existing categories, but every new Conjured product needs a code change |

**Decision: option A** (developer, 2026-10-03).

C# notes:

- **`case string name when …`** is a *type pattern with a guard*. It is used
  instead of `case var name when …` because a `var` pattern also matches
  `null`, and `name.StartsWith` would then throw for a `null` name. Today a
  `null` name behaves as a normal item (Q11), and the type pattern keeps that.
- **`StringComparison.Ordinal`** makes the match an exact character
  comparison. By default, `StartsWith(string)` uses culture rules, which can
  vary by machine settings.

## 4. Edge cases of option A

These follow from the rule "name starts with `Conjured` (ordinal,
case-sensitive)" combined with the `case` order in §2. Exact-name cases are
checked first, then the Conjured prefix, then `default`.

### 4.1 Combinations with special items

The spec defines Conjured only relative to *normal* items. It says nothing
about conjured versions of Aged Brie, Backstage passes or Sulfuras. Under
option A, the special categories still require their **exact full name**
(Q1). Any name that starts with `Conjured` therefore never matches them, and
is treated as a plain Conjured item that **degrades**:

| Name | Treated as | Day result from (5, 10) | Notably *not* |
|---|---|---|---|
| `Conjured Aged Brie` | Conjured | (4, 8) | does not increase with age |
| `Conjured Backstage passes to a TAFKAL80ETC concert` | Conjured | (4, 8); from (0, 10): (−1, 6) | no +2/+3 tiers, no drop to 0 after the concert |
| `Conjured Sulfuras, Hand of Ragnaros` | Conjured | (4, 8) | not legendary: `SellIn` and `Quality` both change |

This is a consequence of the existing exact-name matching, not a choice made
for this feature. If the inn ever stocks conjured special items, the intended
behaviour (e.g. "Conjured Aged Brie increases twice as fast"?) is a **product
decision** that the spec does not answer, and it would be a separate feature.

### 4.2 Name matching

| Name | Treated as | Why |
|---|---|---|
| `Conjured Mana Cake`, `Conjured Sword` | Conjured | prefix match |
| `Conjured` (exactly) | Conjured | a string starts with itself |
| `ConjuredSword` (no space) | Conjured | the prefix has no word-boundary requirement |
| `conjured mana cake`, `CONJURED Sword` | Normal | ordinal comparison is case-sensitive, consistent with Q1 |
| ` Conjured Mana Cake` (leading space) | Normal | the name does not start with `C` |
| `Mana Cake (Conjured)`, `Extra Conjured Cake` | Normal | `Conjured` is not at the start |
| `null` | Normal | the type pattern `string name` does not match `null` (Q11 preserved) |
| `""` (empty) | Normal | an empty string does not start with `Conjured` |

### 4.3 Quality outside 0..50

Conjured items reuse the guarded `DecreaseQuality`, so out-of-range values
behave like normal items (Q5): each step is skipped at 0, and nothing is
pulled back into range.

| Start (`SellIn`, `Quality`) | After one day |
|---|---|
| (5, 80) | (4, 78) |
| (0, 80) | (−1, 76) |
| (5, 51) | (4, 49) |
| (5, −1) | (4, −1) |
| (5, 0) | (4, 0) |

The spec does not cover these inputs. The golden master records the
`Conjured Mana Cake` trajectories for quality −1, 0, 80 and the rest of the
grid after re-approval. No named tests are proposed for them.

### 4.4 Which edge cases get named tests

- **Tested:** `Conjured Sword` (other Conjured names) and `Conjured Aged Brie`
  (precedence of the special categories). Both are rows of C5 in §6.
- **Documented only:** the rest of §4.2 and §4.3. They follow mechanically
  from `StartsWith` + `Ordinal` and from reusing `DecreaseQuality`, and none
  is a scenario the inn has today.

## 5. Impact on existing tests, baselines and docs

**Expected to change (the feature, not a regression):**

| Artefact | Change | Size |
|---|---|---|
| `LegacyBehaviourTests.ConjuredItem_CurrentlyDegradesLikeNormalItem` (L3) | Fails by design. Delete it in the implementation commit; the new business-rule tests replace it. | 1 test, 2 cases |
| `ApprovalTest.GoldenMaster.verified.txt` | Re-approve. Only `Conjured Mana Cake` lines may change. | **38 of 512 lines** |
| `ApprovalTest.ThirtyDays.verified.txt` | Re-approve | **4 lines** (see below) |
| `csharp.xUnit/README.md`, "Known legacy behaviour" | Remove the Conjured row | 1 row |
| `docs/behaviour-model.md` (Q2, M4) | Mark as superseded by the feature | 2 entries |
| `csharp.xUnit/GildedRose/Program.cs:37` comment "this conjured item does not work properly yet" | Becomes false: remove | 1 line |

**Golden master detail.** The counts come from simulating the new rule
(guarded `DecreaseQuality` ×2, ×4 when expired) against the approved baseline.

- 38 of the 64 `Conjured Mana Cake` lines change.
- The other 26 stay byte-identical, because the old and new rules reach the
  same values:
  - every line starting at quality −1, 0 or 1 (8 `SellIn` values each)
  - quality 2 with `SellIn` −1 or 0
- The remaining 448 lines (all other names) must stay byte-identical. None of
  the other golden-master names starts with `Conjured`, so option A affects
  only `Conjured Mana Cake`.

**ThirtyDays detail.** Only the `Conjured Mana Cake` line on days 1–4 changes.
From day 5 both rules give `…, 0`.

| Day | Today | With Conjured |
|---|---|---|
| 1 | `2, 5` | `2, 4` |
| 2 | `1, 4` | `1, 2` |
| 3 | `0, 3` | `0, 0` |
| 4 | `-1, 1` | `-1, 0` |

**Re-approval check.** Before approving either snapshot, diff the received
file against the verified one and confirm that **only** the lines listed above
changed, with exactly the predicted values. Any other difference is a
regression and blocks the approval. This is what keeps re-approval from being
a blind accept.

**Unchanged:** R1–R9 (except for the rows added below), L1, L2, L4, L5, and
the other 474 golden-master lines.

## 6. Tests to write before changing production code

All in `GildedRoseTest.cs`, since Conjured becomes a business rule. These
tests are committed first, while they still fail, and the implementation
follows in a separate commit.

| # | Test | Rule documented | Cases `(name, SellIn, Quality → expected)` | Red before implementation? |
|---|---|---|---|---|
| C1 | `ConjuredItem_QualityDegradesTwiceAsFastAsNormalItem_BeforeSellByDate` (new) | "twice as fast as normal items" | (10, 20 → 18), (1, 20 → 18) | ❌ both |
| C2 | `ConjuredItem_QualityDegradesTwiceAsFastAsNormalItem_OnceSellByDateHasPassed` (new) | the same rule after the sell-by date: 2 × 2 | (0, 20 → 16), (−1, 20 → 16) | ❌ both |
| C3 | `Quality_IsNeverNegative` (existing R4): add a `name` parameter and two Conjured rows | "never negative" applies to the faster decrease | (Conjured, 5, 1 → 0), (Conjured, 0, 3 → 0) | (0, 3) ❌; (5, 1) ✅, which guards against an implementation that subtracts 2 directly |
| C4 | `SellIn_DecreasesByOneEachDay` (existing R1): add one Conjured row | `SellIn` still decreases by 1 | `Conjured Mana Cake` | ✅, which guards against a Conjured case that forgets the decrement |
| C5 | `ConjuredItem_IsRecognisedByNamePrefix` (new) | which items count as Conjured (§3, §4.1) | (`Conjured Sword`, 5, 10 → 8), (`Conjured Aged Brie`, 5, 10 → 8) | ❌ both (today `Conjured Sword` → 9 and `Conjured Aged Brie` → 9 as normal items) |

C3 changes an existing test's parameters but keeps its existing rows,
following the rule-centred style of `Quality_NeverIncreasesAbove50`. That's 3
new tests, 2 extended tests, and 9 new cases in total, 7 of which are red
before the implementation.
