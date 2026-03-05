using SeedLists.Dat.Models;

namespace SeedLists.Dat.Abstractions;

/// <summary>
/// Resolves an appropriate parser for an input DAT payload.
/// </summary>
public interface IDatParserFactory {
	IDatParser? GetParser(string filePath);
	IDatParser? GetParser(DatFormat format);
	IDatParser? GetParser(Stream stream);
}
