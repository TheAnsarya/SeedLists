# Conversion Strategy

Issue: `#2`

This document defines the source-to-canonical conversion contract for SeedLists DAT ingestion.

## Canonical Target

All provider payloads are normalized to the SeedLists JSON catalog schema:

- schema file: `src/SeedLists.Dat/Schemas/seedlists.catalog.schema.json`
- runtime validation: `src/SeedLists.Dat/Services/CatalogValidationService.cs`

## Provider Inputs

- TOSEC: XML-like DAT text, local DAT files, local/remote archives
- No-Intro: XML-like DAT text and guarded remote download workflow
- GoodTools: line-oriented DAT text and local archives

## Conversion Pipeline

1. Provider retrieves payload bytes.
2. `CatalogNormalizationService` maps source payloads into canonical JSON.
3. `CatalogValidationService` enforces schema-aligned runtime constraints.
4. `StreamingJsonDatParser` parses normalized JSON into internal models.

## Mapping Contract

Required canonical root fields:

- `name`
- `provider`
- `games`

Game contract:

- `name`
- `roms`

ROM contract:

- `name`
- `size`
- optional hash fields (`crc32`/`crc`, `md5`, `sha1`) with strict hex formats

## Fallback Rules

For non-JSON payloads that do not match provider mappers:

- output a canonical wrapper with metadata fields
- keep `games` as an empty array
- include `rawPreview` for diagnostics

See `MALFORMED_FIXTURE_STRATEGY.md` for malformed input handling details.
