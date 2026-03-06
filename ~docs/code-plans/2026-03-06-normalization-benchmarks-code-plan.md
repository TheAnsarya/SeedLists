# Code Plan: Issue #14 Normalization Benchmark Coverage

## Objective

Add BenchmarkDotNet coverage for `CatalogNormalizationService` paths and define allocation guardrails for regression tracking.

## Scope

- Extend `benchmarks/SeedLists.Benchmarks/Program.cs` with a dedicated normalization benchmark class.
- Keep existing parser benchmark and use benchmark switcher so both benchmark classes remain runnable.
- Add a benchmark operations doc with command examples and allocation budgets.

## Implementation Steps

1. Replace single benchmark runner entrypoint with `BenchmarkSwitcher` to discover all benchmark classes.
2. Add `CatalogNormalizationBenchmark` with scenario methods:
	- `NormalizeJsonPassthrough`
	- `NormalizeTosecXmlLike`
	- `NormalizeNoIntroXmlLike`
	- `NormalizeGoodToolsText`
3. Build deterministic sample payload generators for each scenario.
4. Add docs for execution and budget guardrails.

## Validation

- Build benchmark project in Release.
- Run normalization benchmark filter and verify all four scenarios execute.
- Ensure `MemoryDiagnoser` output includes allocation metrics.
