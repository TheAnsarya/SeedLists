# SeedLists

SeedLists is a .NET 10 toolkit for ingesting, downloading, parsing, and normalizing ROM DAT files.

This repository is JSON-first: internal parsing and normalization use JSON catalog payloads.

## What It Includes

- `src/SeedLists.Dat`: reusable DAT library (providers, parsing, sync service)
- `src/SeedLists.Worker`: background worker that executes periodic provider syncs
- `tests/SeedLists.Dat.Tests`: unit tests for parsing and policy logic
- `benchmarks/SeedLists.Benchmarks`: parser and normalization performance benchmark project
- `scripts/`: provider-specific helper scripts

## Providers

- TOSEC: local folder and optional internet discovery/download
- GoodTools: local collection ingestion
- No-Intro: local ingestion plus guarded remote mode with a mandatory 24-hour cooldown (unless testing override is enabled)

## Quick Start

```powershell
# build
& "C:\Program Files\dotnet\dotnet.exe" build SeedLists.slnx

# test
& "C:\Program Files\dotnet\dotnet.exe" test SeedLists.slnx

# run worker
& "C:\Program Files\dotnet\dotnet.exe" run --project src/SeedLists.Worker
```

## Scripts

- `scripts/download-tosec-dats.ps1`
- `scripts/download-goodtools-dats.ps1`
- `scripts/download-nointro-dats.ps1`

No-Intro script behavior:

- rejects remote runs if the last run is less than 24 hours ago
- supports `-Testing` for test sessions
