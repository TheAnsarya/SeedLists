# Code Plan: Issue #6 Parser Benchmarks

## Objective

Add baseline parser benchmark scenarios and document throughput/allocation guardrails.

## Scope

- Expand `JsonDatParserBenchmark` with small, medium, and large payload scenarios.
- Keep benchmark runner switch-based so parser and normalization benchmarks can run together.
- Add parser benchmark documentation and budgets.

## Implementation

1. Generate deterministic payloads via `Utf8JsonWriter` for repeatable parse benchmarks.
2. Add benchmark methods for small/medium/large payload scales.
3. Capture baseline benchmark output and write to docs.

## Validation

- `dotnet build benchmarks/SeedLists.Benchmarks/SeedLists.Benchmarks.csproj -c Release`
- `dotnet run --project benchmarks/SeedLists.Benchmarks -c Release -- --filter *JsonDatParserBenchmark* --warmupCount 1 --iterationCount 3`
