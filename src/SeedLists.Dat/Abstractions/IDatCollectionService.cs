using SeedLists.Dat.Models;

namespace SeedLists.Dat.Abstractions;

/// <summary>
/// Orchestrates provider discovery, downloads, parsing, and on-disk output.
/// </summary>
public interface IDatCollectionService {
	Task<DatSyncReport> SyncProviderAsync(
		DatProviderKind provider,
		bool forceRefresh,
		IProgress<DatSyncProgress>? progress = null,
		CancellationToken cancellationToken = default);

	Task<IReadOnlyList<DatProviderKind>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default);
}
