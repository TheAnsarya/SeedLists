# PleasureDome Provider

Issues: `#33`, `#34`, `#35`, `#36`, `#37`, `#48`, `#49`, `#50`, `#51`

This document describes how SeedLists discovers and ingests DAT files from Pleasuredome.

## Source Endpoints

- Home index:
  - `https://pleasuredome.github.io/pleasuredome/index.html`
- MAME DAT page:
  - `https://pleasuredome.github.io/pleasuredome/mame/index.html`
- NonMAME category index:
  - `https://pleasuredome.github.io/pleasuredome/nonmame/index.html`

## Configuration

Add or override the following `SeedListsDat` settings:

```json
{
  "SeedListsDat": {
    "PleasureDomeLocalDirectory": "C:\\~reference-roms\\dats\\pleasuredome",
    "PleasureDomeMameIndexUrl": "https://pleasuredome.github.io/pleasuredome/mame/index.html",
    "PleasureDomeNonMameIndexUrl": "https://pleasuredome.github.io/pleasuredome/nonmame/index.html",
    "PleasureDomeNonMameCategorySlugs": ["demul", "fbneo", "fruitmachines", "hbmame", "kawaks", "pinball", "pinmame", "raine"],
    "EnableInternetDownloads": true,
    "EnableRemoteVersionChecks": true,
    "RemotePollIntervalHours": 24
  }
}
```

## Discovery Behavior

- Local mode:
  - scans `PleasureDomeLocalDirectory` for `.dat`, `.zip`, `.7z`
- Remote mode:
  - discovers MAME DAT ZIP links from `PleasureDomeMameIndexUrl`
  - discovers selected NonMAME category pages from `PleasureDomeNonMameIndexUrl`
  - scans each selected category page for DAT ZIP links
- Link de-duplication:
  - duplicate URLs across pages are ignored

## Change Detection and Polling

- Poll gating key:
  - provider prefix `pleasuredome`
- If `EnableRemoteVersionChecks = true`:
  - polling respects `RemotePollIntervalHours`
  - remote files are listed only when their token has changed
- Token storage:
  - per remote URL token is stored via `IDatSyncStateStore`
  - token value defaults to remote file name

## Download and Extraction

- `remote|<token>|<url>` identifiers are used for remote entries
- `.zip` payloads are extracted automatically to preferred DAT entries (`.dat`, `.xml`, `.json`, `.txt`)
- `.7z` requires manual extraction before ingestion

## Category/System Mapping

Default NonMAME mappings:

- `demul` -> `Demul`
- `fbneo` -> `FinalBurn Neo`
- `fruitmachines` -> `Fruit Machines`
- `hbmame` -> `HBMAME`
- `kawaks` -> `Kawaks`
- `pinball` -> `Pinball`
- `pinmame` -> `PinMAME`
- `raine` -> `Raine`
- `mame` -> `MAME`

Discovered Pleasuredome NonMAME categories currently include:

- `demul`
- `fbneo`
- `fruitmachines`
- `hbmame`
- `kawaks`
- `pinball`
- `pinmame`
- `raine`

## Scripted DAT Downloads

Use the downloader script to fetch DAT ZIP assets from MAME and selected NonMAME categories:

```powershell
& ".\scripts\download-pleasuredome-dats.ps1" -OutputPath "C:\~reference-roms\dats\pleasuredome-remote" -MaxDownloadsPerCategory 1
```

Helpful switches:

- `-CategorySlugs` to limit categories
- `-SkipMame` to only download NonMAME categories
- `-SkipExisting` to avoid re-downloading existing files
- `-MaxDownloadsPerCategory` to keep runs bounded

## Troubleshooting

- No remote entries listed:
  - verify `EnableInternetDownloads = true`
  - verify poll interval was reached or set `RemotePollIntervalHours = 0` for immediate polling tests
  - verify category slugs in `PleasureDomeNonMameCategorySlugs`
- Entries listed but not downloaded:
  - ensure identifiers are passed unchanged (`remote|...`)
- Download errors:
  - check remote URL availability
  - confirm DAT ZIP links still exist on source pages

## Validation Commands

```powershell
# Provider correctness tests
& "C:\Program Files\dotnet\dotnet.exe" test tests/SeedLists.Dat.Tests/SeedLists.Dat.Tests.csproj -c Release --filter "PleasureDomeProviderTests"

# Discovery + allocation benchmark
& "C:\Program Files\dotnet\dotnet.exe" run --project benchmarks/SeedLists.Benchmarks/SeedLists.Benchmarks.csproj -c Release -- --filter "*PleasureDomeDiscoveryBenchmark*" --job short
```
