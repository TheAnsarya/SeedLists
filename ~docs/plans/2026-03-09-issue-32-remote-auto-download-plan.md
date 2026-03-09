# Plan: Issue #32 - Remote Auto-Download and Version Checks

## Issue

- `#32` - `[3.3] Add scheduled auto-download + version checks for MAME/MESS/Redump/GoodTools/Fruit DAT sources`

## Goals

- Add remote polling support to MAME, MESS, Redump, GoodTools, and TOSEC provider workflows.
- Ensure unchanged remote payloads are skipped by token/signature checks.
- Keep local-first behavior as fallback when remote sources are unavailable.
- Document runtime configuration and operator workflow for scheduled remote sync.

## Implementation Phases

- Phase 1: Extend sync state contract with string token get/set support in `IDatSyncStateStore` and `FileDatSyncStateStore`.
- Phase 2: Add `RemoteDatSupport` for remote identifier encoding/decoding, poll-interval gating, and token change-detection helpers.
- Phase 3: Add remote polling options (`EnableRemoteVersionChecks`, `RemotePollIntervalHours`) and remote index/URL settings.
- Phase 4: Upgrade MAME, MESS, Redump, GoodTools, and TOSEC providers for remote polling and token-aware change detection.
- Phase 5: Update provider tests for constructor dependency changes and keep remote/local behavior coverage green.
- Phase 6: Refresh operator docs for remote auto-download scheduling and version-check workflows.

## Validation Checklist

- `dotnet test tests/SeedLists.Dat.Tests/SeedLists.Dat.Tests.csproj -c Release`
- Confirm providers skip unchanged remote entries when `EnableRemoteVersionChecks` is enabled.
- Confirm providers still return local entries when remote indexing/download fails.

## Operational Notes

- No-Intro cooldown enforcement remains unchanged at 24 hours.
- Redump index parsing is best-effort; direct URL lists remain a fallback mechanism.
- Fruit-machine workflows remain pattern-driven through MAME/MESS and optional remote URL feeds.
