# Code Plan - Schema Validation (Issues #8, #9)

## Scope

Implement JSON catalog validation that is enforced before parser execution.

## Components

1. Schema artifact

- Add `src/SeedLists.Dat/Schemas/seedlists.catalog.schema.json`
- Add sample payload under `docs/examples/`

1. Validation contract

- Add `ICatalogValidationService`
- Add `CatalogValidationResult`

1. Validation implementation

- Add `CatalogValidationService` rules:
  - root required fields
  - provider enum validation
  - game/rom required field validation
  - hash format validation

1. Pipeline integration

- Register validator in DI
- Validate payload bytes in `DatCollectionService` prior to parser call

1. Tests

- Valid catalog case
- Invalid provider/missing fields/hash format case

## Risks

- Providers currently return mixed source formats; strict JSON validation may reject non-normalized payloads.
- Mitigation: maintain clear normalization requirement and explicit errors.

## Done Criteria

- Unit tests pass
- Build succeeds
- Docs updated (`docs/JSON_SCHEMA.md`, runbook)
