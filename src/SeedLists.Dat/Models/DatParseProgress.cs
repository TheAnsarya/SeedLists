namespace SeedLists.Dat.Models;

/// <summary>
/// Progress payload for DAT parsing.
/// </summary>
public sealed record DatParseProgress {
	public required string Phase { get; init; }
	public int GamesParsed { get; init; }
	public int RomsParsed { get; init; }
	public long BytesRead { get; init; }
	public long? TotalBytes { get; init; }
}
