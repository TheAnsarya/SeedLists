# SeedLists Ecosystem Guide

Issue: `#25`

This guide explains how to use SeedLists as a full ingestion ecosystem, not just as a command invocation.

## Who This Is For

- operators maintaining ROM catalog ingestion pipelines
- maintainers evolving provider logic and policies
- integrators consuming normalized outputs and run artifacts

## Mental Model

SeedLists is a staged pipeline with explicit contracts between steps:

1. provider discovery and source acquisition
2. canonical JSON normalization
3. validation against runtime schema rules
4. typed parse into internal models
5. artifact emission (normalized payload, summary, manifest)

Treat each stage as observable and testable.

## Recommended Environment Layout

- keep provider source roots stable over time
- separate raw provider sources from SeedLists output directories
- back up output and state directories if runs are part of critical workflows

Suggested separation:

- provider source roots (TOSEC, GoodTools, No-Intro local)
- SeedLists output root (`SeedListsDat:OutputDirectory`)
- SeedLists state root (`SeedListsDat:StateDirectory`)

## Configuration Strategy

Start conservative, then expand:

1. local-only mode:
	- `EnableInternetDownloads = false`
2. bounded onboarding mode:
	- set `MaxDatsPerRun`
	- set include/exclude source-name patterns
3. expanded production mode:
	- relax caps as confidence grows
	- monitor manifests and summaries continuously

No-Intro-specific safety:

- keep remote mode guarded
- respect mandatory 24-hour cooldown
- use testing override only in integration/test environments

## End-to-End Operational Workflow

1. Build and test before changing ingest behavior.
2. Run bounded syncs to verify normalization and validation stability.
3. Inspect `latest-sync-manifest.json` for each provider.
4. Review failed entries and source-level errors.
5. Expand scope gradually by adjusting run controls.
6. Capture benchmark and test evidence when changing hot paths.

## What Happens to Input Data

Input data is never used directly as final downstream contract output.

Instead:

- provider payloads are transformed into canonical JSON catalogs
- canonical catalogs are validated
- validated catalogs are parsed into typed models
- normalized and summary artifacts are persisted for inspection

Malformed/unmappable payloads are wrapped into safe canonical envelopes with `rawPreview` diagnostics instead of failing silently.

## How to Get the Best Results

- prefer local curated source sets before enabling remote ingestion
- use include/exclude patterns to isolate systems or riskier datasets
- watch manifest deltas over time to spot regressions
- keep fixture corpus and malformed fixture tests updated with mapper changes
- benchmark parser and normalization paths before claiming performance wins

## Failure Handling Playbook

- validation failures:
	- inspect canonical payload and schema constraints (`JSON_SCHEMA.md`)
- provider source issues:
	- inspect source-level status and error fields in sync manifests
- No-Intro cooldown errors:
	- wait for cooldown window or use test override in non-production contexts
- repeated ingestion failures:
	- reduce run scope with bounded controls, then iterate on failing subset

## Outputs You Should Automate Against

- `*.summary.json` for quick content-level verification
- `run-manifests/latest-sync-manifest.json` for dashboards, alerts, and automation
- historical run manifests for trend and incident analysis

## Related Guides

- `DOCUMENTATION_INDEX.md`
- `INGESTION_RUNBOOK.md`
- `SYNC_MANIFESTS.md`
- `BOUNDED_SYNC_CONTROLS.md`
- `CONVERSION_STRATEGY.md`
