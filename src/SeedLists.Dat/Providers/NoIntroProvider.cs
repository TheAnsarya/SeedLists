using System.Text.RegularExpressions;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using Microsoft.Extensions.Options;

namespace SeedLists.Dat.Providers;

/// <summary>
/// No-Intro provider with a mandatory 24-hour remote download cooldown.
/// </summary>
public sealed class NoIntroProvider(
	IOptions<SeedListsDatOptions> options,
	IDatSyncStateStore stateStore,
	IHttpClientFactory httpClientFactory) : IDatProvider {
	private const string LastDownloadStateKey = "no-intro:last-download-utc";
	private static readonly TimeSpan Cooldown = TimeSpan.FromHours(24);
	private readonly SeedListsDatOptions _options = options.Value;
	private readonly IDatSyncStateStore _stateStore = stateStore;
	private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

	public DatProviderKind ProviderType => DatProviderKind.NoIntro;

	public async Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
		var list = new List<DatMetadata>();
		list.AddRange(GetLocalDats());

		if (_options.EnableInternetDownloads) {
			var remote = await GetRemoteSystemsAsync(cancellationToken);
			list.AddRange(remote);
		}

		return list;
	}

	public async Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(identifier);

		if (identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase)) {
			var path = identifier["local::".Length..];
			if (!File.Exists(path)) {
				throw new FileNotFoundException("No-Intro local DAT source not found.", path);
			}

			return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		}

		if (!identifier.StartsWith("system::", StringComparison.OrdinalIgnoreCase)) {
			throw new NotSupportedException("No-Intro identifier must be local:: or system::.");
		}

		if (!_options.EnableInternetDownloads) {
			throw new InvalidOperationException("Internet downloads are disabled for No-Intro.");
		}

		await EnforceCooldownAsync(cancellationToken);

		var systemId = identifier["system::".Length..];
		var url = $"{_options.NoIntroBaseUrl}/index.php?page=download&op=dat&s={systemId}";
		var client = _httpClientFactory.CreateClient(nameof(NoIntroProvider));
		var bytes = await client.GetByteArrayAsync(url, cancellationToken);
		await _stateStore.SetDateTimeAsync(LastDownloadStateKey, DateTimeOffset.UtcNow, cancellationToken);

		return new MemoryStream(bytes, writable: false);
	}

	public bool SupportsIdentifier(string identifier) {
		return identifier.StartsWith("local::", StringComparison.OrdinalIgnoreCase)
			|| identifier.StartsWith("system::", StringComparison.OrdinalIgnoreCase);
	}

	private async Task EnforceCooldownAsync(CancellationToken cancellationToken) {
		if (_options.AllowNoIntroDownloadDuringTesting) {
			return;
		}

		var last = await _stateStore.GetDateTimeAsync(LastDownloadStateKey, cancellationToken);
		if (last is null) {
			return;
		}

		var elapsed = DateTimeOffset.UtcNow - last.Value;
		if (elapsed < Cooldown) {
			var wait = Cooldown - elapsed;
			throw new InvalidOperationException($"No-Intro cooldown active. Next remote download allowed in {wait:c}.");
		}
	}

	private IEnumerable<DatMetadata> GetLocalDats() {
		if (!Directory.Exists(_options.NoIntroLocalDirectory)) {
			yield break;
		}

		foreach (var path in Directory.EnumerateFiles(_options.NoIntroLocalDirectory, "*.dat", SearchOption.AllDirectories)) {
			var info = new FileInfo(path);
			yield return new DatMetadata {
				Identifier = $"local::{path}",
				Name = Path.GetFileNameWithoutExtension(path),
				Description = "No-Intro local DAT source",
				System = "No-Intro",
				FileSize = info.Length,
				LastUpdated = info.LastWriteTimeUtc,
			};
		}
	}

	private async Task<IReadOnlyList<DatMetadata>> GetRemoteSystemsAsync(CancellationToken cancellationToken) {
		var client = _httpClientFactory.CreateClient(nameof(NoIntroProvider));
		var html = await client.GetStringAsync(_options.NoIntroDownloadPageUrl, cancellationToken);

		var output = new List<DatMetadata>();
		var pattern = "href=\"[^\"]*\\?page=download&op=dat&s=(?<id>\\d+)\"[^>]*>(?<name>[^<]+)</a>";
		var matches = Regex.Matches(html, pattern, RegexOptions.IgnoreCase);

		foreach (Match match in matches) {
			var id = match.Groups["id"].Value;
			var name = match.Groups["name"].Value.Trim();
			output.Add(new DatMetadata {
				Identifier = $"system::{id}",
				Name = name,
				Description = "No-Intro remote DAT",
				System = name,
				DownloadUrl = $"{_options.NoIntroBaseUrl}/index.php?page=download&op=dat&s={id}",
			});
		}

		return output;
	}
}
