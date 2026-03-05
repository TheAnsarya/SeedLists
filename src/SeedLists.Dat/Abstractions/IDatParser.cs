using SeedLists.Dat.Models;

namespace SeedLists.Dat.Abstractions;

/// <summary>
/// Contract for parsing DAT content into typed objects.
/// </summary>
public interface IDatParser {
	DatFormat Format { get; }
	bool CanParse(string filePath);
	Task<DatFile> ParseAsync(string filePath, IProgress<DatParseProgress>? progress = null, CancellationToken cancellationToken = default);
	Task<DatFile> ParseAsync(Stream stream, string fileName, IProgress<DatParseProgress>? progress = null, CancellationToken cancellationToken = default);
}
