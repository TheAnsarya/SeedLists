# Plan: Issue #6 - Add Parser Benchmarks and Performance Baselines

## Issue

- GitHub: `#6`
- Title: `[1.5] Add parser benchmarks and performance baselines`

## Acceptance Mapping

- BenchmarkDotNet parser baseline coverage: planned.
- Throughput and allocation regression tracking: planned via docs budgets and snapshot.

## Deliverables

- Updated parser benchmark scenarios in `benchmarks/SeedLists.Benchmarks/Program.cs`.
- New docs page `docs/PARSER_BENCHMARKS.md`.
- Baseline benchmark snapshot captured during this session.

## Risks

- Absolute timings vary by machine and runtime.
- Payload growth can increase benchmark duration.

## Mitigations

- Use fixed synthetic payload sizes for comparability.
- Track allocation budgets as regression guardrails instead of absolute pass/fail timing.
