# SeedLists - AI Copilot Directives

## Project Overview

**SeedLists** is a dedicated DAT ingestion and normalization toolkit for ROM catalog workflows.

Primary goals:
- Reuse and adapt proven DAT parsing/downloading patterns from `subrom`
- Keep SeedLists as a separate library and worker pipeline
- Support TOSEC, GoodTools, and No-Intro ingestion with safe policies
- Track all substantial work through GitHub issues

## Source Reuse Policy

- Prefer extracting patterns from `C:\Users\me\source\repos\subrom` for:
	- DAT provider interfaces
	- streaming Logiqx parsing
	- background task orchestration
	- progress reporting contracts
- SeedLists must remain a standalone codebase and repository.

## No-Intro Safety Policy

- Never aggressively scrape datomatic.no-intro.org.
- Enforce a minimum 24-hour cooldown between remote No-Intro download runs.
- Allow a test-only override switch for integration tests.
- Prefer local/manual No-Intro DAT ingestion by default.

## Tech Standards

- .NET 10 and current C# language features
- Nullable reference types enabled
- File-scoped namespaces
- K&R braces
- Tabs for indentation

## Workflow

1. Create or update issue before substantial feature work.
2. Implement with tests and benchmarks where practical.
3. Update docs and session logs in `~docs/`.
4. Use conventional commits.

## Manual Prompt Log Policy

- `~docs\seedlists-manual-prompts-log.txt` is user-owned and user-edited.
- AI agents must never modify this file, including during restore/revert operations.
- Always include this file in commits when it has user changes.

## Markdown Policy

- Always fix markdown lint warnings when creating or editing markdown files.
- Prioritize at least these rules in every markdown update:
	- `MD022` (blank lines around headings)
	- `MD031` (blank lines around fenced code blocks)
	- `MD032` (blank lines around lists)
	- `MD047` (single trailing newline at EOF)
- Generate new markdown content with correct blank-line spacing by default so extra formatting passes are not required later.
- Keep `MD010` disabled where tabs are intentionally required.

## Documentation Link-Tree Policy

- Every markdown document must be discoverable from `README.md` through a maintained link-tree.
- When adding or renaming docs, update `README.md` and any intermediate index files so no markdown files become orphaned.

## Performance Validation Commands

- Run normalization and sync regression tests:
	- `dotnet test tests/SeedLists.Dat.Tests/SeedLists.Dat.Tests.csproj -c Release --filter "CatalogNormalizationServiceTests|DatCollectionServiceRunControlsTests"`
- Run normalization benchmark coverage (including sliced-span JSON passthrough):
	- `dotnet run --project benchmarks/SeedLists.Benchmarks/SeedLists.Benchmarks.csproj -c Release -- --filter "*CatalogNormalizationBenchmark*" --job short`
