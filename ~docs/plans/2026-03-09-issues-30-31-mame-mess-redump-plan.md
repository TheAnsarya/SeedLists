# Plan: Issues #30 and #31 - MAME/MESS/Redump Source Expansion

## Issues

- `#30` - `[3.1] Add MAME/MESS/Redump DAT providers (local-first ingestion)`
- `#31` - `[3.2] Expand DAT source documentation and ingestion plan (MAME/MESS/Redump/Fruit)`

## Goals

- Add first-class local ingestion providers for MAME, MESS, and Redump DAT corpora.
- Keep ingestion policy local-first and operator-controlled.
- Document concrete source paths and onboarding strategy for fruit-machine coverage.

## Implementation Phases

1. Add provider enums and options:
	- Add `DatProviderKind` values for `Mame`, `Mess`, `Redump`.
	- Add local directory options in `SeedListsDatOptions`.
2. Implement providers:
	- Add `MameProvider`, `MessProvider`, `RedumpProvider` with local `.dat`/`.zip` handling.
	- Keep `.7z` policy manual-extract with explicit error messages.
3. Wire runtime paths:
	- Register providers in dependency injection.
	- Add provider defaults to worker options and `appsettings.json`.
4. Harden ingestion behavior:
	- Parse normalized payload bytes in `DatCollectionService` so XML-like payloads ingest correctly.
	- Extend normalization mapping for new XML-like providers.
5. Validate with tests:
	- Provider discovery/download tests for each new provider.
	- Regression test proving XML-like source normalization is parsed successfully.
6. Update docs:
	- Expand `docs/DAT_SOURCES.md` for MAME/MESS/Redump and fruit-machine guidance.
	- Update `docs/INGESTION_RUNBOOK.md`, `docs/ARCHITECTURE.md`, and `README.md` references.

## Validation Checklist

- `dotnet test tests/SeedLists.Dat.Tests/SeedLists.Dat.Tests.csproj -c Release --filter "MameProviderTests|MessProviderTests|RedumpProviderTests|DatCollectionServiceManifestTests"`
- Confirm worker config includes providers:
	- `Tosec`, `GoodTools`, `NoIntro`, `Mame`, `Mess`, `Redump`
- Confirm docs explicitly answer Redump support status and fruit-machine onboarding approach.

## Operational Notes

- Redump website automation is not required for baseline support; local/manual DAT placement is sufficient.
- No-Intro remote download cooldown policy remains unchanged and enforced.
