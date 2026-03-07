# Bounded Sync Controls

Issue: `#23`

SeedLists supports bounded provider runs for safer targeted ingestion.

## Settings

`SeedListsDat:MaxDatsPerRun`

- `0` (default): no cap
- `> 0`: process at most N discovered DAT entries per provider run

`SeedListsDat:IncludeNamePatterns`

- optional wildcard patterns applied to source names
- if non-empty, only matching sources are eligible

`SeedListsDat:ExcludeNamePatterns`

- optional wildcard patterns applied after include filtering
- matching sources are removed from the run set

## Pattern Rules

- `*` matches any sequence
- `?` matches a single character
- matching is case-insensitive

## Evaluation Order

1. include patterns
2. exclude patterns
3. max cap

## Examples

```json
{
	"SeedListsDat": {
		"MaxDatsPerRun": 25,
		"IncludeNamePatterns": ["SNES*", "NES*"],
		"ExcludeNamePatterns": ["*Beta*", "*Prototype*"]
	}
}
```

## Validation

See `tests/SeedLists.Dat.Tests/DatCollectionServiceRunControlsTests.cs`.
