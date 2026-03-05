# Code Plan - Provider Normalization (Issue #12)

## Objective

Convert provider-specific payloads to canonical JSON before validation/parser stages.

## Implemented Now

- Added `ICatalogNormalizationService`
- Implemented `CatalogNormalizationService`
- Integrated normalization into `DatCollectionService`
- Added tests for non-JSON wrapping and JSON default enrichment
- Added technical documentation in `docs/PROVIDER_NORMALIZATION.md`

## Follow-up

- Add provider-specific semantic mappers (TOSEC/GoodTools/No-Intro)
- Replace generic non-JSON wrapper with structured game/rom extraction
- Add benchmark for normalization overhead
