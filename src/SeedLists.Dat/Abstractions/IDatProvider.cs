using SeedLists.Dat.Models;

namespace SeedLists.Dat.Abstractions;

/// <summary>
/// Contract implemented by each DAT source provider.
/// </summary>
public interface IDatProvider {
	DatProviderKind ProviderType { get; }
	Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default);
	Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default);
	bool SupportsIdentifier(string identifier);
}
