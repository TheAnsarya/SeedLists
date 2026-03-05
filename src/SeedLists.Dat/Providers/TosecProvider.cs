using System.Text.RegularExpressions;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using Microsoft.Extensions.Options;

namespace SeedLists.Dat.Providers;

/// <summary>
/// TOSEC provider backed by local DATs and optional internet indexing.
/// </summary>
public sealed class TosecProvider(IOptions<SeedListsDatOptions> options, IHttpClientFactory httpClientFactory) : IDatProvider {
	private readonly SeedListsDatOptions _options = options.Value;
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

	public DatProviderKind ProviderType => DatProviderKind.Tosec;

	public async Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		var entries = new List<DatMetadata>();
		entries.AddRange(GetLocalDats());

		if (_options.EnableInternetDownloads) {
			var remote = await GetRemoteArchiveLinksAsync(cancellationToken);
			entries.AddRange(remote);
		}

		return entries;
	}

	public async Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

		if (identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase)) {
			var path = identifier["local::".Length..];
			if (!File.Exists(path)) {
				throw new FileNotFoundException("TOSEC local DAT source not found.", path);
			}

			return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		if (!identifier.StartsWith("url::", StringComparison.OrdinalIgnoreCase)) {
			throw new NotSupportedException("TOSEC identifier must be local:: or url::.");
		}

		if (!_options.EnableInternetDownloads) {
			throw new InvalidOperationException("Internet downloads are disabled. Enable SeedListsDat:EnableInternetDownloads.");
		}

		var url = identifier["url::".Length..];
		var client = _httpClientFactory.CreateClient(nameof(TosecProvider));
		var bytes = await client.GetByteArrayAsync(url, cancellationToken);
		return new MemoryStream(bytes, writable: false);
	}

	public bool SupportsIdentifier(string identifier) {
		return identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase)
			|| identifier.StartsWith("url::", StringComparison.OrdinalIgnoreCase);
	}

	private IEnumerable<DatMetadata> GetLocalDats() {
		if (!Directory.Exists(_options.TosecLocalDirectory)) {
			yield break;
		}

		foreach (var path in Directory.EnumerateFiles(_options.TosecLocalDirectory, "*.dat", SearchOption.AllDirectories)) {
			var info = new FileInfo(path);
			yield return new DatMetadata {
				Identifier = $"local::{path}",
				Name = Path.GetFileNameWithoutExtension(path),
				Description = "TOSEC local DAT source",
				System = ExtractSystemName(path),
				FileSize = info.Length,
				LastUpdated = info.LastWriteTimeUtc,
			};
		}
	}

	private async Task<IReadOnlyList<DatMetadata>> GetRemoteArchiveLinksAsync(CancellationToken cancellationToken) {
		var client = _httpClientFactory.CreateClient(nameof(TosecProvider));
		var html = await client.GetStringAsync(_options.TosecDatFilesUrl, cancellationToken);

		var links = new List<DatMetadata>();
		var pattern = "href=\"(?<link>[^\"]*(tosec|TOSEC)[^\"]*\\.(zip|7z))\"";
		var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

		foreach (Match match in matches) {
			var link = match.Groups["link"].Value;
			if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)) {
				uri = new Uri(new Uri(_options.TosecBaseUrl), link);
			}

			links.Add(new DatMetadata {
				Identifier = $"url::{uri}",
				Name = Path.GetFileNameWithoutExtension(uri.AbsolutePath),
				Description = "TOSEC remote DAT archive",
				System = "TOSEC",
				DownloadUrl = uri.ToString(),
			});
		}

		return links;
	}

	private static string ExtractSystemName(string path) {
		var file = Path.GetFileNameWithoutExtension(path);
		var split = file.Split(" - ");
		return split.Length > 0 ? split[0] : "TOSEC";
	}
}
