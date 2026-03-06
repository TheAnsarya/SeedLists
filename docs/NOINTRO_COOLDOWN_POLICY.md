# No-Intro Cooldown Policy

SeedLists enforces a strict cooldown policy for remote No-Intro downloads.

## Rules

- Remote No-Intro downloads are blocked unless `SeedListsDat:EnableInternetDownloads` is `true`.
- A successful remote download writes `no-intro:last-download-utc` to persistent state.
- Subsequent remote download attempts are blocked for 24 hours.
- Testing override (`SeedListsDat:AllowNoIntroDownloadDuringTesting`) bypasses cooldown checks for integration scenarios.

## Persistent State

Cooldown state is persisted by `FileDatSyncStateStore`:

- File: `{StateDirectory}/provider-sync-state.json`
- Key: `no-intro:last-download-utc`
- Value format: UTC ISO-8601 (`O` format)

## Test Coverage

- Unit coverage: `tests/SeedLists.Dat.Tests/NoIntroProviderTests.cs`
- Integration coverage: `tests/SeedLists.Dat.Tests/NoIntroProviderCooldownIntegrationTests.cs`

Integration tests verify both:

1. Cooldown persistence across provider instances and process restarts.
2. Explicit testing override behavior when persisted cooldown state exists.
