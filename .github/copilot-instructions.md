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
