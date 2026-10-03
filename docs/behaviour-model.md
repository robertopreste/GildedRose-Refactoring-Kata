# Gilded Rose: behaviour model of the legacy `UpdateQuality()`

Source of truth: `csharp.xUnit/GildedRose/GildedRose.cs` (unmodified). Every
value in this document was cross-checked by running the original code over the
grid `SellIn ∈ {-1, 0, 1, 5, 6, 10, 11}` × `Quality ∈ {-1, 0, 1, 2, 47, 48, 49, 50, 51, 80}`
for each item name below.

## 1. How the algorithm is structured

For each item, independently and in list order, `UpdateQuality()` runs three
phases:

| Phase | What happens | Uses `SellIn` value |
|---|---|---|
| **A. Daily quality change** | Brie/Backstage: `+1` if `Quality < 50` (Backstage adds up to two more bonus points). Others: `-1` if `Quality > 0`, except Sulfuras. | *before* decrement |
| **B. Age** | `SellIn -= 1`, except Sulfuras. | – |
| **C. Expired adjustment** | Only if `SellIn < 0` **after** phase B. Brie: another `+1` if `< 50`. Backstage: `Quality = 0`. Others: another `-1` if `> 0`, except Sulfuras. | *after* decrement |

Consequences:

- An item is "expired" for the current update when its **starting** `SellIn <= 0`.
  So an item with `SellIn = 0` already gets the double rate on that update.
- The `0` floor and the `50` cap are **guards on each ±1 step**, not clamps.
  The code never pulls an out-of-range value back into range. It only refuses
  to step further past the boundary.

## 2. Item categories and how they are recognised

The code matches items by **exact, case-sensitive string equality** on the
full name. Prefixes and partial matches are not recognised.

| Category | Recognised name (exact) | Anything else with a similar name |
|---|---|---|
| Aged Brie | `Aged Brie` | `aged brie` → Normal |
| Backstage pass | `Backstage passes to a TAFKAL80ETC concert` | `Backstage passes to another concert` → Normal |
| Sulfuras | `Sulfuras, Hand of Ragnaros` | `Sulfuras` → Normal |
| Normal | any other name, including `null` | – |
| *(Conjured)* | *not implemented*: `Conjured Mana Cake` → Normal | – |

## 3. Rules per category

Notation: `s` = starting `SellIn`, `q` = starting `Quality`.

### 3.1 Normal items

| Condition | ΔSellIn | ΔQuality |
|---|---|---|
| `s >= 1` | −1 | −1, not below 0 |
| `s <= 0` | −1 | −2, each step not below 0 |
| `q <= 0` (any `s`) | −1 | unchanged (a negative `q` stays negative) |
| `q > 50` (e.g. 80) | −1 | decreases normally from that value (no cap applied) |

Boundary examples: `(s=0, q=1) → q=0`, `(s=0, q=2) → q=0`, `(s=1, q=1) → q=0`, `(s=0, q=80) → q=78`.

### 3.2 Aged Brie

| Condition | ΔSellIn | ΔQuality |
|---|---|---|
| `s >= 1` | −1 | +1, not above 50 |
| `s <= 0` | −1 | **+2**, each step not above 50 |
| `q >= 50` | −1 | unchanged (a `q` of 51 or 80 stays as is) |
| `q < 0` | −1 | increases normally (`-1 → 0` or `-1 → 1`) |

Boundary examples: `(s=0, q=49) → 50`, `(s=0, q=48) → 50`, `(s=1, q=49) → 50`.

### 3.3 Backstage passes (`… TAFKAL80ETC concert`)

The increase uses the `SellIn` **before** decrement:

| Condition | ΔSellIn | ΔQuality |
|---|---|---|
| `s >= 11` | −1 | +1 |
| `6 <= s <= 10` | −1 | +2 |
| `1 <= s <= 5` | −1 | +3 |
| `s <= 0` | −1 | **set to 0** (whatever `q` was, including negative or 80) |
| `q >= 50` and `s >= 1` | −1 | unchanged (51 or 80 stays) |

Each +1 step is guarded separately by `< 50`, so the gain is truncated at 50:
`(s=5, q=48) → 50`, `(s=5, q=49) → 50`, `(s=10, q=49) → 50`.
Negative `q` increases normally: `(s=5, q=-1) → 2`.

### 3.4 Sulfuras, Hand of Ragnaros

| Condition | ΔSellIn | ΔQuality |
|---|---|---|
| any | 0 | 0 |

