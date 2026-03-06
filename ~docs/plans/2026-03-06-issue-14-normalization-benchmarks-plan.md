# Plan: Issue #14 - Benchmark Normalization Throughput and Allocations

## Issue

- GitHub: `#14`
- Title: `[1.14] Benchmark normalization throughput and allocations`

## Acceptance Mapping

- JSON passthrough benchmark: planned.
- TOSEC text mapper benchmark: planned.
- No-Intro text mapper benchmark: planned.
- GoodTools text mapper benchmark: planned.
- Allocation budget tracking: planned via benchmark doc guardrails.

## Deliverables

- Extended BenchmarkDotNet harness in `benchmarks/SeedLists.Benchmarks`.
- New docs page: `docs/NORMALIZATION_BENCHMARKS.md`.
- Build and benchmark run confirmation in this session.

## Risks

- Benchmark duration in CI/local may vary across hardware.
- Absolute allocations can shift with runtime updates.

## Mitigations

- Use scenario-specific filter for fast local verification.
- Treat budgets as guardrails and update only with documented rationale.
