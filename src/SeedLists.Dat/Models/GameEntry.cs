namespace SeedLists.Dat.Models;

/// <summary>
/// A game entry and its ROM list in a DAT.
/// </summary>
public sealed class GameEntry {
	public required string Name { get; init; }
	public string? Description { get; set; }
	public string? Publisher { get; set; }
	public string? Year { get; set; }
	public List<RomEntry> Roms { get; } = [];
}
