using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using Microsoft.Extensions.Options;

namespace SeedLists.Dat.Services;

/// <summary>
/// Coordinates provider enumeration, download, parsing, and DAT output persistence.
/// </summary>
public sealed class DatCollectionService(
	IEnumerable<IDatProvider> providers,
	IDatParserFactory parserFactory,
	IOptions<SeedListsDatOptions> options) : IDatCollectionService {
	private readonly IReadOnlyList<IDatProvider> _providers = providers.ToList();
	private readonly IDatParserFactory _parserFactory = parserFactory;
	private readonly SeedListsDatOptions _options = options.Value;

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
		var discovered = await providerInstance.ListAvailableAsync(cancellationToken);
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

				var rawName = SafeFileName(metadata.Name);
				var rawPath = Path.Combine(providerOutputDir, $"{rawName}.dat");
				await File.WriteAllBytesAsync(rawPath, buffer.ToArray(), cancellationToken);

				progress?.Report(new DatSyncProgress {
					Provider = provider,
					Phase = DatSyncPhase.Parsing,
					CurrentDat = metadata.Name,
					ProcessedCount = i,
					TotalCount = discovered.Count,
				});

				buffer.Position = 0;
				var parser = _parserFactory.GetParser(buffer);
				if (parser is null) {
					throw new InvalidOperationException($"No parser available for '{metadata.Name}'.");
				}

				buffer.Position = 0;
				var parsed = await parser.ParseAsync(buffer, Path.GetFileName(rawPath), cancellationToken: cancellationToken);
				var summaryPath = Path.Combine(providerOutputDir, $"{rawName}.summary.json");
				var summaryJson = System.Text.Json.JsonSerializer.Serialize(parsed, new System.Text.Json.JsonSerializerOptions { WriteIndented = true });
				await File.WriteAllTextAsync(summaryPath, summaryJson, cancellationToken);

				processed++;

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
			}
		}

		progress?.Report(new DatSyncProgress {
			Provider = provider,
			Phase = DatSyncPhase.Completed,
			ProcessedCount = discovered.Count,
			TotalCount = discovered.Count,
		});

		return new DatSyncReport {
			Provider = provider,
			StartedAtUtc = started,
			CompletedAtUtc = DateTimeOffset.UtcNow,
			DatsDiscovered = discovered.Count,
			DatsProcessed = processed,
			DatsFailed = failed,
			Errors = errors,
		};
	}

	public Task<IReadOnlyList<DatProviderKind>> GetAvailableProvidersAsync(CancellationToken cancellationToken = default) {
		_ = cancellationToken;
		return Task.FromResult<IReadOnlyList<DatProviderKind>>(_providers.Select(p => p.ProviderType).Distinct().ToList());
	}

	private static string SafeFileName(string name) {
		var invalid = Path.GetInvalidFileNameChars();
		var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
		return new string(chars);
	}
}
