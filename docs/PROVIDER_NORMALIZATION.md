# Provider Normalization

Issue: `#12`

SeedLists normalizes provider payloads into canonical JSON catalogs before schema validation and parsing.

## Flow

1. Provider download returns raw bytes.
2. `CatalogNormalizationService` transforms bytes into canonical JSON.
3. `CatalogValidationService` validates canonical JSON rules.
4. JSON parser maps catalog into typed models.

## Current Behavior

- JSON payloads:
  - preserved and enriched with defaults (`name`, `provider`, `games`) when missing.
- Non-JSON payloads:
  - wrapped in a canonical JSON envelope with `games: []` and `rawPreview`.

## Notes

This keeps the pipeline JSON-only while allowing legacy provider sources to flow through safely with explicit metadata.
