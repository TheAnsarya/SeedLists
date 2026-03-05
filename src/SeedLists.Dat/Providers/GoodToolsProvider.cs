using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using Microsoft.Extensions.Options;

namespace SeedLists.Dat.Providers;

/// <summary>
/// GoodTools provider using local DAT/archives that are already available.
/// </summary>
public sealed class GoodToolsProvider(IOptions<SeedListsDatOptions> options) : IDatProvider {
	private readonly SeedListsDatOptions _options = options.Value;

	public DatProviderKind ProviderType => DatProviderKind.GoodTools;

	public Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		var root = _options.GoodToolsLocalDirectory;
		if (!Directory.Exists(root)) {
			return Task.FromResult<IReadOnlyList<DatMetadata>>([]);
		}

		var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
			.Where(path => path.EndsWith(".dat", StringComparison.OrdinalIgnoreCase)
				|| path.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
				|| path.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
			.Select(path => {
				var info = new FileInfo(path);
				return new DatMetadata {
					Identifier = $"local::{path}",
					Name = Path.GetFileNameWithoutExtension(path),
					Description = "GoodTools local DAT source",
					System = "GoodTools",
					DownloadUrl = null,
					FileSize = info.Length,
					LastUpdated = info.LastWriteTimeUtc,
				};
			})
			.ToList();

		return Task.FromResult<IReadOnlyList<DatMetadata>>(files);
	}

	public Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

		if (!identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase)) {
			throw new NotSupportedException("GoodTools provider currently supports only local identifiers.");
		}

		var path = identifier["local::".Length..];
		if (!File.Exists(path)) {
			throw new FileNotFoundException("GoodTools DAT source file not found.", path);
		}

		Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		return Task.FromResult(stream);
	}

	public bool SupportsIdentifier(string identifier) {
		return identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase);
	}
}
