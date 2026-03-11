using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SeedLists.Dat.Services;

/// <summary>
/// Coordinates provider enumeration, download, parsing, and DAT output persistence.
/// </summary>
public sealed class DatCollectionService(
	IEnumerable<IDatProvider> providers,
	IDatParserFactory parserFactory,
	ICatalogNormalizationService normalizationService,
	ICatalogValidationService validationService,
	IOptions<SeedListsDatOptions> options) : IDatCollectionService {
	private static readonly HashSet<char> InvalidFileNameChars = [.. Path.GetInvalidFileNameChars()];
	private static readonly uint[] Crc32Table = BuildCrc32Table();

	private static readonly JsonSerializerOptions JsonOptions = new() {
		WriteIndented = true,
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		Converters = { new JsonStringEnumConverter() },
	};

	private static readonly JsonSerializerOptions SummaryJsonOptions = new() {
		WriteIndented = true,
	};

	private readonly IReadOnlyList<IDatProvider> _providers = providers.ToList();
	private readonly IDatParserFactory _parserFactory = parserFactory;
	private readonly ICatalogNormalizationService _normalizationService = normalizationService;
	private readonly ICatalogValidationService _validationService = validationService;
	private readonly SeedListsDatOptions _options = options.Value;
	private readonly DatIngestionLedgerStore _ingestionLedgerStore = new();

	public async Task<DatSyncReport> SyncProviderAsync(
		DatProviderKind provider,
		bool forceRefresh,
		IProgress<DatSyncProgress>? progress = null,
		CancellationToken cancellationToken = default) {
		_ = forceRefresh;

		var providerInstance = _providers.FirstOrDefault(p => p.ProviderType == provider)
			?? throw new InvalidOperationException($"Provider '{provider}' is not registered.");

		Directory.CreateDirectory(_options.OutputDirectory);
		var providerOutputDir = Path.Combine(_options.OutputDirectory, provider.ToString().ToLowerInvariant());
		Directory.CreateDirectory(providerOutputDir);

		var started = DateTimeOffset.UtcNow;
		var discovered = ApplyRunControls(await providerInstance.ListAvailableAsync(cancellationToken));
		var sourceStatuses = discovered.Select(metadata => new DatSyncManifestSource {
			Identifier = metadata.Identifier,
			Name = metadata.Name,
			Description = metadata.Description,
			Version = metadata.Version,
			System = metadata.System,
			DownloadUrl = metadata.DownloadUrl,
			FileSize = metadata.FileSize,
			LastUpdated = metadata.LastUpdated,
			Status = "pending",
		}).ToList();

		var errors = new List<string>();
		var processed = 0;
		var failed = 0;

		progress?.Report(new DatSyncProgress {
			Provider = provider,
			Phase = DatSyncPhase.Discovering,
			ProcessedCount = 0,
			TotalCount = discovered.Count,
		});

		for (var i = 0; i < discovered.Count; i++) {
			cancellationToken.ThrowIfCancellationRequested();

			var metadata = discovered[i];
			try {
				progress?.Report(new DatSyncProgress {
					Provider = provider,
					Phase = DatSyncPhase.Downloading,
					CurrentDat = metadata.Name,
					ProcessedCount = i,
					TotalCount = discovered.Count,
				});

				await using var source = await providerInstance.DownloadDatAsync(metadata.Identifier, cancellationToken);
				await using var buffer = new MemoryStream();
				await source.CopyToAsync(buffer, cancellationToken);
				buffer.Position = 0;
				if (!buffer.TryGetBuffer(out var sourceSegment)) {
					throw new InvalidOperationException("Unable to read DAT payload buffer.");
				}

				var sourcePayload = new ReadOnlyMemory<byte>(sourceSegment.Array!, sourceSegment.Offset, (int)buffer.Length);
				var sourceArtifact = await SaveSourceArtifactAsync(providerOutputDir, metadata, sourcePayload, cancellationToken);
				var hashes = ComputeSourceHashes(sourcePayload.Span);

				var payloadBytes = _normalizationService.Normalize(
					sourcePayload.Span,
					provider,
					metadata.Name);

				var validation = _validationService.Validate(payloadBytes);
				if (!validation.IsValid) {
					throw new InvalidOperationException($"Catalog validation failed: {string.Join(" | ", validation.Errors)}");
				}

				var rawName = SafeFileName(metadata.Name);
				var normalizedPath = Path.Combine(providerOutputDir, $"{rawName}.dat");
				await File.WriteAllBytesAsync(normalizedPath, payloadBytes, cancellationToken);

				progress?.Report(new DatSyncProgress {
					Provider = provider,
					Phase = DatSyncPhase.Parsing,
					CurrentDat = metadata.Name,
					ProcessedCount = i,
					TotalCount = discovered.Count,
				});

				await using var normalizedPayloadStream = new MemoryStream(payloadBytes, writable: false);
				var parser = _parserFactory.GetParser(normalizedPayloadStream);
				if (parser is null) {
					throw new InvalidOperationException($"No parser available for '{metadata.Name}'.");
				}

				normalizedPayloadStream.Position = 0;
				var parsed = await parser.ParseAsync(normalizedPayloadStream, Path.GetFileName(normalizedPath), cancellationToken: cancellationToken);
				var summaryPath = Path.Combine(providerOutputDir, $"{rawName}.summary.json");
				var summaryJson = JsonSerializer.Serialize(parsed, SummaryJsonOptions);
				await File.WriteAllTextAsync(summaryPath, summaryJson, cancellationToken);

				await _ingestionLedgerStore.WriteAsync(
					ResolveIngestionDatabasePath(),
					new DatIngestionLedgerEntry {
						IngestedAtUtc = DateTimeOffset.UtcNow,
						Provider = provider.ToString(),
						System = metadata.System,
						SourceIdentifier = metadata.Identifier,
						SourceUrl = metadata.DownloadUrl,
						SourceName = metadata.Name,
						SourceVersion = metadata.Version,
						SourceLastUpdatedUtc = metadata.LastUpdated,
						SourceReportedSize = metadata.FileSize,
						SavedSourcePath = sourceArtifact.FullPath,
						SavedSourceFileName = sourceArtifact.FileName,
						SavedSourceSize = sourceArtifact.Size,
						SavedSourceCreatedUtc = sourceArtifact.CreatedUtc,
						SavedSourceModifiedUtc = sourceArtifact.ModifiedUtc,
						SavedNormalizedPath = normalizedPath,
						SavedSummaryPath = summaryPath,
						Crc32 = hashes.Crc32,
						Md5 = hashes.Md5,
						Sha1 = hashes.Sha1,
						Sha256 = hashes.Sha256,
						NormalizedCatalogUtf8 = payloadBytes,
					},
					cancellationToken);

				processed++;
				sourceStatuses[i] = sourceStatuses[i] with { Status = "processed", Error = null };

				progress?.Report(new DatSyncProgress {
					Provider = provider,
					Phase = DatSyncPhase.Saving,
					CurrentDat = metadata.Name,
					ProcessedCount = i + 1,
					TotalCount = discovered.Count,
				});
			} catch (Exception ex) {
				failed++;
				errors.Add($"{metadata.Name}: {ex.Message}");
				sourceStatuses[i] = sourceStatuses[i] with { Status = "failed", Error = ex.Message };
			}
		}

		var completed = DateTimeOffset.UtcNow;
		var manifestPath = await WriteManifestAsync(
			providerOutputDir,
			provider,
			started,
			completed,
			processed,
			failed,
			errors,
			sourceStatuses,
			cancellationToken);

		progress?.Report(new DatSyncProgress {
			Provider = provider,
			Phase = DatSyncPhase.Completed,
			ProcessedCount = discovered.Count,
			TotalCount = discovered.Count,
		});

		return new DatSyncReport {
			Provider = provider,
			StartedAtUtc = started,
			CompletedAtUtc = completed,
			DatsDiscovered = discovered.Count,
			DatsProcessed = processed,
			DatsFailed = failed,
			Errors = errors,
			ManifestPath = manifestPath,
		};
	}

	public Task<IReadOnlyList<DatProviderKind>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default) {
		_ = cancellationToken;
		return Task.FromResult<IReadOnlyList<DatProviderKind>>(_providers.Select(p => p.ProviderType).Distinct().ToList());
	}

	private static string SafeFileName(string name) {
		if (string.IsNullOrEmpty(name)) {
			return name;
		}

		var chars = name.ToCharArray();
		var changed = false;
		for (var i = 0; i < chars.Length; i++) {
			if (!InvalidFileNameChars.Contains(chars[i])) {
				continue;
			}

			chars[i] = '_';
			changed = true;
		}

		return changed ? new string(chars) : name;
	}

	private async Task<SavedSourceArtifact> SaveSourceArtifactAsync(
		string providerOutputDirectory,
		DatMetadata metadata,
		ReadOnlyMemory<byte> payload,
		CancellationToken cancellationToken) {
		var capturedAt = DateTimeOffset.UtcNow;
		var systemSegment = SafeFileName(string.IsNullOrWhiteSpace(metadata.System) ? "unknown" : metadata.System);
		if (string.IsNullOrWhiteSpace(systemSegment)) {
			systemSegment = "unknown";
		}

		var hierarchyDirectory = Path.Combine(
			providerOutputDirectory,
			"ingested-sources",
			systemSegment,
			capturedAt.ToString("yyyy"),
			capturedAt.ToString("MM"),
			capturedAt.ToString("dd"));

		Directory.CreateDirectory(hierarchyDirectory);

		var baseName = SafeFileName(metadata.Name);
		if (string.IsNullOrWhiteSpace(baseName)) {
			baseName = "source";
		}

		var extension = ResolveSourceExtension(metadata);
		var sourceFileName = $"{baseName}-{capturedAt:HHmmssfff}{extension}";
		var sourcePath = Path.Combine(hierarchyDirectory, sourceFileName);

		await using (var destination = new FileStream(sourcePath, FileMode.Create, FileAccess.Write, FileShare.Read)) {
			await destination.WriteAsync(payload, cancellationToken);
		}

		var info = new FileInfo(sourcePath);
		return new SavedSourceArtifact(
			sourcePath,
			sourceFileName,
			info.Length,
			info.CreationTimeUtc,
			info.LastWriteTimeUtc);
	}

	private string ResolveIngestionDatabasePath() {
		if (string.IsNullOrWhiteSpace(_options.IngestionDatabasePath)) {
			return Path.Combine(_options.OutputDirectory, "ingestion", "ingestion-ledger.sqlite");
		}

		if (Path.IsPathRooted(_options.IngestionDatabasePath)) {
			return _options.IngestionDatabasePath;
		}

		return Path.Combine(_options.OutputDirectory, _options.IngestionDatabasePath);
	}

	private static string ResolveSourceExtension(DatMetadata metadata) {
		if (!string.IsNullOrWhiteSpace(metadata.DownloadUrl) && Uri.TryCreate(metadata.DownloadUrl, UriKind.Absolute, out var uri)) {
			var fromUrl = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
			if (!string.IsNullOrWhiteSpace(fromUrl)) {
				return fromUrl;
			}
		}

		var fromName = Path.GetExtension(metadata.Name).ToLowerInvariant();
		if (!string.IsNullOrWhiteSpace(fromName)) {
			return fromName;
		}

		return ".dat";
	}

	private static SourceHashes ComputeSourceHashes(ReadOnlySpan<byte> payload) {
		return new SourceHashes(
			ComputeCrc32Hex(payload),
			Convert.ToHexString(MD5.HashData(payload)).ToLowerInvariant(),
			Convert.ToHexString(SHA1.HashData(payload)).ToLowerInvariant(),
			Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant());
	}

	private static string ComputeCrc32Hex(ReadOnlySpan<byte> payload) {
		uint crc = 0xffffffff;
		foreach (var b in payload) {
			var index = (int)((crc ^ b) & 0xff);
			crc = (crc >> 8) ^ Crc32Table[index];
		}

		crc ^= 0xffffffff;
		return crc.ToString("x8");
	}

	private static uint[] BuildCrc32Table() {
		const uint polynomial = 0xedb88320u;
		var table = new uint[256];

		for (var i = 0; i < table.Length; i++) {
			uint value = (uint)i;
			for (var bit = 0; bit < 8; bit++) {
				value = (value & 1) == 1 ? (value >> 1) ^ polynomial : value >> 1;
			}

			table[i] = value;
		}

		return table;
	}

	private sealed record SavedSourceArtifact(
		string FullPath,
		string FileName,
		long Size,
		DateTimeOffset CreatedUtc,
		DateTimeOffset ModifiedUtc);

	private sealed record SourceHashes(string Crc32, string Md5, string Sha1, string Sha256);

	private IReadOnlyList<DatMetadata> ApplyRunControls(IReadOnlyList<DatMetadata> discovered) {
		IEnumerable<DatMetadata> query = discovered;

		if (_options.IncludeNamePatterns.Length > 0) {
			query = query.Where(metadata => MatchesAnyPattern(metadata.Name, _options.IncludeNamePatterns));
		}

		if (_options.ExcludeNamePatterns.Length > 0) {
			query = query.Where(metadata => !MatchesAnyPattern(metadata.Name, _options.ExcludeNamePatterns));
		}

		if (_options.MaxDatsPerRun > 0) {
			query = query.Take(_options.MaxDatsPerRun);
		}

		return query.ToList();
	}

	private static bool MatchesAnyPattern(string sourceName, IReadOnlyList<string> patterns) {
		foreach (var pattern in patterns) {
			if (string.IsNullOrWhiteSpace(pattern)) {
				continue;
			}

			if (WildcardMatch(sourceName, pattern)) {
				return true;
			}
		}

		return false;
	}

	private static bool WildcardMatch(string input, string pattern) {
		ReadOnlySpan<char> source = input.AsSpan();
		ReadOnlySpan<char> wildcard = pattern.Trim().AsSpan();

		var sourceIndex = 0;
		var wildcardIndex = 0;
		var starIndex = -1;
		var sourceBacktrackIndex = 0;

		while (sourceIndex < source.Length) {
			if (wildcardIndex < wildcard.Length &&
				(wildcard[wildcardIndex] == '?' ||
				char.ToUpperInvariant(wildcard[wildcardIndex]) == char.ToUpperInvariant(source[sourceIndex]))) {
				sourceIndex++;
				wildcardIndex++;
				continue;
			}

			if (wildcardIndex < wildcard.Length && wildcard[wildcardIndex] == '*') {
				starIndex = wildcardIndex++;
				sourceBacktrackIndex = sourceIndex;
				continue;
			}

			if (starIndex == -1) {
				return false;
			}

			wildcardIndex = starIndex + 1;
			sourceIndex = ++sourceBacktrackIndex;
		}

		while (wildcardIndex < wildcard.Length && wildcard[wildcardIndex] == '*') {
			wildcardIndex++;
		}

		return wildcardIndex == wildcard.Length;
	}

	private static async Task<string> WriteManifestAsync(
		string providerOutputDirectory,
		DatProviderKind provider,
		DateTimeOffset started,
		DateTimeOffset completed,
		int processed,
		int failed,
		IReadOnlyList<string> errors,
		IReadOnlyList<DatSyncManifestSource> sources,
		CancellationToken cancellationToken) {
		var manifestDirectory = Path.Combine(providerOutputDirectory, "run-manifests");
		Directory.CreateDirectory(manifestDirectory);

		var runId = started.ToString("yyyyMMdd-HHmmss-fff");
		var manifest = new DatSyncManifest {
			RunId = runId,
			Provider = provider,
			StartedAtUtc = started,
			CompletedAtUtc = completed,
			ElapsedMilliseconds = Math.Max(0, (long)(completed - started).TotalMilliseconds),
			DatsDiscovered = sources.Count,
			DatsProcessed = processed,
			DatsFailed = failed,
			Errors = [.. errors],
			Sources = [.. sources],
		};

		var manifestPath = Path.Combine(manifestDirectory, $"{runId}-{provider.ToString().ToLowerInvariant()}-sync-manifest.json");
		var latestPath = Path.Combine(manifestDirectory, "latest-sync-manifest.json");

		var json = JsonSerializer.Serialize(manifest, JsonOptions);
		await File.WriteAllTextAsync(manifestPath, json, cancellationToken);
		await File.WriteAllTextAsync(latestPath, json, cancellationToken);

		return manifestPath;
	}
}
