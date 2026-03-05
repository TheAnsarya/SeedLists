using SeedLists.Dat.Models;

namespace SeedLists.Dat.Abstractions;

/// <summary>
/// Converts provider payloads into canonical SeedLists JSON catalog format.
/// </summary>
public interface ICatalogNormalizationService {
	byte[] Normalize(ReadOnlySpan<byte> payload, DatProviderKind provider, string sourceName);
}
