# Code Plan: Issue #2 JSON Schema and Conversion Contract

## Objective

Finalize canonical JSON schema contract deliverables and conversion strategy documentation.

## Scope

- Ensure machine-readable schema artifact remains authoritative.
- Add schema contract tests to prevent drift in required fields and provider enum values.
- Validate sample catalog against runtime validation rules.
- Document conversion strategy entrypoint and mapping contract references.

## Validation

- `dotnet test SeedLists.slnx -c Release`
- confirm `CatalogSchemaContractTests` passes
