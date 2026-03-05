# Ingestion Runbook

Issue: `#10`

## Prerequisites

- .NET 10 SDK installed
- Local source paths configured:
  - TOSEC: `D:\Roms\TOSEC`
  - GoodTools: `C:\~reference-roms\roms`
  - No-Intro local DATs: `C:\~reference-roms\dats\nointro`

## Worker Execution

```powershell
& "C:\Program Files\dotnet\dotnet.exe" run --project src/SeedLists.Worker
```

### Reliability Settings

- `Worker:MaxRetryAttempts`: retries per provider before hard failure.
- `Worker:RetryDelaySeconds`: delay between retry attempts.
- `Worker:StopCycleOnProviderFailure`: stop remaining providers in the cycle when one provider fails.
- `Worker:EmitCycleSummary`: writes cycle-level telemetry summary logs.

## Internet Download Safety

- Keep `SeedListsDat:EnableInternetDownloads` disabled by default.
- For No-Intro, remote download runs are cooldown-limited (24h).
- Testing-only override is controlled by `SeedListsDat:AllowNoIntroDownloadDuringTesting`.

## Script Workflows

- TOSEC pull: `scripts/download-tosec-dats.ps1`
- GoodTools local collection: `scripts/download-goodtools-dats.ps1`
- No-Intro guarded pull: `scripts/download-nointro-dats.ps1`

## Troubleshooting

- Validation failures:
  - confirm payload conforms to `docs/JSON_SCHEMA.md`
- No-Intro cooldown errors:
  - wait 24h, or use testing override only for test environments
- Parser failures:
  - ensure ingested files are normalized JSON catalog payloads
