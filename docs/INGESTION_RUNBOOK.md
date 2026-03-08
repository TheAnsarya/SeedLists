# Ingestion Runbook

Issues: `#10`, `#25`

## Documentation Context

- Full documentation tree: `DOCUMENTATION_INDEX.md`
- Ecosystem strategy: `SEEDLISTS_ECOSYSTEM_GUIDE.md`

## Prerequisites

- .NET 10 SDK installed
- Local source paths configured:
  - TOSEC: `D:\Roms\TOSEC`
  - GoodTools: `C:\~reference-roms\roms`
  - No-Intro local DATs: `C:\~reference-roms\dats\nointro`

## Recommended Initial Configuration

Start in local-first mode and expand gradually:

```json
{
  "SeedListsDat": {
    "OutputDirectory": "C:\\SeedLists\\output",
    "StateDirectory": "C:\\SeedLists\\state",
    "EnableInternetDownloads": false,
    "AllowNoIntroDownloadDuringTesting": false,
    "MaxDatsPerRun": 25,
    "IncludeNamePatterns": ["*"],
    "ExcludeNamePatterns": []
  }
}
```

Use bounded controls until the pipeline behavior is stable for your dataset.

## Worker Execution

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project src/SeedLists.Worker
```

## Recommended Operator Workflow

1. Build and test before each ingestion change.
2. Start with bounded runs (`MaxDatsPerRun`, include/exclude patterns).
3. Inspect provider manifests and summary outputs.
4. Resolve failing sources and re-run bounded scopes.
5. Expand scope after stable manifest/error trends.

### Reliability Settings

- `Worker:MaxRetryAttempts`: retries per provider before hard failure.
- `Worker:RetryDelaySeconds`: delay between retry attempts.
- `Worker:StopCycleOnProviderFailure`: stop remaining providers in the cycle when one provider fails.
- `Worker:EmitCycleSummary`: writes cycle-level telemetry summary logs.
- `SeedListsDat:MaxDatsPerRun`: optional cap for per-provider source processing.
- `SeedListsDat:IncludeNamePatterns`: optional wildcard include list for source names.
- `SeedListsDat:ExcludeNamePatterns`: optional wildcard exclude list for source names.

## Internet Download Safety

- Keep `SeedListsDat:EnableInternetDownloads` disabled by default.
- For No-Intro, remote download runs are cooldown-limited (24h).
- Testing-only override is controlled by `SeedListsDat:AllowNoIntroDownloadDuringTesting`.

No-Intro policy details: `NOINTRO_COOLDOWN_POLICY.md`

## Script Workflows

- TOSEC pull: `scripts/download-tosec-dats.ps1`
- GoodTools local collection: `scripts/download-goodtools-dats.ps1`
- No-Intro guarded pull: `scripts/download-nointro-dats.ps1`

Provider behavior references:

- TOSEC: `TOSEC_PROVIDER_HARDENING.md`
- GoodTools: `GOODTOOLS_INGESTION.md`
- Source policies: `DAT_SOURCES.md`

## Troubleshooting

- Validation failures:
  - confirm payload conforms to `JSON_SCHEMA.md`
- No-Intro cooldown errors:
  - wait 24h, or use testing override only for test environments
- Parser failures:
  - ensure ingested files are normalized JSON catalog payloads

## Understanding Run Outputs

- normalized catalogs:
  - `{OutputDirectory}/{provider}/{name}.dat`
- parsed summaries:
  - `{OutputDirectory}/{provider}/{name}.summary.json`
- manifest artifacts:
  - `{OutputDirectory}/{provider}/run-manifests/latest-sync-manifest.json`
  - `{OutputDirectory}/{provider}/run-manifests/{runId}-{provider}-sync-manifest.json`

Use summary files to inspect parsed content and manifests to monitor health, throughput, and failures.

## Run Artifacts

- Provider run manifests are written under:
  - `{SeedListsDat:OutputDirectory}/{provider}/run-manifests/`
- Manifest references:
  - latest: `latest-sync-manifest.json`
  - historical: `{runId}-{provider}-sync-manifest.json`
- Schema details and usage: `SYNC_MANIFESTS.md`
- Bounded run controls: `BOUNDED_SYNC_CONTROLS.md`

## Next Reads

- `SEEDLISTS_ECOSYSTEM_GUIDE.md`
- `CONVERSION_STRATEGY.md`
- `ARCHITECTURE.md`
