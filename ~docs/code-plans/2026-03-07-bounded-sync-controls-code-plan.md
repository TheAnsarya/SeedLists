# Code Plan: Issue #23 Bounded Sync Controls

## Objective

Allow bounded provider runs using max-count and include/exclude source-name pattern controls.

## Scope

- add run-control options in `SeedListsDatOptions`
- apply filtering/cap logic in `DatCollectionService`
- keep run manifests/report counters aligned with selected run set
- add test coverage for pattern filtering and max caps
- add operator documentation for configuration

## Validation

- `dotnet test SeedLists.slnx -c Release`
- verify selected source set and processed counts in tests
