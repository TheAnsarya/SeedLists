# Plan: Issue #2 - Define JSON DAT Schema and Conversion Strategy

## Issue

- GitHub: `#2`
- Title: `[1.1] Define JSON DAT schema and conversion strategy`

## Deliverables

- Schema contract documentation refresh (`docs/JSON_SCHEMA.md`)
- Conversion strategy document (`docs/CONVERSION_STRATEGY.md`)
- Schema contract tests covering required fields and provider enum
- Runtime sample validation test

## Validation

- full test suite execution after schema contract test additions

## Risks

- Schema and runtime validator can drift over time without tests

## Mitigation

- Keep schema contract tests as a blocking guardrail in CI/test runs
