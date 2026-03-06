# Code Plan: Issue #5 No-Intro Cooldown Persistence

## Objective

Finalize strict No-Intro cooldown enforcement with persistent-state integration tests and explicit testing override validation.

## Scope

- Add integration tests that use `FileDatSyncStateStore` and real on-disk state files.
- Validate cross-instance cooldown lockout behavior.
- Validate explicit testing override bypass with persisted cooldown state.
- Document policy and persistent state details.

## Validation

- `dotnet test SeedLists.slnx -c Release`
- Verify integration tests pass and state file key is present.
