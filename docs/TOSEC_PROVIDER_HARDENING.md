# TOSEC Provider Hardening

This document captures robustness improvements for TOSEC discovery and download handling.

## Discovery

- Local discovery now includes `.dat`, `.zip`, and `.7z` files.
- Remote archive links are parsed from TOSEC index HTML with relative and absolute URL support.
- Duplicate remote links are removed before returning metadata.
- Remote index failures fall back to local-only discovery when internet mode is enabled.

## Download and Archive Handling

- Remote and local `.zip` sources are automatically extracted to the preferred DAT payload entry.
- Entry selection preference: `.dat` -> `.xml` -> `.json` -> `.txt`.
- `.7z` sources are detected and rejected with a clear manual-extraction message.

## Retry/Error Handling

- Remote index fetches and payload downloads use transient retry logic.
- Retries apply to `HttpRequestException` and transient `TaskCanceledException` scenarios.
- Attempt count: 3
- Delay: 300 ms

## Test Coverage

See `tests/SeedLists.Dat.Tests/TosecProviderTests.cs` for:

- local + deduplicated remote discovery
- local-only fallback when remote index fails
- remote zip extraction
- transient retry success path