`SellIn` is never decremented, even when negative. `Quality` is **not** forced
to 80. Whatever value it has (−1, 0, 50, 80…) is preserved.

## 4. Unusual behaviour (spec vs code) and quirks to pin

These must be **preserved** during refactoring (AGENTS.md rule 2). Each one
needs a pinning test.

| # | Quirk | Spec says | Code does |
|---|---|---|---|
| Q1 | Exact-name matching | "Sulfuras", "Backstage passes", "Aged Brie" read like categories | Only the three exact full strings are special. Case and wording variants are Normal. |
| Q2 | Conjured not implemented | Conjured degrades ×2 | `Conjured Mana Cake` behaves as Normal (expected, since this is the future feature) |
| Q3 | Brie gains ×2 after sell date | Spec only says Brie "increases" | +2/day once `s <= 0` |
| Q4 | Expiry judged after decrement | "once the sell by date has passed" | `s = 0` already counts as expired on this update |
| Q5 | Bounds are guards, not clamps | Quality is never negative / never > 50 | Out-of-range input persists: Normal `q=80` decays from 80, Brie `q=80` stays 80, Normal `q=-1` stays −1 |
| Q6 | Backstage ≥ 50 frozen until concert | – | `q=80` Backstage stays 80, then drops to 0 |
| Q7 | Backstage reset ignores sign | "drops to 0 after the concert" | `q - q` turns a negative quality into 0 as well |
| Q8 | Backstage thresholds on pre-decrement `SellIn` | "10 days or less", "5 days or less" | Implemented as `s < 11` and `s < 6` before ageing (matches spec when read as "days left at start of day") |
| Q9 | Sulfuras quality not enforced | Sulfuras is always 80 | Any value is accepted and preserved |
| Q10 | Sulfuras `SellIn` frozen | "never has to be sold" | `SellIn` never changes |
| Q11 | `null` name | – | Treated as Normal (C# `!=` on strings is null-safe) |
| Q12 | `int.MinValue` `SellIn` | – | Unchecked overflow wraps to `int.MaxValue`, so only a −1 quality change. Extreme edge, document only. |
| Q13 | Same `Item` instance twice in the list | – | Updated twice per call. Items are processed independently and in order. |
| Q14 | `null` element in the list | – | `NullReferenceException` (not probed, follows from `Items[i].Name`) |

Q11–Q14 are pathological inputs. I suggest documenting them, and pinning Q11
and Q13 only if you want the refactoring to keep exact parity. **Your call.**

## 5. Behaviour matrix (one-day transitions, for test design)

Each row is a candidate test case: input `(Name, SellIn, Quality)` and the
expected values after **one** `UpdateQuality()`. All of these come from the
probe of the original code.

### Normal (`+5 Dexterity Vest`; same for `Elixir of the Mongoose`, `Conjured Mana Cake`, …)

| ID | s | q | → s | → q | Rule exercised |
|---|---|---|---|---|---|
| N1 | 10 | 20 | 9 | 19 | standard −1 |
| N2 | 1 | 1 | 0 | 0 | last day before expiry, −1 |
| N3 | 0 | 10 | −1 | 8 | expiry day, −2 (Q4) |
| N4 | −1 | 10 | −2 | 8 | already expired, −2 |
| N5 | 5 | 0 | 4 | 0 | floor at 0 |
| N6 | 0 | 1 | −1 | 0 | floor reached mid-update |
| N7 | 0 | 2 | −1 | 0 | exact −2 to floor |
| N8 | 5 | 80 | 4 | 79 | no cap on decay (Q5) |
| N9 | 5 | −1 | 4 | −1 | negative persists (Q5) |
| N10 | 5 | 51 | 4 | 50 | above-cap value decays normally |

### Aged Brie

| ID | s | q | → s | → q | Rule exercised |
|---|---|---|---|---|---|
| B1 | 2 | 0 | 1 | 1 | +1 |
| B2 | 1 | 10 | 0 | 11 | last day before expiry, +1 |
| B3 | 0 | 10 | −1 | 12 | +2 after sell date (Q3, Q4) |
| B4 | −1 | 10 | −2 | 12 | +2 when expired |
| B5 | 5 | 49 | 4 | 50 | reaches cap |
| B6 | 5 | 50 | 4 | 50 | at cap |
| B7 | 0 | 49 | −1 | 50 | +2 truncated at cap |
| B8 | 0 | 48 | −1 | 50 | +2 lands exactly on cap |
| B9 | 5 | 80 | 4 | 80 | above cap preserved (Q5) |
| B10 | 5 | −1 | 4 | 0 | negative increases |

### Backstage passes (`… TAFKAL80ETC concert`)

| ID | s | q | → s | → q | Rule exercised |
|---|---|---|---|---|---|
| P1 | 15 | 20 | 14 | 21 | far: +1 |
| P2 | 11 | 20 | 10 | 21 | boundary 11: still +1 (Q8) |
| P3 | 10 | 20 | 9 | 22 | boundary 10: +2 |
| P4 | 6 | 20 | 5 | 22 | boundary 6: +2 |
| P5 | 5 | 20 | 4 | 23 | boundary 5: +3 |
| P6 | 1 | 20 | 0 | 23 | last day: +3 |
| P7 | 0 | 20 | −1 | 0 | concert passed: 0 |
| P8 | −1 | 20 | −2 | 0 | stays 0 |
| P9 | 10 | 49 | 9 | 50 | +2 truncated at cap |
| P10 | 5 | 49 | 4 | 50 | +3 truncated at cap |
| P11 | 5 | 48 | 4 | 50 | +3 truncated at cap |
| P12 | 5 | 50 | 4 | 50 | at cap |
| P13 | 5 | 80 | 4 | 80 | above cap frozen (Q6) |
| P14 | 0 | 80 | −1 | 0 | above cap reset at concert (Q6) |
| P15 | 5 | −1 | 4 | 2 | negative increases by 3 |
| P16 | 0 | −1 | −1 | 0 | negative reset to 0 (Q7) |

### Sulfuras, Hand of Ragnaros

| ID | s | q | → s | → q | Rule exercised |
|---|---|---|---|---|---|
| S1 | 0 | 80 | 0 | 80 | unchanged |
| S2 | −1 | 80 | −1 | 80 | expired: unchanged, SellIn frozen (Q10) |
| S3 | 5 | 80 | 5 | 80 | unchanged |
| S4 | 5 | 10 | 5 | 10 | non-80 quality preserved (Q9) |

### Name-matching (Q1, Q2)

| ID | Name | s | q | → s | → q | Behaves as |
|---|---|---|---|---|---|---|
| M1 | `aged brie` | 5 | 10 | 4 | 9 | Normal |
| M2 | `Backstage passes to another concert` | 5 | 10 | 4 | 9 | Normal |
| M3 | `Sulfuras` | 5 | 10 | 4 | 9 | Normal |
| M4 | `Conjured Mana Cake` | 3 | 6 | 2 | 5 | Normal (current behaviour) |

## 6. Golden-master input space (proposal for the next step)

As required by AGENTS.md, the golden master should be the full cross product:

- **Names**: the 3 special names, 2 normal names (`+5 Dexterity Vest`,
  `Elixir of the Mongoose`), `Conjured Mana Cake`, and the near-miss names from M1–M3.
- **SellIn**: −1, 0, 1, 5, 6, 10, 11 (I suggest adding 2 and 12 so that
  multi-day runs cross every threshold from both sides).
- **Quality**: 0, 1, 49, 50, 80 (I suggest adding −1, 2, 48 and 51 to cover
  Q5/Q7 and the truncation cases).
- **Days**: simulate e.g. 15 days per combination and record every day's
  state. Starting at `SellIn = 11`, this crosses the 11→10, 6→5 and 0→−1
  boundaries.

## 7. Current test state (baseline, original code)

`dotnet test` in `csharp.xUnit/`: **2 failed, 1 passed** out of 3.

| Test | Status | Why |
|---|---|---|
| `GildedRoseTest.foo` | ❌ fails | Kata placeholder: asserts `Name == "fixme"` |
| `ApprovalTest.Foo` | ❌ fails | No `.verified.txt` baseline approved yet (only `.received.txt`) |
| `ApprovalTest.ThirtyDays` | ✅ passes | Snapshot of `Program.Main` over 30 days: 9 items only, so it misses most of the quirks above |

Things to know before writing the golden master:

- **Verify omits default values.** `ApprovalTest.Foo.received.txt` has no
  `Quality` line because `Quality` was `0`. A golden master that renders its
  own plain-text lines (as `ThirtyDays` does) avoids this.
- The two failing placeholders need a decision before the suite can go green.
  AGENTS.md rule 6 forbids editing failing tests to make them pass, but these
  are kata scaffolding, not characterization tests. I propose replacing them
  as part of the "tests first" step, with your explicit approval.
