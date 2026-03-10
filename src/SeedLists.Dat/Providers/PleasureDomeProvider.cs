using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;

namespace SeedLists.Dat.Providers;

/// <summary>
/// Pleasuredome provider that discovers DAT archives for MAME and selected NonMAME categories.
/// </summary>
public sealed partial class PleasureDomeProvider(
	IOptions<SeedListsDatOptions> options,
	IDatSyncStateStore stateStore,
	IHttpClientFactory httpClientFactory) : IDatProvider {
	private static readonly string[] LocalExtensions = [".dat", ".zip", ".7z"];
	private static readonly string[] ZipPreferredExtensions = [".dat", ".xml", ".json", ".txt"];

	private readonly SeedListsDatOptions _options = options.Value;
	private readonly IDatSyncStateStore _stateStore = stateStore;
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

	public DatProviderKind ProviderType => DatProviderKind.PleasureDome;

	public async Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		var entries = new List<DatMetadata>();
		entries.AddRange(GetLocalDats());

		if (!_options.EnableInternetDownloads) {
			return entries;
		}

		if (_options.EnableRemoteVersionChecks) {
			var shouldPoll = await RemoteDatSupport.ShouldPollAsync(_stateStore, "pleasuredome", _options.RemotePollIntervalHours, cancellationToken);
			if (!shouldPoll) {
				return entries;
			}
		}

		try {
			var remoteEntries = await GetRemoteDatsAsync(cancellationToken);
			entries.AddRange(remoteEntries);
		} catch (Exception ex) when (ex is not OperationCanceledException) {
			// Keep local discovery usable when Pleasuredome is temporarily unavailable.
		} finally {
			if (_options.EnableRemoteVersionChecks) {
				await RemoteDatSupport.MarkPolledAsync(_stateStore, "pleasuredome", cancellationToken);
			}
		}

		return entries;
	}

	public async Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

		if (identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase)) {
			var path = identifier["local::".Length..];
			if (!File.Exists(path)) {
				throw new FileNotFoundException("Pleasuredome DAT source file not found.", path);
			}

			var extension = Path.GetExtension(path).ToLowerInvariant();
			if (extension == ".7z") {
				throw new NotSupportedException("Pleasuredome .7z archives are not extracted automatically. Extract to a local .dat file first.");
			}

			if (extension == ".zip") {
				return await ExtractDatFromZipAsync(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), path, cancellationToken);
			}

			return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		if (!RemoteDatSupport.TryParseRemoteIdentifier(identifier, out var token, out var remoteUrl)) {
			throw new NotSupportedException("Pleasuredome identifier must be local:: or remote|<token>|<url>.");
		}

		var client = _httpClientFactory.CreateClient(nameof(PleasureDomeProvider));
		var payload = await client.GetByteArrayAsync(remoteUrl, cancellationToken);
		await RemoteDatSupport.SetTokenAsync(_stateStore, "pleasuredome", remoteUrl, token, cancellationToken);

		var extensionFromUrl = Path.GetExtension(new Uri(remoteUrl).AbsolutePath).ToLowerInvariant();
		if (extensionFromUrl == ".zip") {
			return await ExtractDatFromZipAsync(new MemoryStream(payload, writable: false), remoteUrl, cancellationToken);
		}

		return new MemoryStream(payload, writable: false);
	}

	public bool SupportsIdentifier(string identifier) {
		return identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase)
			|| identifier.StartsWith("remote|", StringComparison.OrdinalIgnoreCase);
	}

	private IEnumerable<DatMetadata> GetLocalDats() {
		var root = _options.PleasureDomeLocalDirectory;
		if (!Directory.Exists(root)) {
			yield break;
		}

		foreach (var path in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories)
			.Where(path => LocalExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase))) {
			var info = new FileInfo(path);
			var extension = Path.GetExtension(path).ToLowerInvariant();
			var classification = extension switch {
				".zip" => "archive (zip)",
				".7z" => "archive (7z)",
				_ => "dat",
			};

			yield return new DatMetadata {
				Identifier = $"local::{path}",
				Name = Path.GetFileNameWithoutExtension(path),
				Description = $"Pleasuredome local {classification} source",
				System = ExtractSystemNameFromPath(path),
				FileSize = info.Length,
				LastUpdated = info.LastWriteTimeUtc,
			};
		}
	}

	private async Task<IReadOnlyList<DatMetadata>> GetRemoteDatsAsync(CancellationToken cancellationToken) {
		var client = _httpClientFactory.CreateClient(nameof(PleasureDomeProvider));
		var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		var results = new List<DatMetadata>();

		await AppendRemoteEntriesFromPageAsync(client, _options.PleasureDomeMameIndexUrl, "mame", seenUrls, results, cancellationToken);

		var nonMamePages = await GetNonMameCategoryPagesAsync(client, cancellationToken);
		foreach (var page in nonMamePages) {
			await AppendRemoteEntriesFromPageAsync(client, page.Url, page.Slug, seenUrls, results, cancellationToken);
		}

		return results;
	}

	private async Task<IReadOnlyList<PleasureDomePage>> GetNonMameCategoryPagesAsync(HttpClient client, CancellationToken cancellationToken) {
		var configuredSlugs = _options.PleasureDomeNonMameCategorySlugs
			.Where(slug => !string.IsNullOrWhiteSpace(slug))
			.Select(slug => slug.Trim().ToLowerInvariant())
			.ToHashSet(StringComparer.OrdinalIgnoreCase);

		var pages = new List<PleasureDomePage>();
		var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		try {
			var indexHtml = await client.GetStringAsync(_options.PleasureDomeNonMameIndexUrl, cancellationToken);
			foreach (Match match in NonMameCategoryLinkRegex().Matches(indexHtml)) {
				var slug = match.Groups["slug"].Value.Trim().ToLowerInvariant();
				if (configuredSlugs.Count > 0 && !configuredSlugs.Contains(slug)) {
					continue;
				}

				var href = match.Groups["href"].Value;
				var absolute = RemoteDatSupport.NormalizeUrl(_options.PleasureDomeNonMameIndexUrl, href);
				if (!seenUrls.Add(absolute)) {
					continue;
				}

				pages.Add(new PleasureDomePage(slug, absolute));
			}
		} catch (Exception ex) when (ex is not OperationCanceledException) {
			// Fall back to configured category slugs if category index fetch/parsing fails.
		}

		if (pages.Count == 0) {
			foreach (var slug in configuredSlugs) {
				var absolute = BuildNonMameCategoryPageUrl(slug);
				if (!seenUrls.Add(absolute)) {
					continue;
				}

				pages.Add(new PleasureDomePage(slug, absolute));
			}
		}

		return pages;
	}

	private string BuildNonMameCategoryPageUrl(string slug) {
		var baseUri = new Uri(_options.PleasureDomeNonMameIndexUrl);
		return new Uri(baseUri, $"{slug}/index.html").ToString();
	}

	private async Task AppendRemoteEntriesFromPageAsync(
		HttpClient client,
		string pageUrl,
		string categorySlug,
		HashSet<string> seenUrls,
		List<DatMetadata> results,
		CancellationToken cancellationToken) {
		var html = await client.GetStringAsync(pageUrl, cancellationToken);

		foreach (Match match in DatZipLinkRegex().Matches(html)) {
			var href = match.Groups["href"].Value;
			var remoteUrl = RemoteDatSupport.NormalizeUrl(pageUrl, href);
			if (!seenUrls.Add(remoteUrl)) {
				continue;
			}

			var fileName = Uri.UnescapeDataString(Path.GetFileName(new Uri(remoteUrl).AbsolutePath));
			if (string.IsNullOrWhiteSpace(fileName)) {
				continue;
			}

			if (_options.EnableRemoteVersionChecks) {
				var changed = await RemoteDatSupport.HasChangedAsync(_stateStore, "pleasuredome", remoteUrl, fileName, cancellationToken);
				if (!changed) {
					continue;
				}
			}

			var versionToken = TryExtractVersion(fileName) ?? fileName;
			results.Add(new DatMetadata {
				Identifier = RemoteDatSupport.BuildRemoteIdentifier(fileName, remoteUrl),
				Name = Path.GetFileNameWithoutExtension(fileName),
				Description = $"Pleasuredome {MapSystemName(categorySlug)} DAT source",
				Version = versionToken,
				System = MapSystemName(categorySlug),
				DownloadUrl = remoteUrl,
			});
		}
	}

	private static string MapSystemName(string categorySlug) {
		return categorySlug.Trim().ToLowerInvariant() switch {
			"mame" => "MAME",
			"fruitmachines" => "Fruit Machines",
			"pinball" => "Pinball",
			"raine" => "Raine",
			_ => categorySlug,
		};
	}

	private static string ExtractSystemNameFromPath(string path) {
		var directoryName = Directory.GetParent(path)?.Name;
		return string.IsNullOrWhiteSpace(directoryName) ? "Pleasuredome" : directoryName;
	}

	private static string? TryExtractVersion(string fileName) {
		var match = VersionRegex().Match(fileName);
		return match.Success ? match.Groups["version"].Value : null;
	}

	private static async Task<Stream> ExtractDatFromZipAsync(Stream archiveStream, string sourceName, CancellationToken cancellationToken) {
		await using var source = archiveStream;
		using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: false);

		var entry = archive.Entries
			.Where(item => !string.IsNullOrWhiteSpace(item.Name))
			.OrderBy(item => RankExtension(Path.GetExtension(item.FullName).ToLowerInvariant()))
			.FirstOrDefault(item => RankExtension(Path.GetExtension(item.FullName).ToLowerInvariant()) < int.MaxValue);

		if (entry is null) {
			throw new InvalidOperationException($"Pleasuredome archive '{sourceName}' does not contain a supported DAT payload entry.");
		}

		await using var entryStream = entry.Open();
		var output = new MemoryStream();
		await entryStream.CopyToAsync(output, cancellationToken);
		output.Position = 0;
		return output;
	}

	private static int RankExtension(string extension) {
		for (var i = 0; i < ZipPreferredExtensions.Length; i++) {
			if (string.Equals(ZipPreferredExtensions[i], extension, StringComparison.OrdinalIgnoreCase)) {
				return i;
			}
		}

		return int.MaxValue;
	}

	[GeneratedRegex("href\\s*=\\s*[\"'](?<href>[^\"']+\\.zip)[\"']", RegexOptions.IgnoreCase)]
	private static partial Regex DatZipLinkRegex();

	[GeneratedRegex("href\\s*=\\s*[\"'](?<href>[^\"']*/nonmame/(?<slug>[a-z0-9\\-]+)/index\\.html)[\"']", RegexOptions.IgnoreCase)]
	private static partial Regex NonMameCategoryLinkRegex();

	[GeneratedRegex("(?<version>v\\d+\\.\\d+|\\d{8}|\\d{4}-\\d{2}-\\d{2})", RegexOptions.IgnoreCase)]
	private static partial Regex VersionRegex();

	private sealed record PleasureDomePage(string Slug, string Url);
}
