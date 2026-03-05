# Multi-Session Plan (Issue #11)

## Goal

Execute SeedLists in phased delivery while maintaining issue-first discipline.

## Phase A: JSON Contract Foundation

- Publish canonical schema and examples (`#8`)
- Add runtime validation service and tests (`#9`)
- Stabilize parser behavior on validated catalogs

## Phase B: Provider Hardening

- TOSEC discovery retry and archive extraction improvements (`#3`)
- GoodTools source indexing and metadata quality (`#4`)
- No-Intro cooldown telemetry and integration tests (`#5`)

## Phase C: Operational Readiness

- Worker retries and run-state metrics (`#7`)
- Ingestion runbook and operator docs (`#10`)
- Perf baselines and CI benchmark checks (`#6`)

## Validation Gates

- Build + tests green on each phase merge
- Benchmarks captured after parser/provider changes
- Docs updated in `docs/` and `~docs/` each phase
