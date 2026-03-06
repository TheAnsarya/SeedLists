# Plan: Issues #3 and #4 - TOSEC and GoodTools Hardening

## Issues

- `#3` Harden TOSEC provider discovery and archive handling
- `#4` Expand GoodTools local ingestion and classification

## Deliverables

- TOSEC retry and remote discovery resiliency improvements
- TOSEC/GoodTools zip extraction support with explicit `.7z` policy
- GoodTools metadata classification and system derivation updates
- Provider test coverage for critical behaviors
- Documentation updates for both providers

## Validation

- full test suite run
- no regressions in existing normalization/provider tests

## Risks

- automatic `.7z` extraction is intentionally unsupported in current implementation
- archive entry heuristics may need future tuning for edge-case packs

## Follow-up

- add optional `.7z` extraction support in a future issue if dependency policy permits
