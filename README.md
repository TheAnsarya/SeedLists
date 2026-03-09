# SeedLists

SeedLists is a JSON-first .NET 10 ingestion and normalization toolkit for ROM DAT workflows.

It is designed to turn mixed provider inputs (local DAT files, local archives, and optional remote provider payloads) into a consistent canonical JSON catalog format, then validate, parse, and persist operator-friendly run artifacts.

## What SeedLists Does With Your Data

For each provider sync run, SeedLists:

1. Discovers available source entries from provider-specific locations.
2. Downloads or reads source payload bytes.
3. Normalizes payloads into canonical JSON catalogs.
4. Validates canonical catalogs against runtime schema rules.
5. Persists normalized payloads and parsed summary outputs.
6. Emits per-run manifest artifacts for observability and troubleshooting.

Output artifacts are written under `SeedListsDat:OutputDirectory` per provider.

## Repository Layout

- `src/SeedLists.Dat`: reusable ingestion library (providers, normalization, validation, parser, sync orchestration)
- `src/SeedLists.Worker`: long-running worker for scheduled provider sync cycles
- `tests/SeedLists.Dat.Tests`: unit and integration tests for providers, policies, and pipeline behavior
- `benchmarks/SeedLists.Benchmarks`: BenchmarkDotNet suites for normalization and parser regressions
- `docs/`: user and operator documentation
- `scripts/`: helper scripts for provider-specific acquisition workflows

## Provider Model

- TOSEC: local discovery plus optional remote index/archive workflows
- GoodTools: local `.dat` and archive ingestion workflows
- No-Intro: local-first ingestion with guarded remote mode and a mandatory 24-hour cooldown policy
- MAME: local `.dat`/`.zip` ingestion for arcade machine catalog workflows
- MESS: local `.dat`/`.zip` ingestion for software-list catalog workflows
- Redump: local `.dat`/`.zip` ingestion for disc-preservation catalog workflows

## Quick Start

```powershell
# build
& "C:\Program Files\dotnet\dotnet.exe" build SeedLists.slnx

# test
& "C:\Program Files\dotnet\dotnet.exe" test SeedLists.slnx -c Release

# run worker
& "C:\Program Files\dotnet\dotnet.exe" run --project src/SeedLists.Worker
```

## Recommended Setup Workflow

1. Start local-first: configure local provider directories first and keep internet downloads disabled initially.
2. Validate pipeline health with tests and one small bounded sync run.
3. Enable remote provider behavior only when needed and policy-safe.
4. Use run manifests to verify discovered/processed/failed counts before scaling up.
5. Tune bounded sync controls (`MaxDatsPerRun`, include/exclude patterns) for safer large library onboarding.

For MAME/MESS and fruit-machine-focused onboarding, prefer `IncludeNamePatterns` filters (for example `"*fruit*"`, `"*slot*"`, `"*aristocrat*"`) to stage ingestion in narrow slices.

## Getting Best Results

- Prefer clean local DAT corpora and stable directory conventions per provider.
- Use include/exclude wildcard controls to phase large ingestions by system/region/revision.
- Monitor `latest-sync-manifest.json` per provider to catch regressions quickly.
- Treat No-Intro remote mode as controlled and infrequent; use local/manual ingestion by default.
- Run parser and normalization benchmarks before and after performance-sensitive changes.

## Key Runtime Artifacts

- Canonical normalized payloads: `{OutputDirectory}/{provider}/{name}.dat`
- Parsed summaries: `{OutputDirectory}/{provider}/{name}.summary.json`
- Run manifests: `{OutputDirectory}/{provider}/run-manifests/*.json`

## Script Entry Points

- `scripts/download-tosec-dats.ps1`
- `scripts/download-goodtools-dats.ps1`
- `scripts/download-nointro-dats.ps1`
- `scripts/test-markdown-policy.ps1`
- `scripts/benchmark-markdown-policy.ps1`

No-Intro script behavior:

- rejects remote runs if the last run is less than 24 hours ago
- supports `-Testing` for controlled test sessions only

## Documentation Link Tree

Start here for complete docs discovery:

- `docs/DOCUMENTATION_INDEX.md`

Primary operator paths:

- `docs/SEEDLISTS_ECOSYSTEM_GUIDE.md`
- `docs/INGESTION_RUNBOOK.md`
- `docs/SYNC_MANIFESTS.md`
- `docs/BOUNDED_SYNC_CONTROLS.md`

Quality automation scripts:

- `scripts/test-markdown-policy.ps1`
- `scripts/benchmark-markdown-policy.ps1`
