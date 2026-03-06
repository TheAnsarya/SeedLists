# Provider Mapping Rules

Issues: `#13`, `#16`

## Purpose

Define how provider source payloads are converted to canonical SeedLists JSON catalogs.

## TOSEC and No-Intro (XML-like DAT text)

- Detection: non-JSON payload with `game`/`machine` + `rom` tags.
- Mapping:
  - `<game name="...">` -> `games[].name`
  - `<description>` -> `games[].description`
  - `<manufacturer>` -> `games[].publisher`
  - `<year>` -> `games[].year`
  - `<rom name="..." size="..." crc="..." md5="..." sha1="..." status="..." />` -> `games[].roms[]`
- Notes:
  - extraction is regex-based and tolerant of incomplete tags.
  - missing values remain null/default.

## GoodTools (text heuristics)

- Detection: non-JSON payload for provider `GoodTools`.
- Mapping:
  - line-level ROM filename matches (e.g., `.zip`, `.nes`, `.gb`, `.gba`, `.iso`) produce one game entry per line.
  - `games[].name` defaults to ROM filename without extension.
  - ROM `size` is set to `0` when unknown.

## Fallback

- For unsupported non-JSON payloads, a canonical wrapper is emitted with:
  - metadata fields (`name`, `provider`, `version`)
  - empty `games` array
  - `rawPreview` snippet for diagnostics
