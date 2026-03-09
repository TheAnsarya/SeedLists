using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;

namespace SeedLists.Dat.Providers;

/// <summary>
/// Redump provider using local DAT/archives and optional remote DAT link polling.
/// </summary>
public sealed partial class RedumpProvider(
	IOptions<SeedListsDatOptions> options,
	IDatSyncStateStore stateStore,
	IHttpClientFactory httpClientFactory) : IDatProvider {
	private static readonly string[] LocalExtensions = [".dat", ".zip", ".7z"];
	private static readonly string[] ZipPreferredExtensions = [".dat", ".xml", ".json", ".txt"];

	private readonly SeedListsDatOptions _options = options.Value;
	private readonly IDatSyncStateStore _stateStore = stateStore;
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

	public DatProviderKind ProviderType => DatProviderKind.Redump;

	public async Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		var entries = new List<DatMetadata>();
		entries.AddRange(GetLocalDats());

		if (!_options.EnableInternetDownloads) {
			return entries;
		}

		if (_options.EnableRemoteVersionChecks) {
			var shouldPoll = await RemoteDatSupport.ShouldPollAsync(_stateStore, "redump", _options.RemotePollIntervalHours, cancellationToken);
			if (!shouldPoll) {
				return entries;
			}
		}

		try {
			var remoteEntries = await GetRemoteDatsAsync(cancellationToken);
			entries.AddRange(remoteEntries);
		} catch (Exception ex) when (ex is not OperationCanceledException) {
			// Keep local discovery usable if remote Redump indexing is unavailable.
		} finally {
			if (_options.EnableRemoteVersionChecks) {
				await RemoteDatSupport.MarkPolledAsync(_stateStore, "redump", cancellationToken);
			}
		}

		return entries;
	}

	public async Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

		if (identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase)) {
			var path = identifier["local::".Length..];
			if (!File.Exists(path)) {
				throw new FileNotFoundException("Redump DAT source file not found.", path);
			}

			var extension = Path.GetExtension(path).ToLowerInvariant();
			if (extension == ".7z") {
				throw new NotSupportedException("Redump .7z archives are not extracted automatically. Extract to a local .dat file first.");
			}

			if (extension == ".zip") {
				return await ExtractDatFromZip(path, cancellationToken);
			}

			Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return stream;
		}

		if (!RemoteDatSupport.TryParseRemoteIdentifier(identifier, out var token, out var remoteUrl)) {
			throw new NotSupportedException("Redump identifier must be local:: or remote|<token>|<url>.");
		}

		var client = _httpClientFactory.CreateClient(nameof(RedumpProvider));
		var payload = await client.GetByteArrayAsync(remoteUrl, cancellationToken);
		await RemoteDatSupport.SetTokenAsync(_stateStore, "redump", remoteUrl, token, cancellationToken);

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
		var root = _options.RedumpLocalDirectory;
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
				Description = $"Redump local {classification} source",
				System = ExtractSystemName(path),
				FileSize = info.Length,
				LastUpdated = info.LastWriteTimeUtc,
			};
		}
	}

	private async Task<IReadOnlyList<DatMetadata>> GetRemoteDatsAsync(CancellationToken cancellationToken) {
		var urls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var url in _options.RedumpRemoteDatUrls) {
			if (!string.IsNullOrWhiteSpace(url)) {
				urls.Add(url.Trim());
			}
		}

		try {
			var client = _httpClientFactory.CreateClient(nameof(RedumpProvider));
			var html = await client.GetStringAsync(_options.RedumpRemoteIndexUrl, cancellationToken);
			foreach (Match match in RedumpDatLinkRegex().Matches(html)) {
				var href = match.Groups["href"].Value;
				var absolute = RemoteDatSupport.NormalizeUrl(_options.RedumpRemoteIndexUrl, href);
				urls.Add(absolute);
			}
		} catch (Exception ex) when (ex is not OperationCanceledException) {
			// Ignore index scraping failures if configured direct URLs still exist.
		}

		var results = new List<DatMetadata>();
		foreach (var remoteUrl in urls) {
			var token = Path.GetFileName(new Uri(remoteUrl).AbsolutePath);
			if (_options.EnableRemoteVersionChecks) {
				var changed = await RemoteDatSupport.HasChangedAsync(_stateStore, "redump", remoteUrl, token, cancellationToken);
				if (!changed) {
					continue;
				}
			}

			results.Add(new DatMetadata {
				Identifier = RemoteDatSupport.BuildRemoteIdentifier(token, remoteUrl),
				Name = Path.GetFileNameWithoutExtension(token),
				Description = "Redump remote DAT source",
				Version = token,
				System = "Redump",
				DownloadUrl = remoteUrl,
			});
		}

		return results;
	}

	private static Task<Stream> ExtractDatFromZip(string path, CancellationToken cancellationToken) {
		using var source = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: false);

		var entry = archive.Entries
			.Where(item => !string.IsNullOrWhiteSpace(item.Name))
			.OrderBy(item => RankExtension(Path.GetExtension(item.FullName).ToLowerInvariant()))
			.FirstOrDefault(item => RankExtension(Path.GetExtension(item.FullName).ToLowerInvariant()) < int.MaxValue);

		if (entry is null) {
			throw new InvalidOperationException($"Redump archive '{path}' does not contain a supported DAT payload entry.");
		}

		using var entryStream = entry.Open();
		var output = new MemoryStream();
		entryStream.CopyTo(output);
		output.Position = 0;
		_ = cancellationToken;
		return Task.FromResult<Stream>(output);
	}

	private static async Task<Stream> ExtractDatFromZipAsync(Stream archiveStream, string sourceName, CancellationToken cancellationToken) {
		await using var source = archiveStream;
		using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: false);

		var entry = archive.Entries
			.Where(item => !string.IsNullOrWhiteSpace(item.Name))
			.OrderBy(item => RankExtension(Path.GetExtension(item.FullName).ToLowerInvariant()))
			.FirstOrDefault(item => RankExtension(Path.GetExtension(item.FullName).ToLowerInvariant()) < int.MaxValue);

		if (entry is null) {
			throw new InvalidOperationException($"Redump archive '{sourceName}' does not contain a supported DAT payload entry.");
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

	private static string ExtractSystemName(string path) {
		var directoryName = Directory.GetParent(path)?.Name;
		return string.IsNullOrWhiteSpace(directoryName) ? "Redump" : directoryName;
	}

	[GeneratedRegex("href\\s*=\\s*\\\"(?<href>[^\\\"]*(?:dat|download)[^\\\"]*\\.(?:dat|zip))\\\"", RegexOptions.IgnoreCase)]
	private static partial Regex RedumpDatLinkRegex();
}
