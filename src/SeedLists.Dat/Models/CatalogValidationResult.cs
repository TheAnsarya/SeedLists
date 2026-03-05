namespace SeedLists.Dat.Models;

/// <summary>
/// Result of validating a SeedLists JSON catalog payload.
/// </summary>
public sealed record CatalogValidationResult {
	public required bool IsValid { get; init; }
	public IReadOnlyList<string> Errors { get; init; } = [];
}
