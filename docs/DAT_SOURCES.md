# DAT Sources

Issues: `#24`, `#30`, `#31`, `#32`

SeedLists consumes normalized JSON catalogs for parser and storage workflows.

## TOSEC

- Local root default: `D:\Roms\TOSEC`
- Remote index: `https://www.tosecdev.org/downloads/category/22-datfiles`
- Supported local file types: `.dat`, `.zip`, `.7z`
- Archive handling:
  - `.zip`: extracted automatically to preferred DAT payload entry
  - `.7z`: currently requires manual extraction

## GoodTools

- Local source default: `C:\~reference-roms\roms`
- Provider ingests `.dat`, `.zip`, and `.7z` entries as candidates.
- Optional remote source URLs:
  - `SeedListsDat:GoodToolsRemoteDatUrls`
- Remote behavior:
  - poll and list only changed remote entries when `EnableRemoteVersionChecks` is enabled
- Archive handling:
  - `.zip`: extracted automatically
  - `.7z`: currently requires manual extraction

## No-Intro

- Local source default: `C:\~reference-roms\dats\nointro`
- Remote page: `https://datomatic.no-intro.org/index.php?page=download&s=64`
- Policy:
  - enforce minimum 24 hours between remote download runs
  - allow testing override in controlled contexts

## MAME

- Local source default: `C:\~reference-roms\dats\mame`
- Recommended source references:
  - MAME command-line docs (`-listxml`, `-listsoftware`):
    - `https://docs.mamedev.org/commandline/commandline-all.html`
  - progetto-SNAPS MAME DAT history/packages:
    - `https://www.progettosnaps.net/dats/MAME/`
    - `https://www.progettosnaps.net/dats/`
- Supported local file types: `.dat`, `.zip`, `.7z`
- Optional remote index:
  - `SeedListsDat:MameRemoteIndexUrl`
- Remote behavior:
  - index scraping for DAT package links
  - token-based change detection and poll gating (`RemotePollIntervalHours`)
- Archive handling:
  - `.zip`: extracted automatically
  - `.7z`: currently requires manual extraction

## MESS (Software Lists)

- Local source default: `C:\~reference-roms\dats\mess`
- Recommended source references:
  - MAME command-line software list exports (`-listsoftware`, `-getsoftlist`):
    - `https://docs.mamedev.org/commandline/commandline-all.html`
  - progetto-SNAPS DAT resource packs with MESS/softlist coverage:
    - `https://www.progettosnaps.net/dats/`
- Supported local file types: `.dat`, `.zip`, `.7z`
- Optional remote index:
  - `SeedListsDat:MessRemoteIndexUrl`
- Remote behavior:
  - index scraping for software-list DAT package links
  - token-based change detection and poll gating (`RemotePollIntervalHours`)
- Archive handling:
  - `.zip`: extracted automatically
  - `.7z`: currently requires manual extraction

## Redump

- Local source default: `C:\~reference-roms\dats\redump`
- Status: Redump ingestion is supported in both local-first and optional remote polling workflows.
- Optional remote index:
  - `SeedListsDat:RedumpRemoteIndexUrl`
- Optional fallback URL list:
  - `SeedListsDat:RedumpRemoteDatUrls`
- Remote behavior:
  - index scraping where available with fallback to configured direct URLs
  - token-based change detection and poll gating (`RemotePollIntervalHours`)
- Primary project page:
  - `https://www.redump.org/`

## Fruit Machine Coverage

- Fruit-machine catalogs are commonly present in MAME ecosystem sets (for example Aristocrat, Barcrest, JPM, and related machine families).
- Recommended onboarding strategy:
  - stage by include patterns (for example `*fruit*`, `*slot*`, `*aristocrat*`, `*barcrest*`, `*jpm*`)
  - keep bounded run caps enabled until manifests show stable pass rates
- Optional remote URL list for fruit-machine DAT payloads:
  - `SeedListsDat:FruitMachineRemoteDatUrls`
  - remote files are staged through MAME/MESS naming and include-pattern filtering
- Operator reference catalog browser:
  - `https://mame.spludlow.co.uk/`

## Shared Remote Polling Controls

- `SeedListsDat:EnableInternetDownloads`: enables remote provider index/URL polling and download
- `SeedListsDat:EnableRemoteVersionChecks`: enables token/signature checks before re-downloading
- `SeedListsDat:RemotePollIntervalHours`: minimum interval between provider poll attempts

## Output Contract

- All provider payloads are normalized into canonical JSON catalogs before parsing.
- Provider sync runs emit summary artifacts and run manifests for operational diagnostics.
