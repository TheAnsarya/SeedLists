# Plan: Epic #33 - Pleasuredome DAT Source Integration

## Issues

- `#33` - `[Epic 3] Pleasuredome DAT source integration`
- `#34` - `[3.4] Implement Pleasuredome remote source discovery and ingestion`
- `#35` - `[3.5] Add Pleasuredome provider correctness and resilience tests`
- `#36` - `[3.6] Add Pleasuredome parsing performance and allocation benchmarks`
- `#37` - `[3.7] Document Pleasuredome setup, runbook, and operational guidance`

## Goals

- Add a first-class Pleasuredome provider to SeedLists ingestion.
- Cover MAME and selected NonMAME categories (`fruitmachines`, `pinball`, `raine`).
- Keep remote polling safe with change detection and configurable cadence.
- Deliver validation artifacts: tests, benchmark results, and docs.

## Implementation Phases

- Phase 1: Add provider surface (`DatProviderKind`, DI, worker defaults, schema/validation enums).
- Phase 2: Implement `PleasureDomeProvider` with local fallback and remote page discovery.
- Phase 3: Add tests for discovery correctness, token-change behavior, zip extraction, and remote failure fallback.
- Phase 4: Add benchmark coverage for Pleasuredome discovery throughput and allocations.
- Phase 5: Update README, source/runbook docs, and dedicated provider instructions.

## Validation

- `dotnet test tests/SeedLists.Dat.Tests/SeedLists.Dat.Tests.csproj -c Release`
- `dotnet run --project benchmarks/SeedLists.Benchmarks/SeedLists.Benchmarks.csproj -c Release -- --filter "*PleasureDomeDiscoveryBenchmark*" --job short`
- `pwsh -File scripts/test-markdown-policy.ps1`

## Operational Notes

- Polling cadence is controlled by `SeedListsDat:RemotePollIntervalHours`.
- Changed-only remote listings require `SeedListsDat:EnableRemoteVersionChecks = true`.
- NonMAME scope can be tightened by editing `SeedListsDat:PleasureDomeNonMameCategorySlugs`.
