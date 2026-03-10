# Parser Benchmarks

This document tracks parser throughput and allocations for `StreamingJsonDatParser`.

## Covered Scenarios

- Small catalog parse (`ParseSmallDatAsync`) - 1 game x 1 rom
- Medium catalog parse (`ParseMediumDatAsync`) - 80 games x 4 roms
- Large catalog parse (`ParseLargeDatAsync`) - 400 games x 6 roms
- Pleasuredome discovery parse (`DiscoverPleasureDomeDatLinks`) - MAME + NonMAME page link extraction
- Pleasuredome discovery parse with token checks (`DiscoverPleasureDomeDatLinksWithVersionChecks`)

## Run Command

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project benchmarks/SeedLists.Benchmarks -c Release -- --filter *JsonDatParserBenchmark*
```

Pleasuredome discovery benchmark:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project benchmarks/SeedLists.Benchmarks -c Release -- --filter *PleasureDomeDiscoveryBenchmark* --job short
```

## Baseline Snapshot (2026-03-06)

Captured with:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project benchmarks/SeedLists.Benchmarks -c Release -- --filter *JsonDatParserBenchmark* --warmupCount 1 --iterationCount 3
```

| Scenario | Mean | Allocated |
|---|---:|---:|
| Small (1x1) | 2.126 us | 2.4 KB/op |
| Medium (80x4) | 256.064 us | 268.32 KB/op |
| Large (400x6) | 2.111 ms | 2036.45 KB/op |

## Pleasuredome Discovery Snapshot (2026-03-10)

Captured with:

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project benchmarks/SeedLists.Benchmarks/SeedLists.Benchmarks.csproj -c Release -- --filter "*PleasureDomeDiscoveryBenchmark*" --job short
```

| Scenario | Mean | Allocated |
|---|---:|---:|
| DiscoverPleasureDomeDatLinks | 1.089 ms | 816.12 KB/op |
| DiscoverPleasureDomeDatLinksWithVersionChecks | 1.274 ms | 1019.15 KB/op |

## Allocation Budgets

| Scenario | Allocation Budget |
|---|---|
| Small | <= 6 KB/op |
| Medium | <= 350 KB/op |
| Large | <= 2.5 MB/op |
| Pleasuredome discovery | <= 1.2 MB/op |

If any benchmark exceeds budget:

1. Capture benchmark output in `~docs/session-logs/`.
2. Open a performance issue with before/after numbers.
3. Ship optimization changes with benchmark evidence.
