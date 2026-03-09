using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using SeedLists.Dat.Parsing;
using SeedLists.Dat.Services;
using System.Text;
using System.Text.Json;

namespace SeedLists.Dat.Tests;

public sealed class DatCollectionServiceManifestTests {
	[Fact]
	public async Task SyncProviderAsync_ParsesNormalizedPayloadForXmlLikeSources() {
		var outputDirectory = CreateTempDirectory();
		try {
			var provider = new XmlLikeProvider();
			var service = new DatCollectionService(
				[provider],
				new DatParserFactory([new StreamingJsonDatParser()]),
				new CatalogNormalizationService(),
				new CatalogValidationService(),
				Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
					OutputDirectory = outputDirectory,
				}));

			var report = await service.SyncProviderAsync(DatProviderKind.Mame, forceRefresh: false);

			Assert.Equal(1, report.DatsDiscovered);
			Assert.Equal(1, report.DatsProcessed);
			Assert.Equal(0, report.DatsFailed);

			var summaryPath = Path.Combine(outputDirectory, "mame", "sample-xml.summary.json");
			Assert.True(File.Exists(summaryPath));

			using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(summaryPath));
			Assert.Equal(1, document.RootElement.GetProperty("Games").GetArrayLength());
		} finally {
			DeleteTempDirectory(outputDirectory);
		}
	}

	[Fact]
	public async Task SyncProviderAsync_WritesManifestWithSourceStatuses() {
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
				}));

			var report = await service.SyncProviderAsync(DatProviderKind.Tosec, forceRefresh: false);

			Assert.Equal(2, report.DatsDiscovered);
			Assert.Equal(1, report.DatsProcessed);
			Assert.Equal(1, report.DatsFailed);
			Assert.NotNull(report.ManifestPath);
			Assert.True(File.Exists(report.ManifestPath));

			using var document = JsonDocument.Parse(await File.ReadAllBytesAsync(report.ManifestPath!));
			var root = document.RootElement;

			Assert.Equal("Tosec", root.GetProperty("provider").GetString());
			Assert.Equal(2, root.GetProperty("datsDiscovered").GetInt32());
			Assert.Equal(1, root.GetProperty("datsProcessed").GetInt32());
			Assert.Equal(1, root.GetProperty("datsFailed").GetInt32());
			Assert.Equal(2, root.GetProperty("sources").GetArrayLength());

			var statuses = root.GetProperty("sources")
				.EnumerateArray()
				.Select(source => source.GetProperty("status").GetString() ?? string.Empty)
				.ToArray();

			Assert.Contains("processed", statuses);
			Assert.Contains("failed", statuses);

			var latestPath = Path.Combine(Path.GetDirectoryName(report.ManifestPath!)!, "latest-sync-manifest.json");
			Assert.True(File.Exists(latestPath));
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
			// Best effort test cleanup.
		}
	}

	private sealed class TestProvider : IDatProvider {
		public DatProviderKind ProviderType => DatProviderKind.Tosec;

		public Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			return Task.FromResult<IReadOnlyList<DatMetadata>>([
				new DatMetadata {
					Identifier = "ok",
					Name = "ok-source",
					Description = "ok source",
					System = "TOSEC",
				},
				new DatMetadata {
					Identifier = "fail",
					Name = "fail-source",
					Description = "failing source",
					System = "TOSEC",
				}
			]);
		}

		public Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
			_ = cancellationToken;

			if (identifier == "fail") {
				throw new InvalidOperationException("simulated provider failure");
			}

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
			return identifier is "ok" or "fail";
		}
	}

	private sealed class XmlLikeProvider : IDatProvider {
		public DatProviderKind ProviderType => DatProviderKind.Mame;

		public Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			return Task.FromResult<IReadOnlyList<DatMetadata>>([
				new DatMetadata {
					Identifier = "xml-ok",
					Name = "sample-xml",
					Description = "xml source",
					System = "MAME",
				}
			]);
		}

		public Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			_ = identifier;

			var xml = """
				<datafile>
					<game name="sample-game">
						<description>Sample Game</description>
						<rom name="sample.bin" size="16" crc="ABCDEF12"/>
					</game>
				</datafile>
				""";

			return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(xml), writable: false));
		}

		public bool SupportsIdentifier(string identifier) {
			return identifier == "xml-ok";
		}
	}
}
