# DAT Sources

Issue: `#24`

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

## Output Contract

- All provider payloads are normalized into canonical JSON catalogs before parsing.
- Provider sync runs emit summary artifacts and run manifests for operational diagnostics.
