# Architecture

## Projects

- `SeedLists.Dat`
  - Contracts: providers, parsers, sync service
  - Models: DAT metadata, parse/sync payloads
  - Parsing: JSON DAT parser
  - Providers: TOSEC, GoodTools, No-Intro
  - Services: sync orchestration and state persistence

- `SeedLists.Worker`
  - Loads configuration
  - Runs provider sync jobs in a scheduled loop

## Data Flow

1. Provider lists available DAT sources.
2. Provider download stream is materialized to disk.
3. Parser maps DAT XML into typed model objects.
4. Summary JSON is written for quick inspection.

## State

No-Intro remote cooldown timestamps are stored in a JSON state file under local app data.
