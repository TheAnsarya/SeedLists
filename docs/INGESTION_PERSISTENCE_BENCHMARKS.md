# Ingestion Persistence Benchmarks

Issues: `#38`, `#39`, `#40`, `#42`

This document tracks BenchmarkDotNet coverage for ingestion persistence overhead (source hierarchy writes, hashing, SQLite ledger inserts, normalized JSON DB persistence).

## Covered Scenarios

- `SyncSingleDat_WithSqlitePersistence`
- `SyncTenDats_WithSqlitePersistence`

## Run Command

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project benchmarks/SeedLists.Benchmarks/SeedLists.Benchmarks.csproj -c Release -- --filter "*IngestionPersistenceBenchmark*" --job short
```

## Baseline Snapshot (2026-03-11)

| Scenario | Mean | Allocated |
|---|---:|---:|
| SyncSingleDat_WithSqlitePersistence | 39.75 ms | 65.79 KB/op |
| SyncTenDats_WithSqlitePersistence | 61.00 ms | 376.25 KB/op |

## Guardrails

| Scenario | Allocation Budget | Timing Budget |
|---|---|---|
| Single DAT persistence | <= 128 KB/op | <= 60 ms/op |
| Ten DAT persistence batch | <= 512 KB/op | <= 90 ms/op |

If guardrails are exceeded:

1. Capture benchmark output in `~docs/session-logs/`.
2. Open/update a performance issue with before/after metrics.
3. Land allocation/timing improvements with benchmark evidence.
