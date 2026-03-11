# Plan: Epic #38 - SQLite Ingestion Ledger and Normalized Catalog Persistence

## Issues

- `#38` - `[Epic 4] SQLite ingestion ledger and normalized catalog persistence`
- `#39` - `[4.1] Implement SQLite ingestion ledger for all DAT sync operations`
- `#40` - `[4.2] Persist normalized catalogs in SQLite and file hierarchy`
- `#41` - `[4.3] Add correctness tests for ingestion DB and artifact persistence`
- `#42` - `[4.4] Benchmark ingestion persistence overhead and allocation impact`
- `#43` - `[4.5] Document SQLite ingestion storage model and operator workflow`

## Goals

- Persist every successful DAT ingestion into SQLite with required metadata and hashes.
- Persist normalized catalog JSON in SQLite while preserving normalized/summary JSON files.
- Persist source ingested payload files into deterministic folder hierarchy.
- Add correctness tests and benchmark evidence with guardrails.

## Implementation Phases

- Phase 1: Add SQLite storage dependency and ingestion DB path option.
- Phase 2: Implement `DatIngestionLedgerStore` with idempotent schema creation and record writes.
- Phase 3: Integrate source hierarchy saves + hash capture + DB writes in `DatCollectionService`.
- Phase 4: Add tests validating DB rows, normalized catalog storage, and source hierarchy artifacts.
- Phase 5: Add benchmark coverage for persistence path overhead and update docs.

## Validation

- `dotnet test tests/SeedLists.Dat.Tests/SeedLists.Dat.Tests.csproj -c Release`
- `dotnet run --project benchmarks/SeedLists.Benchmarks/SeedLists.Benchmarks.csproj -c Release -- --filter "*IngestionPersistenceBenchmark*" --job short`
- `pwsh -File scripts/test-markdown-policy.ps1`

## Notes

- Relative `IngestionDatabasePath` resolves under `OutputDirectory`.
- Source hierarchy path pattern: `{OutputDirectory}/{provider}/ingested-sources/{system}/{yyyy}/{MM}/{dd}/`.
