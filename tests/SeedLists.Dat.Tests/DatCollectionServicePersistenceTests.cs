using Microsoft.Data.Sqlite;
using SeedLists.Dat.Abstractions;
using SeedLists.Dat.Models;
using SeedLists.Dat.Options;
using SeedLists.Dat.Parsing;
using SeedLists.Dat.Services;
using System.Text;

namespace SeedLists.Dat.Tests;

public sealed class DatCollectionServicePersistenceTests {
	[Fact]
	public async Task SyncProviderAsync_PersistsSqliteLedgerAndNormalizedCatalog() {
		var outputDirectory = CreateTempDirectory();
		try {
			var databasePath = Path.Combine(outputDirectory, "ingestion", "ingestion-ledger.sqlite");
			var service = new DatCollectionService(
				[new PersistenceProvider()],
				new DatParserFactory([new StreamingJsonDatParser()]),
				new CatalogNormalizationService(),
				new CatalogValidationService(),
				Microsoft.Extensions.Options.Options.Create(new SeedListsDatOptions {
					OutputDirectory = outputDirectory,
					IngestionDatabasePath = "ingestion/ingestion-ledger.sqlite",
				}));

			var report = await service.SyncProviderAsync(DatProviderKind.PleasureDome, forceRefresh: false);

			Assert.Equal(1, report.DatsDiscovered);
			Assert.Equal(1, report.DatsProcessed);
			Assert.Equal(0, report.DatsFailed);
			Assert.True(File.Exists(databasePath));

			await using var connection = new SqliteConnection($"Data Source={databasePath}");
			await connection.OpenAsync();

			var ingestionCommand = connection.CreateCommand();
			ingestionCommand.CommandText = """
				SELECT
					provider,
					source_url,
					source_name,
					system_name,
					saved_source_path,
					saved_source_file_name,
					saved_source_size,
					saved_normalized_path,
					saved_summary_path,
					crc32,
					md5,
					sha1,
					sha256
				FROM ingestion_records
				LIMIT 1;
				""";

			await using var reader = await ingestionCommand.ExecuteReaderAsync();
			Assert.True(await reader.ReadAsync());

			Assert.Equal("PleasureDome", reader.GetString(0));
			Assert.Equal("https://example.invalid/pleasuredome/FruitMachines-20251022.zip", reader.GetString(1));
			Assert.Equal("FruitMachines-20251022", reader.GetString(2));
			Assert.Equal("Fruit Machines", reader.GetString(3));

			var sourcePath = reader.GetString(4);
			var sourceFileName = reader.GetString(5);
			var sourceSize = reader.GetInt64(6);
			var normalizedPath = reader.GetString(7);
			var summaryPath = reader.GetString(8);
			var crc32 = reader.GetString(9);
			var md5 = reader.GetString(10);
			var sha1 = reader.GetString(11);
			var sha256 = reader.GetString(12);

			Assert.True(File.Exists(sourcePath));
			Assert.True(File.Exists(normalizedPath));
			Assert.True(File.Exists(summaryPath));
			Assert.True(sourceSize > 0);
			Assert.Contains(Path.Combine("pleasuredome", "ingested-sources", "Fruit Machines"), sourcePath, StringComparison.OrdinalIgnoreCase);
			Assert.StartsWith("FruitMachines-20251022", sourceFileName, StringComparison.OrdinalIgnoreCase);
			Assert.Equal(8, crc32.Length);
			Assert.Equal(32, md5.Length);
			Assert.Equal(40, sha1.Length);
			Assert.Equal(64, sha256.Length);

			var normalizedCommand = connection.CreateCommand();
			normalizedCommand.CommandText = "SELECT normalized_json, normalized_bytes FROM normalized_catalogs LIMIT 1;";
			await using var normalizedReader = await normalizedCommand.ExecuteReaderAsync();
			Assert.True(await normalizedReader.ReadAsync());
			var normalizedJson = normalizedReader.GetString(0);
			var normalizedBytes = normalizedReader.GetInt64(1);

			Assert.Contains("\"provider\": \"PleasureDome\"", normalizedJson, StringComparison.Ordinal);
			Assert.True(normalizedBytes > 0);
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
			// Best effort cleanup.
		}
	}

	private sealed class PersistenceProvider : IDatProvider {
		public DatProviderKind ProviderType => DatProviderKind.PleasureDome;

		public Task<IReadOnlyList<DatMetadata>> ListAvailableAsync(CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			return Task.FromResult<IReadOnlyList<DatMetadata>>([
				new DatMetadata {
					Identifier = "persist-source",
					Name = "FruitMachines-20251022",
					Description = "Pleasuredome fruit machines",
					System = "Fruit Machines",
					DownloadUrl = "https://example.invalid/pleasuredome/FruitMachines-20251022.zip",
					LastUpdated = DateTimeOffset.UtcNow,
					FileSize = 1234,
				}
			]);
		}

		public Task<Stream> DownloadDatAsync(string identifier, CancellationToken cancellationToken = default) {
			_ = cancellationToken;
			if (identifier != "persist-source") {
				throw new InvalidOperationException("Unexpected identifier.");
			}

			var json = """
				{
					"name": "Pleasuredome Fruit Machines",
					"provider": "PleasureDome",
					"games": []
				}
				""";

			return Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(json), writable: false));
		}

		public bool SupportsIdentifier(string identifier) {
			return identifier == "persist-source";
		}
	}
}
