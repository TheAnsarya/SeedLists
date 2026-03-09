# DAT Sources

Issues: `#24`, `#30`, `#31`

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
- Archive handling:
  - `.zip`: extracted automatically
  - `.7z`: currently requires manual extraction

## Redump

- Local source default: `C:\~reference-roms\dats\redump`
- Status: Yes, Redump ingestion is supported in SeedLists through local-first provider workflows.
- Acquisition model: manual/local DAT placement is currently recommended.
- Primary project page:
  - `https://www.redump.org/`
- Note:
  - Direct automated download integration is intentionally not required for baseline ingestion; place DAT packs locally and run provider sync.

## Fruit Machine Coverage

- Fruit-machine catalogs are commonly present in MAME ecosystem sets (for example Aristocrat, Barcrest, JPM, and related machine families).
- Recommended onboarding strategy:
  - stage by include patterns (for example `*fruit*`, `*slot*`, `*aristocrat*`, `*barcrest*`, `*jpm*`)
  - keep bounded run caps enabled until manifests show stable pass rates
- Operator reference catalog browser:
  - `https://mame.spludlow.co.uk/`

## Output Contract

- All provider payloads are normalized into canonical JSON catalogs before parsing.
- Provider sync runs emit summary artifacts and run manifests for operational diagnostics.
