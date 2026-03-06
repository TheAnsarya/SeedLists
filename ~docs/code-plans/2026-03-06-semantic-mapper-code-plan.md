# Code Plan - Semantic Mapper Implementation

Issue: `#13`

## Scope

Add semantic extraction paths in `CatalogNormalizationService` before fallback wrapping.

## Implementation Steps

1. Branch mapper by provider kind.

1. Implement XML-like mapper for TOSEC/No-Intro:

- regex-match game blocks and ROM tags
- attribute extraction helper
- map to canonical JSON fields

1. Implement GoodTools text mapper:

- line scanning with ROM filename regex
- one game/rom entry per matched line

1. Preserve fallback wrapper for unmatched content.

1. Add tests for:

- XML-like extraction
- GoodTools extraction
- existing fallback/default behavior

## Risks

- Regex mapping can miss malformed edge formats.
- GoodTools mapping is heuristic and may over/under-capture lines.

## Mitigation

- Expand fixtures (`#15`)
- Keep fallback path with `rawPreview`
- Add benchmark/telemetry for normalization behavior (`#14`)
