# Steering file: Gilded Rose refactoring (C#)

This file defines how AI assistants work in this repository. It is part of the
submitted artefacts. Rules here take precedence over anything the AI "thinks
would be nicer".

## Goal

Refactor the legacy `GildedRose.UpdateQuality()` in `csharp.xUnit/` into clean,
readable, well-tested code **without changing its behaviour**. The existing
implementation is the source of truth, including its quirks. If a requirement
is ambiguous, the code decides.

## Hard rules

1. **Never modify the `Item` class** (name, fields, properties, constructor).
   The `GildedRose` class keeps its public API: constructor taking
   `IList<Item>` and `UpdateQuality()`.
2. **No behaviour change during refactoring.** Do not fix bugs or odd edge
   cases. If you find one, report it to me and I will decide; pin it with a
   test and note it in the README.
3. **Tests come first.** No production code is changed until the
   characterization (golden master) tests exist and pass against the
   *original* code.
4. **One small step at a time.** One refactoring (or one test addition) per
   step, then stop and show me the diff. Do not batch several refactorings.
5. **Run the tests after every step** (`dotnet test` from `csharp.xUnit/`) and show
   the real output. Do not claim tests pass without running them.
6. **Do not edit or delete a failing test to make it pass.** A red golden
   master means the refactoring changed behaviour: revert the step.
7. **No commits by the AI.** I review each diff and commit myself, one
   refactoring per commit, with a clear message.

## Design guidance

- Keep it proportionate. Prefer the simplest design that removes the nested
  conditionals and makes adding a new item type a small change.
- Acceptable direction: a small per-category update rule (e.g. an
  `IItemUpdater` or similar) chosen by item name, with the shared quality
  clamping in one place. Alternatives (data-driven rules, switch expression)
  are fine if simpler.
- Do **not** introduce frameworks, DI containers, generics, reflection,
  abstract factories, or extra projects unless I ask for them. If you think
  an abstraction is needed, justify it in one or two sentences first and wait
  for my approval.
- Use idiomatic modern C# (PascalCase, `sealed` where sensible, file-scoped
  namespaces only if the project already targets a version that supports them),
  but match the existing project's target framework and style.
- No dead code, no commented-out code, no unnecessary comments. Names should
  carry the meaning.

## Testing guidance

- Golden master: generate expected output from the **original** code across
  all item names, sell-in values around every threshold (negative, 0, 1, 5,
  6, 10, 11) and quality values around the boundaries (0, 1, 49, 50, 80),
  over multiple days. Store the baseline in the repo.
- Add a few readable, named unit tests for the key rules; they serve as
  documentation.
- Mutation testing (Stryker.NET) is run at the end to check that the tests
  would actually catch regressions. Report survivors; do not just silence them.

## Conjured items

Adding "Conjured" behaviour is a *feature*, not part of the refactoring.
Only do it after the refactoring is complete and green, test-first, in
separate commits, and only when I ask.

## Working with me (the developer)

- I am **experienced in software engineering but new to C#**. When you use a
  C# idiom or .NET tool I may not know (properties, `switch` expressions,
  `IList` vs `List`, xUnit attributes, `dotnet` CLI), add a one-line
  explanation. Do not assume I will accept code I cannot explain.
- Before each step, state in one or two sentences what you are about to do and
  why. After each step, state what changed and what you verified.
- If something is unclear, ask a single focused question instead of guessing.
- If you disagree with a rule in this file, say so rather than working around it.

## Artefacts

Session transcripts and my decision log (where I accepted, rejected or
simplified AI output) live in `docs/ai/`.
