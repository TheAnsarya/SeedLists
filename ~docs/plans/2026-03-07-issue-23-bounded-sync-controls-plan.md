# Plan: Issue #23 - Add Bounded Sync Controls

## Issue

- GitHub: `#23`
- Title: `[2.2] Add bounded sync controls (max DATs and include filters)`

## Deliverables

- max DAT cap support
- include/exclude wildcard pattern filtering
- tests validating selection behavior
- runbook and configuration documentation

## Risks

- misconfigured patterns may over-filter expected sources

## Mitigation

- document evaluation order and wildcard rules clearly
- keep defaults permissive (no cap, no filters)
