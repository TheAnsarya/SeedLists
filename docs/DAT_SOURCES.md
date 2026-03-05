# DAT Sources

SeedLists consumes normalized JSON catalogs for parser and storage workflows.

## TOSEC

- Local root default: `D:\Roms\TOSEC`
- Remote index: `https://www.tosecdev.org/downloads/category/22-datfiles`

## GoodTools

- Local source default: `C:\~reference-roms\roms`
- Provider ingests `.dat`, `.zip`, and `.7z` entries as candidates.

## No-Intro

- Local source default: `C:\~reference-roms\dats\nointro`
- Remote page: `https://datomatic.no-intro.org/index.php?page=download&s=64`
- Policy:
  - enforce minimum 24 hours between remote download runs
  - allow testing override in controlled contexts
