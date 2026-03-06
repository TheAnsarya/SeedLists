# JSON Schema

Issue: `#2`

SeedLists uses a canonical JSON catalog schema for normalized DAT payloads.

## Canonical Schema File

- Source: `src/SeedLists.Dat/Schemas/seedlists.catalog.schema.json`
- Example payload: `docs/examples/sample-catalog.json`
- Contract tests: `tests/SeedLists.Dat.Tests/CatalogSchemaContractTests.cs`

## Conversion Strategy

- Canonical mapping contract: `docs/CONVERSION_STRATEGY.md`
- Provider mapping specifics: `docs/PROVIDER_MAPPINGS.md`
- Malformed fallback strategy: `docs/MALFORMED_FIXTURE_STRATEGY.md`

## Required Root Fields

- `name`: non-empty string
- `provider`: one of `Unknown`, `NoIntro`, `Tosec`, `GoodTools`
- `games`: array of game entries

## Game Requirements

- `name`: non-empty string
- `roms`: array of rom entries

## Rom Requirements

- `name`: non-empty string
- `size`: integer >= 0
- Optional hashes:
  - `crc32` or `crc`: 8 hex chars
  - `md5`: 32 hex chars
  - `sha1`: 40 hex chars

## Validation Integration

`CatalogValidationService` enforces these rules in runtime sync flow before parsing.
