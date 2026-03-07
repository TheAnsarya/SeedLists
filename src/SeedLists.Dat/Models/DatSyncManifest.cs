namespace SeedLists.Dat.Models;

/// <summary>
/// Persisted run artifact describing a provider sync execution.
/// </summary>
public sealed record DatSyncManifest {
	public required string RunId { get; init; }
	public required DatProviderKind Provider { get; init; }
	public required DateTimeOffset StartedAtUtc { get; init; }
	public required DateTimeOffset CompletedAtUtc { get; init; }
	public long ElapsedMilliseconds { get; init; }
	public int DatsDiscovered { get; init; }
	public int DatsProcessed { get; init; }
	public int DatsFailed { get; init; }
	public IReadOnlyList<string> Errors { get; init; } = [];
	public IReadOnlyList<DatSyncManifestSource> Sources { get; init; } = [];
}

/// <summary>
/// Per-source summary entry inside a sync manifest.
/// </summary>
public sealed record DatSyncManifestSource {
	public required string Identifier { get; init; }
	public required string Name { get; init; }
	public string? Description { get; init; }
	public string? Version { get; init; }
	public string? System { get; init; }
	public string? DownloadUrl { get; init; }
	public long? FileSize { get; init; }
	public DateTimeOffset? LastUpdated { get; init; }
	public string Status { get; init; } = "pending";
	public string? Error { get; init; }
}
