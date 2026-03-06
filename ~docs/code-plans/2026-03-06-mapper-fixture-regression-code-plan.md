# Code Plan - Mapper Fixture Regression (Issue #15)

## Scope

Create durable provider fixture samples and regression tests for semantic mapper behavior.

## Steps

1. Add fixture files for TOSEC, No-Intro, and GoodTools payload patterns.

1. Configure test project to copy fixture files to test output.

1. Add file-based tests that load fixtures and assert canonical mapping output.

1. Document fixture corpus and maintenance approach.

## Done Criteria

- Fixture files committed in `tests/SeedLists.Dat.Tests/Fixtures/`
- Test suite validates fixture mapping paths
- Documentation published in `docs/MAPPER_FIXTURES.md`
