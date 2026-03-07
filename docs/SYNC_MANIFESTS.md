# Sync Manifests

Issue: `#22`

SeedLists writes a per-provider run manifest after each sync execution.

## Output Location

For provider output directory `{OutputDirectory}/{provider}`:

- `run-manifests/{runId}-{provider}-sync-manifest.json`
- `run-manifests/latest-sync-manifest.json`

## Manifest Contents

Each manifest includes:

- run metadata: `runId`, `provider`, `startedAtUtc`, `completedAtUtc`, `elapsedMilliseconds`
- aggregate counters: `datsDiscovered`, `datsProcessed`, `datsFailed`
- `errors` array with human-readable failure messages
- `sources` array with per-source metadata and status

Per-source status values:

- `pending`
- `processed`
- `failed`

## Operational Use

- use `latest-sync-manifest.json` for dashboards and automation hooks
- use historical run files for incident timelines and trend analysis
- correlate `errors` with provider source identifiers for targeted retries

## Validation

See `tests/SeedLists.Dat.Tests/DatCollectionServiceManifestTests.cs`.
