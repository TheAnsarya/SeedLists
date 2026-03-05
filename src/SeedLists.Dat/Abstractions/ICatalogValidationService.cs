using SeedLists.Dat.Models;

namespace SeedLists.Dat.Abstractions;

/// <summary>
/// Validates normalized JSON catalog payloads against SeedLists requirements.
/// </summary>
public interface ICatalogValidationService {
	CatalogValidationResult Validate(ReadOnlySpan<byte> jsonUtf8);
}
