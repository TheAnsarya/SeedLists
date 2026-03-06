# Code Plan: Issues #3 and #4 Provider Hardening

## Objective

Harden TOSEC and GoodTools provider ingestion paths with archive handling, discovery robustness, and targeted tests.

## Scope

- TOSEC:
	- add archive-aware local discovery
	- add remote discovery dedupe + fallback behavior
	- add retry handling for transient HTTP failures
	- add zip payload extraction and explicit `.7z` guardrails
- GoodTools:
	- expand metadata classification and system derivation
	- add zip payload extraction and explicit `.7z` guardrails
- tests for both providers

## Validation

- `dotnet test SeedLists.slnx -c Release`
- confirm provider tests cover retries, extraction, and classification paths
