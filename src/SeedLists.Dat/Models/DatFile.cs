namespace SeedLists.Dat.Models;

/// <summary>
/// Parsed DAT payload containing header and game records.
/// </summary>
public sealed class DatFile {
	public required string FileName { get; init; }
	public required string Name { get; init; }
	public DatProviderKind Provider { get; init; }
	public DatFormat Format { get; init; }
	public string? Description { get; init; }
	public string? Version { get; init; }
	public string? Author { get; init; }
	public string? Homepage { get; init; }
	public string? System { get; init; }
	public DateTimeOffset ParsedAtUtc { get; init; } = DateTimeOffset.UtcNow;
	public List<GameEntry> Games { get; } = [];
}
