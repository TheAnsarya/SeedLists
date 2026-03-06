# Code Plan - Malformed Fixture and Fallback Assertions

Issue: `#17`

## Scope

Add malformed provider fixtures and assert expected fallback or diagnostic outcomes.

## Work Items

1. Add malformed TOSEC fixture with missing game name.

1. Add malformed GoodTools fixture with non-matching text lines.

1. Add malformed No-Intro fixture with invalid hash values.

1. Add test assertions for:

- fallback wrapper and `rawPreview`
- validation diagnostics for malformed hashes

1. Publish strategy docs for malformed fixture coverage.

## Acceptance Criteria

- Tests pass with malformed fixture suite included.
- Fallback and diagnostic paths are explicitly verified.
