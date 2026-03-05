namespace SeedLists.Dat.Models;

/// <summary>
/// A single ROM entry in a DAT game block.
/// </summary>
public sealed class RomEntry {
	public required string Name { get; init; }
	public long Size { get; init; }
	public string? Crc32 { get; init; }
	public string? Md5 { get; init; }
	public string? Sha1 { get; init; }
	public string? Status { get; init; }
}
