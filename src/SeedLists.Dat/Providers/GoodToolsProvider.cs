using System.IO.Compression;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using Microsoft.Extensions.Options;

namespace SeedLists.Dat.Providers;

/// <summary>
/// GoodTools provider using local DAT/archives that are already available.
/// </summary>
public sealed class GoodToolsProvider(IOptions<SeedListsDatOptions> options) : IDatProvider {
	private static readonly string[] LocalExtensions = [".dat", ".zip", ".7z"];
	private static readonly string[] ZipPreferredExtensions = [".dat", ".txt", ".json", ".xml"];

	private readonly SeedListsDatOptions _options = options.Value;

	public DatProviderKind ProviderType => DatProviderKind.GoodTools;

	public Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		_ = cancellationToken;

		var root = _options.GoodToolsLocalDirectory;
		if (!Directory.Exists(root)) {
			return Task.FromResult<IReadOnlyList<DatMetadata>>([]);
		}

		var files = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
			.Where(path => LocalExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))
			.Select(path => {
				var info = new FileInfo(path);
				var extension = Path.GetExtension(path).ToLowerInvariant();
				var classification = extension switch {
					".zip" => "archive (zip)",
					".7z" => "archive (7z)",
					_ => "dat",
				};

				return new DatMetadata {
					Identifier = $"local::{path}",
					Name = Path.GetFileNameWithoutExtension(path),
					Description = $"GoodTools local {classification} source",
					System = ExtractSystemName(path),
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

		var extension = Path.GetExtension(path).ToLowerInvariant();
		if (extension == ".7z") {
			throw new NotSupportedException("GoodTools .7z archives are not extracted automatically. Extract to a local .dat file first.");
		}

		if (extension == ".zip") {
			return ExtractDatFromZip(path, cancellationToken);
		}

		Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		return Task.FromResult(stream);
	}

	public bool SupportsIdentifier(string identifier) {
		return identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase);
	}

	private static Task<Stream> ExtractDatFromZip(string path, CancellationToken cancellationToken) {
		using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: false);

		var entry = archive.Entries
			.Where(item => !string.IsNullOrWhiteSpace(item.Name))
			.OrderBy(item => RankExtension(Path.GetExtension(item.FullName).ToLowerInvariant()))
			.FirstOrDefault(item => RankExtension(Path.GetExtension(item.FullName).ToLowerInvariant()) < int.MaxValue);

		if (entry is null) {
			throw new InvalidOperationException($"GoodTools archive '{path}' does not contain a supported DAT payload entry.");
		}

		using var entryStream = entry.Open();
		var output = new MemoryStream();
		entryStream.CopyTo(output);
		output.Position = 0;
		_ = cancellationToken;
		return Task.FromResult<Stream>(output);
	}

	private static int RankExtension(string extension) {
		for (var i = 0; i < ZipPreferredExtensions.Length; i++) {
			if (string.Equals(ZipPreferredExtensions[i], extension, StringComparison.OrdinalIgnoreCase)) {
				return i;
			}
		}

		return int.MaxValue;
	}

	private static string ExtractSystemName(string path) {
		var directoryName = Directory.GetParent(path)?.Name;
		return string.IsNullOrWhiteSpace(directoryName) ? "GoodTools" : directoryName;
	}
}
