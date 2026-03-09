using System.IO.Compression;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;

namespace SeedLists.Dat.Providers;

/// <summary>
/// MESS provider using local DAT/archives and optional remote DAT package polling.
/// </summary>
public sealed partial class MessProvider(
	IOptions<SeedListsDatOptions> options,
	IDatSyncStateStore stateStore,
	IHttpClientFactory httpClientFactory) : IDatProvider {
	private static readonly string[] LocalExtensions = [".dat", ".zip", ".7z"];
	private static readonly string[] ZipPreferredExtensions = [".dat", ".xml", ".json", ".txt"];

	private readonly SeedListsDatOptions _options = options.Value;
	private readonly IDatSyncStateStore _stateStore = stateStore;
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

	public DatProviderKind ProviderType => DatProviderKind.Mess;

	public async Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		var entries = new List<DatMetadata>();
		entries.AddRange(GetLocalDats());

		if (!_options.EnableInternetDownloads) {
			return entries;
		}

		if (_options.EnableRemoteVersionChecks) {
			var shouldPoll = await RemoteDatSupport.ShouldPollAsync(_stateStore, "mess", _options.RemotePollIntervalHours, cancellationToken);
			if (!shouldPoll) {
				return entries;
			}
		}

		try {
			var remoteEntries = await GetRemoteDatsAsync(cancellationToken);
			entries.AddRange(remoteEntries);
		} catch (Exception ex) when (ex is not OperationCanceledException) {
			// Keep local discovery usable if remote indexing is temporarily unavailable.
		} finally {
			if (_options.EnableRemoteVersionChecks) {
				await RemoteDatSupport.MarkPolledAsync(_stateStore, "mess", cancellationToken);
			}
		}

		return entries;
	}

	public async Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

		if (identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase)) {
			var path = identifier["local::".Length..];
			if (!File.Exists(path)) {
				throw new FileNotFoundException("MESS DAT source file not found.", path);
			}

			var extension = Path.GetExtension(path).ToLowerInvariant();
			if (extension == ".7z") {
				throw new NotSupportedException("MESS .7z archives are not extracted automatically. Extract to a local .dat file first.");
			}

			if (extension == ".zip") {
				return await ExtractDatFromZip(path, cancellationToken);
			}

			Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			return stream;
		}

		if (!RemoteDatSupport.TryParseRemoteIdentifier(identifier, out var token, out var remoteUrl)) {
			throw new NotSupportedException("MESS identifier must be local:: or remote|<token>|<url>.");
		}

		var client = _httpClientFactory.CreateClient(nameof(MessProvider));
		var payload = await client.GetByteArrayAsync(remoteUrl, cancellationToken);
		await RemoteDatSupport.SetTokenAsync(_stateStore, "mess", remoteUrl, token, cancellationToken);

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
		var root = _options.MessLocalDirectory;
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
				Description = $"MESS local {classification} source",
				System = ExtractSystemName(path),
				FileSize = info.Length,
				LastUpdated = info.LastWriteTimeUtc,
			};
		}
	}

	private async Task<IReadOnlyList<DatMetadata>> GetRemoteDatsAsync(CancellationToken cancellationToken) {
		var client = _httpClientFactory.CreateClient(nameof(MessProvider));
		var html = await client.GetStringAsync(_options.MessRemoteIndexUrl, cancellationToken);

		var links = MessDatLinkRegex().Matches(html)
			.Select(match => match.Groups["href"].Value)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.ToList();

		var results = new List<DatMetadata>();
		foreach (var href in links) {
			var remoteUrl = RemoteDatSupport.NormalizeUrl(_options.MessRemoteIndexUrl, href);
			var fileName = Path.GetFileName(new Uri(remoteUrl).AbsolutePath);
			var versionToken = TryExtractVersion(fileName) ?? fileName;

			if (_options.EnableRemoteVersionChecks) {
				var changed = await RemoteDatSupport.HasChangedAsync(_stateStore, "mess", remoteUrl, versionToken, cancellationToken);
				if (!changed) {
					continue;
				}
			}

			results.Add(new DatMetadata {
				Identifier = RemoteDatSupport.BuildRemoteIdentifier(versionToken, remoteUrl),
				Name = Path.GetFileNameWithoutExtension(fileName),
				Description = fileName.Contains("SL_", StringComparison.OrdinalIgnoreCase)
					? "MESS software-list remote DAT package"
					: "MESS remote DAT package",
				Version = versionToken,
				System = "MESS",
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
			throw new InvalidOperationException($"MESS archive '{path}' does not contain a supported DAT payload entry.");
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
			throw new InvalidOperationException($"MESS archive '{sourceName}' does not contain a supported DAT payload entry.");
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
		return string.IsNullOrWhiteSpace(directoryName) ? "MESS" : directoryName;
	}

	private static string? TryExtractVersion(string fileName) {
		var match = VersionRegex().Match(fileName);
		return match.Success ? match.Groups["version"].Value : null;
	}

	[GeneratedRegex("href\\s*=\\s*\\\"(?<href>/download/\\\\?tipo=dat_resource&amp;file=/dats/cmdats/(?:pS_MESS_DATs|pS_SL_AllProject)[^\\\"]+\\\\.zip)\\\"", RegexOptions.IgnoreCase)]
	private static partial Regex MessDatLinkRegex();

	[GeneratedRegex("(?<version>\\d+\\.\\d+|\\d{8})", RegexOptions.IgnoreCase)]
	private static partial Regex VersionRegex();
}
