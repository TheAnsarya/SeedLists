namespace SeedLists.Dat.Models;

/// <summary>
/// Progress payload for sync operations.
/// </summary>
public sealed record DatSyncProgress {
	public required DatProviderKind Provider { get; init; }
	public required DatSyncPhase Phase { get; init; }
	public string CurrentDat { get; init; } = string.Empty;
	public int ProcessedCount { get; init; }
	public int TotalCount { get; init; }
}
