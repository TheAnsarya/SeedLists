# Normalization Benchmarks

This document tracks BenchmarkDotNet coverage and allocation budgets for normalization paths in `CatalogNormalizationService`.

## Covered Scenarios

- JSON passthrough (`NormalizeJsonPassthrough`)
- TOSEC XML-like mapper (`NormalizeTosecXmlLike`)
- No-Intro XML-like mapper (`NormalizeNoIntroXmlLike`)
- GoodTools text mapper (`NormalizeGoodToolsText`)

## Run Command

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project benchmarks/SeedLists.Benchmarks -c Release -- --filter *CatalogNormalizationBenchmark*
```

## Baseline Snapshot (2026-03-06)

Captured with:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project benchmarks/SeedLists.Benchmarks -c Release -- --filter *CatalogNormalizationBenchmark* --warmupCount 1 --iterationCount 3
```

| Scenario | Mean | Allocated |
|---|---:|---:|
| JSON passthrough | 1.971 us | 1.81 KB/op |
| GoodTools mapper | 15.363 us | 7.21 KB/op |
| No-Intro mapper | 17.313 us | 13.5 KB/op |
| TOSEC mapper | 21.176 us | 13.41 KB/op |

## Allocation Budgets

Use the `Allocated` column from BenchmarkDotNet output and treat these as regression guardrails.

| Scenario | Allocation Budget |
|---|---|
| JSON passthrough | <= 4 KB/op |
| TOSEC mapper | <= 20 KB/op |
| No-Intro mapper | <= 20 KB/op |
| GoodTools mapper | <= 10 KB/op |

If a scenario crosses its budget:

1. Capture the benchmark summary in `~docs/session-logs/`.
2. Open a performance issue with benchmark output and suspected hot path.
3. Land fixes with before/after benchmark evidence.

