# Plan - Issue #13 Semantic Mappers

## Goal

Implement provider-specific extraction that produces meaningful canonical JSON `games/roms` entries.

## Delivered in This Session

- Regex mapper for TOSEC and No-Intro XML-like DAT text.
- Heuristic line mapper for GoodTools text payloads.
- Tests for mapper behavior.
- Documentation for mapping rules and fallback behavior.

## Next Steps

- Expand fixture corpus (`#15`) with real-world DAT samples.
- Add benchmark coverage for normalization mapper throughput (`#14`).
- Refine GoodTools parser with checksum/token extraction where available.
