namespace SeedLists.Dat.Models;

/// <summary>
/// Metadata returned by DAT providers.
/// </summary>
public sealed record DatMetadata {
	public required string Identifier { get; init; }
	public required string Name { get; init; }
	public string? Description { get; init; }
	public string? Version { get; init; }
	public string? System { get; init; }
	public string? DownloadUrl { get; init; }
	public long? FileSize { get; init; }
	public DateTimeOffset? LastUpdated { get; init; }
	public int? GameCount { get; init; }
}
