# Malformed Fixture Strategy

Issue: `#17`

## Objective

Validate resilience of provider normalization paths when input payloads are malformed or partial.

## Added Malformed Fixtures

- `tests/SeedLists.Dat.Tests/Fixtures/malformed-tosec-no-game-name.dat`
- `tests/SeedLists.Dat.Tests/Fixtures/malformed-goodtools-no-rom-lines.dat`
- `tests/SeedLists.Dat.Tests/Fixtures/malformed-nointro-bad-hash.dat`

## Assertions

- TOSEC malformed input falls back to canonical wrapper with `rawPreview`.
- GoodTools malformed input falls back to canonical wrapper with `rawPreview`.
- No-Intro malformed hash payload maps into canonical entries and surfaces validation diagnostics (`crc32`, `md5`, `sha1`).

## Why This Matters

This ensures mapper regressions are caught early and fallback behavior remains safe and explicit.
