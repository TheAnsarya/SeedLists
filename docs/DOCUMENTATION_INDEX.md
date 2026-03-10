# Documentation Index

Issue: `#25`

This index is the canonical documentation link-tree for SeedLists.

## Start Here

- `../README.md` - Project overview and top-level entry point
- `DOCUMENTATION_INDEX.md` - This complete link-tree page
- `SEEDLISTS_ECOSYSTEM_GUIDE.md` - End-to-end usage and operational strategy
- `INGESTION_RUNBOOK.md` - Day-to-day operator run instructions and troubleshooting

## Core Pipeline and Contracts

- `ARCHITECTURE.md` - System architecture and data flow
- `CONVERSION_STRATEGY.md` - Source-to-canonical conversion contract
- `JSON_SCHEMA.md` - Canonical JSON schema summary and validation contract
- `PROVIDER_NORMALIZATION.md` - Normalization behavior by payload type
- `PROVIDER_MAPPINGS.md` - Provider mapping rules and field mappings
- `examples/sample-catalog.json` - Canonical example catalog payload

## Provider Behavior and Policies

- `DAT_SOURCES.md` - Provider source locations and handling policies
- `PLEASUREDOME_PROVIDER.md` - Pleasuredome discovery, configuration, and operational behavior
- `TOSEC_PROVIDER_HARDENING.md` - TOSEC discovery/download hardening behavior
- `GOODTOOLS_INGESTION.md` - GoodTools local ingestion and classification behavior
- `NOINTRO_COOLDOWN_POLICY.md` - No-Intro remote cooldown policy and persistence

## Operations and Observability

- `INGESTION_RUNBOOK.md` - Operational workflow and troubleshooting
- `SYNC_MANIFESTS.md` - Per-run manifest artifact schema and usage
- `BOUNDED_SYNC_CONTROLS.md` - Max-run and include/exclude source controls

## Quality, Testing, and Regression Safety

- `MAPPER_FIXTURES.md` - Normalization fixture corpus and maintenance guidance
- `MALFORMED_FIXTURE_STRATEGY.md` - Malformed payload resilience strategy
- `NORMALIZATION_BENCHMARKS.md` - Normalization performance baselines and budgets
- `PARSER_BENCHMARKS.md` - Parser performance baselines and budgets

## Navigation Guarantee

Every Markdown document and example file under `docs/` is listed on this page and is reachable from `README.md` via this index.

## Documentation Maintenance Checklist

When behavior changes in code:

1. Update `README.md` entry-point guidance.
2. Update impacted topic docs listed above.
3. Confirm links from `README.md` -> `DOCUMENTATION_INDEX.md` -> affected docs.
4. Add/update issue and plan artifacts under `~docs/`.
