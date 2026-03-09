using System.IO.Compression;
using System.Text.RegularExpressions;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using Microsoft.Extensions.Options;

namespace SeedLists.Dat.Providers;

/// <summary>
/// TOSEC provider backed by local DATs and optional internet indexing.
/// </summary>
public sealed class TosecProvider(
	IOptions<SeedListsDatOptions> options,
	IHttpClientFactory httpClientFactory,
	IDatSyncStateStore stateStore) : IDatProvider {
	private const int MaxHttpAttempts = 3;
	private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(300);
	private static readonly string[] LocalExtensions = [".dat", ".zip", ".7z"];
	private static readonly string[] ZipPreferredExtensions = [".dat", ".xml", ".json", ".txt"];

	private readonly SeedListsDatOptions _options = options.Value;
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;
	private readonly IDatSyncStateStore _stateStore = stateStore;

	public DatProviderKind ProviderType => DatProviderKind.Tosec;

	public async Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		var entries = new List<DatMetadata>();
		entries.AddRange(GetLocalDats());

		if (_options.EnableInternetDownloads) {
			if (_options.EnableRemoteVersionChecks) {
				var shouldPoll = await RemoteDatSupport.ShouldPollAsync(_stateStore, "tosec", _options.RemotePollIntervalHours, cancellationToken);
				if (!shouldPoll) {
					return entries;
				}
			}

			try {
				var remote = await GetRemoteArchiveLinksAsync(cancellationToken);
				entries.AddRange(remote);
			} catch (Exception ex) when (ex is not OperationCanceledException) {
				// Keep local discovery usable if TOSEC indexing is temporarily unavailable.
			} finally {
				if (_options.EnableRemoteVersionChecks) {
					await RemoteDatSupport.MarkPolledAsync(_stateStore, "tosec", cancellationToken);
				}
			}
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

			var localExtension = Path.GetExtension(path).ToLowerInvariant();
			return localExtension switch {
				".zip" => await ExtractDatFromZipAsync(new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read), path, cancellationToken),
				".7z" => throw new NotSupportedException("TOSEC .7z archives are not extracted automatically. Extract to a local .dat file first."),
				_ => new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read),
			};
		}

		if (!_options.EnableInternetDownloads) {
			throw new InvalidOperationException("Internet downloads are disabled. Enable SeedListsDat:EnableInternetDownloads.");
		}

		string url;
		string? remoteToken = null;
		if (identifier.StartsWith("url::", StringComparison.OrdinalIgnoreCase)) {
			url = identifier["url::".Length..];
		} else if (RemoteDatSupport.TryParseRemoteIdentifier(identifier, out var parsedToken, out var remoteUrl)) {
			remoteToken = parsedToken;
			url = remoteUrl;
		} else {
			throw new NotSupportedException("TOSEC identifier must be local::, url::, or remote|<token>|<url>.");
		}

		var bytes = await DownloadWithRetryAsync(url, cancellationToken);
		var remoteExtension = TryGetUriExtension(url);
		if (!string.IsNullOrWhiteSpace(remoteToken)) {
			await RemoteDatSupport.SetTokenAsync(_stateStore, "tosec", url, remoteToken, cancellationToken);
		}

		return remoteExtension switch {
			".zip" => await ExtractDatFromZipAsync(new MemoryStream(bytes, writable: false), url, cancellationToken),
			".7z" => throw new NotSupportedException("TOSEC .7z archives are not extracted automatically. Download and extract locally first."),
			_ => new MemoryStream(bytes, writable: false),
		};
	}

	public bool SupportsIdentifier(string identifier) {
		return identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase)
			|| identifier.StartsWith("url::", StringComparison.OrdinalIgnoreCase)
			|| identifier.StartsWith("remote|", StringComparison.OrdinalIgnoreCase);
	}

	private IEnumerable<DatMetadata> GetLocalDats() {
		if (!Directory.Exists(_options.TosecLocalDirectory)) {
			yield break;
		}

		foreach (var path in Directory.EnumerateFiles(_options.TosecLocalDirectory, "*.*", SearchOption.AllDirectories)
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
				Description = $"TOSEC local {classification} source",
				System = ExtractSystemName(path),
				FileSize = info.Length,
				LastUpdated = info.LastWriteTimeUtc,
			};
		}
	}

	private async Task<IReadOnlyList<DatMetadata>> GetRemoteArchiveLinksAsync(CancellationToken cancellationToken) {
		var html = await DownloadStringWithRetryAsync(_options.TosecDatFilesUrl, cancellationToken);

		var links = new List<DatMetadata>();
		var pattern = "href\\s*=\\s*[\"'](?<link>[^\"']*(?:tosec)[^\"']*\\.(?:zip|7z))[\"']";
		var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);
		var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		foreach (Match match in matches) {
			var link = match.Groups["link"].Value;
			if (!Uri.TryCreate(link, UriKind.Absolute, out var uri)) {
				uri = new Uri(new Uri(_options.TosecBaseUrl), link);
			}

			if (!seen.Add(uri.ToString())) {
				continue;
			}

			var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();

			var remoteUrl = uri.ToString();
			var fileToken = Path.GetFileName(uri.AbsolutePath);
			if (_options.EnableRemoteVersionChecks) {
				var changed = await RemoteDatSupport.HasChangedAsync(_stateStore, "tosec", remoteUrl, fileToken, cancellationToken);
				if (!changed) {
					continue;
				}
			}

			links.Add(new DatMetadata {
				Identifier = RemoteDatSupport.BuildRemoteIdentifier(fileToken, remoteUrl),
				Name = Path.GetFileNameWithoutExtension(uri.AbsolutePath),
				Description = extension switch {
					".7z" => "TOSEC remote DAT archive (7z)",
					_ => "TOSEC remote DAT archive (zip)",
				},
				Version = fileToken,
				System = "TOSEC",
				DownloadUrl = remoteUrl,
			});
		}

		return links;
	}

	private async Task<byte[]> DownloadWithRetryAsync(string url, CancellationToken cancellationToken) {
		var client = _httpClientFactory.CreateClient(nameof(TosecProvider));

		for (var attempt = 1; attempt <= MaxHttpAttempts; attempt++) {
			try {
				return await client.GetByteArrayAsync(url, cancellationToken);
			} catch (Exception ex) when (attempt < MaxHttpAttempts && IsTransient(ex, cancellationToken)) {
				await Task.Delay(RetryDelay, cancellationToken);
			}
		}

		throw new InvalidOperationException($"Failed to download TOSEC payload after {MaxHttpAttempts} attempts.");
	}

	private async Task<string> DownloadStringWithRetryAsync(string url, CancellationToken cancellationToken) {
		var client = _httpClientFactory.CreateClient(nameof(TosecProvider));

		for (var attempt = 1; attempt <= MaxHttpAttempts; attempt++) {
			try {
				return await client.GetStringAsync(url, cancellationToken);
			} catch (Exception ex) when (attempt < MaxHttpAttempts && IsTransient(ex, cancellationToken)) {
				await Task.Delay(RetryDelay, cancellationToken);
			}
		}

		throw new InvalidOperationException($"Failed to download TOSEC index page after {MaxHttpAttempts} attempts.");
	}

	private static bool IsTransient(Exception ex, CancellationToken cancellationToken) {
		if (cancellationToken.IsCancellationRequested) {
			return false;
		}

		return ex is HttpRequestException || ex is TaskCanceledException;
	}

	private static string TryGetUriExtension(string url) {
		return Uri.TryCreate(url, UriKind.Absolute, out var uri)
			? Path.GetExtension(uri.AbsolutePath).ToLowerInvariant()
			: Path.GetExtension(url).ToLowerInvariant();
	}

	private static async Task<Stream> ExtractDatFromZipAsync(Stream archiveStream, string sourceName, CancellationToken cancellationToken) {
		await using var source = archiveStream;
		using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: false);

		var target = archive.Entries
			.Where(entry => !string.IsNullOrWhiteSpace(entry.Name))
			.OrderBy(entry => RankExtension(Path.GetExtension(entry.FullName).ToLowerInvariant()))
			.FirstOrDefault(entry => RankExtension(Path.GetExtension(entry.FullName).ToLowerInvariant()) < int.MaxValue);

		if (target is null) {
			throw new InvalidOperationException($"TOSEC archive '{sourceName}' does not contain a supported DAT payload entry.");
		}

		await using var entryStream = target.Open();
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
		var file = Path.GetFileNameWithoutExtension(path);
		var split = file.Split(" - ");
		return split.Length > 0 ? split[0] : "TOSEC";
	}
}
