using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using SeedLists.Dat.Parsing;
using SeedLists.Dat.Services;
using System.Text;
using System.Text.Json;

namespace SeedLists.Dat.Tests;

public sealed class DatCollectionServiceRunControlsTests {
	[Fact]
	public async Task SyncProviderAsync_AppliesIncludeExcludeAndMaxControls() {
		var outputDirectory = CreateTempDirectory();
		try {
			var provider = new TestProvider();
			var service = new DatCollectionService(
				[provider],
				new DatParserFactory([new StreamingJsonDatParser()]),
				new CatalogNormalizationService(),
				new CatalogValidationService(),
				Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
					OutputDirectory = outputDirectory,
					IncludeNamePatterns = ["SNES*", "NES*"],
					ExcludeNamePatterns = ["*Beta*"],
					MaxDatsPerRun = 1,
				}));

			var report = await service.SyncProviderAsync(DatProviderKind.Tosec, forceRefresh: false);

			Assert.Equal(1, report.DatsDiscovered);
			Assert.Equal(1, report.DatsProcessed);
			Assert.Equal(0, report.DatsFailed);
			Assert.Equal(["id-1"], provider.DownloadedIdentifiers);

			using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(report.ManifestPath!));
			Assert.Equal(1, document.RootElement.GetProperty("sources").GetArrayLength());
		} finally {
			DeleteTempDirectory(outputDirectory);
		}
	}

	[Fact]
	public async Task SyncProviderAsync_AppliesMaxWithoutPatterns() {
		var outputDirectory = CreateTempDirectory();
		try {
			var provider = new TestProvider();
			var service = new DatCollectionService(
				[provider],
				new DatParserFactory([new StreamingJsonDatParser()]),
				new CatalogNormalizationService(),
				new CatalogValidationService(),
				Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
					OutputDirectory = outputDirectory,
					MaxDatsPerRun = 2,
				}));

			var report = await service.SyncProviderAsync(DatProviderKind.Tosec, forceRefresh: false);

			Assert.Equal(2, report.DatsDiscovered);
			Assert.Equal(2, report.DatsProcessed);
			Assert.Equal(0, report.DatsFailed);
			Assert.Equal(["id-1", "id-2"], provider.DownloadedIdentifiers);
		} finally {
			DeleteTempDirectory(outputDirectory);
		}
	}

	private static string CreateTempDirectory() {
		var path = Path.Combine(Path.GetTempPath(), "SeedLists.Tests", Guid.NewGuid().ToString("N"));
		Directory.CreateDirectory(path);
		return path;
	}

	private static void DeleteTempDirectory(string path) {
		if (!Directory.Exists(path)) {
			return;
		}

		try {
			Directory.Delete(path, recursive: true);
		} catch {
			// Best effort temp cleanup.
		}
	}

	private sealed class TestProvider : IDatProvider {
		private readonly IReadOnlyList<DatMetadata> _metadata = [
			new DatMetadata {
				Identifier = "id-1",
				Name = "SNES Alpha",
			},
			new DatMetadata {
				Identifier = "id-2",
				Name = "SNES Beta",
			},
			new DatMetadata {
				Identifier = "id-3",
				Name = "NES Gamma",
			},
			new DatMetadata {
				Identifier = "id-4",
				Name = "GB Delta",
			}
		];

		public DatProviderKind ProviderType => DatProviderKind.Tosec;
		public List<string> DownloadedIdentifiers { get; } = [];

		public Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			return Task.FromResult<IReadOnlyList<DatMetadata>>(_metadata);
		}

		public Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			DownloadedIdentifiers.Add(identifier);

			var json = """
				{
					"name": "test",
					"provider": "Tosec",
					"games": []
				}
				""";

			return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(json), writable: false));
		}

		public bool SupportsIdentifier(string identifier) {
			return _metadata.Any(item => item.Identifier == identifier);
		}
	}
}
