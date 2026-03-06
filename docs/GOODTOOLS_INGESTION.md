# GoodTools Ingestion Expansion

This document captures local GoodTools ingestion and classification behavior.

## Local Source Discovery

GoodTools discovery includes:

- `.dat`
- `.zip`
- `.7z`

Each discovered source emits metadata with:

- classification-based description (`dat`, `archive (zip)`, `archive (7z)`)
- derived `System` from parent folder name when available
- file size and last-updated timestamp

## File-Type Handling

- `.dat` files are streamed directly.
- `.zip` archives are extracted to a preferred DAT payload entry.
- `.7z` archives are rejected with a clear manual-extraction message.

ZIP entry preference order:

1. `.dat`
2. `.txt`
3. `.json`
4. `.xml`

## Test Coverage

See `tests/SeedLists.Dat.Tests/GoodToolsProviderTests.cs` for:

- metadata classification and system derivation
- zip extraction behavior
- `.7z` not-supported guardrails
