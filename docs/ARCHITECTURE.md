# Architecture

Issue: `#24`

- Full docs tree: `DOCUMENTATION_INDEX.md`

## Projects

- `SeedLists.Dat`
  - Contracts: providers, parsers, sync service abstractions
  - Models: DAT metadata, parse/sync payloads, run manifest artifacts
  - Parsing: `StreamingJsonDatParser` (canonical JSON)
  - Providers: TOSEC, GoodTools, No-Intro
  - Services:
    - `CatalogNormalizationService`
    - `CatalogValidationService`
    - `DatCollectionService`
    - `FileDatSyncStateStore`

- `SeedLists.Worker`
  - Loads configuration
  - Runs provider sync jobs in a scheduled loop with retry and cycle summary options

## Data Flow

1. Provider lists available DAT sources.
2. Provider source bytes are downloaded (local or remote provider rules).
3. `CatalogNormalizationService` maps payload into canonical SeedLists JSON.
4. `CatalogValidationService` enforces schema-aligned constraints.
5. Canonical JSON payload is written to provider output storage.
6. `StreamingJsonDatParser` maps canonical JSON into typed models.
7. Summary JSON is emitted for quick inspection.
8. Sync run manifest JSON is emitted for observability.

## State

- No-Intro remote cooldown timestamps are persisted in `provider-sync-state.json`.
- Per-run provider manifests are persisted under `{OutputDirectory}/{provider}/run-manifests/`.

## Reference Docs

- `JSON_SCHEMA.md`
- `CONVERSION_STRATEGY.md`
- `PROVIDER_MAPPINGS.md`
- `INGESTION_RUNBOOK.md`
- `SYNC_MANIFESTS.md`
