# Mapper Fixture Corpus

Issue: `#15`

## Fixture Files

- `tests/SeedLists.Dat.Tests/Fixtures/tosec-sample.dat`
- `tests/SeedLists.Dat.Tests/Fixtures/nointro-sample.dat`
- `tests/SeedLists.Dat.Tests/Fixtures/goodtools-sample.dat`

## Purpose

These fixtures provide stable provider-style sample payloads for regression tests in `CatalogNormalizationServiceTests`.

## Coverage

- TOSEC XML-like game/rom extraction
- No-Intro XML-like game/rom extraction
- GoodTools line-based ROM extraction

## Maintenance

When mapper logic changes, update or add fixture files and extend fixture-based tests to preserve expected behavior.
