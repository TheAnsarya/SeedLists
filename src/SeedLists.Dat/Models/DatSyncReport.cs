namespace SeedLists.Dat.Models;

/// <summary>
/// Aggregate result of a provider sync run.
/// </summary>
public sealed record DatSyncReport {
	public required DatProviderKind Provider { get; init; }
	public required DateTimeOffset StartedAtUtc { get; init; }
	public required DateTimeOffset CompletedAtUtc { get; init; }
	public int DatsDiscovered { get; init; }
	public int DatsProcessed { get; init; }
	public int DatsFailed { get; init; }
	public IReadOnlyList<string> Errors { get; init; } = [];
}
