using System.Globalization;
using System.Data.Common;
using System.Text;
using Microsoft.Data.Sqlite;

namespace SeedLists.Dat.Services;

/// <summary>
/// Persists DAT ingestion audit rows and normalized catalog payloads into SQLite.
/// </summary>
public sealed class DatIngestionLedgerStore {
	private static readonly HashSet<string> InitializedDatabases = new(StringComparer.OrdinalIgnoreCase);
	private static readonly object SchemaLock = new();

	public async Task WriteAsync(string databasePath, DatIngestionLedgerEntry entry, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
		ArgumentNullException.ThrowIfNull(entry);

		var parent = Path.GetDirectoryName(databasePath);
		if (!string.IsNullOrWhiteSpace(parent)) {
			Directory.CreateDirectory(parent);
		}

		var connectionBuilder = new SqliteConnectionStringBuilder {
			DataSource = databasePath,
			Mode = SqliteOpenMode.ReadWriteCreate,
		};

		await using var connection = new SqliteConnection(connectionBuilder.ToString());
		await connection.OpenAsync(cancellationToken);

		await EnablePragmasAsync(connection, cancellationToken);
		await EnsureSchemaAsync(connection, databasePath, cancellationToken);

		await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

		var ingestionId = await InsertIngestionRecordAsync(connection, transaction, entry, cancellationToken);
		await InsertNormalizedCatalogAsync(connection, transaction, ingestionId, entry, cancellationToken);

		await transaction.CommitAsync(cancellationToken);
	}

	public async Task WriteFailureAsync(string databasePath, DatIngestionFailureEntry entry, CancellationToken cancellationToken = default) {
		ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
		ArgumentNullException.ThrowIfNull(entry);

		var parent = Path.GetDirectoryName(databasePath);
		if (!string.IsNullOrWhiteSpace(parent)) {
			Directory.CreateDirectory(parent);
		}

		var connectionBuilder = new SqliteConnectionStringBuilder {
			DataSource = databasePath,
			Mode = SqliteOpenMode.ReadWriteCreate,
		};

		await using var connection = new SqliteConnection(connectionBuilder.ToString());
		await connection.OpenAsync(cancellationToken);

		await EnablePragmasAsync(connection, cancellationToken);
		await EnsureSchemaAsync(connection, databasePath, cancellationToken);

		var command = connection.CreateCommand();
		command.CommandText = """
			INSERT INTO ingestion_failures (
				failed_at_utc,
				provider,
				system_name,
				source_identifier,
				source_url,
				source_name,
				source_version,
				source_last_updated_utc,
				source_reported_size,
				stage,
				error_message)
			VALUES (
				$failedAtUtc,
				$provider,
				$systemName,
				$sourceIdentifier,
				$sourceUrl,
				$sourceName,
				$sourceVersion,
				$sourceLastUpdatedUtc,
				$sourceReportedSize,
				$stage,
				$errorMessage);
			""";

		command.Parameters.AddWithValue("$failedAtUtc", entry.FailedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
		command.Parameters.AddWithValue("$provider", entry.Provider);
		command.Parameters.AddWithValue("$systemName", entry.System ?? string.Empty);
		command.Parameters.AddWithValue("$sourceIdentifier", entry.SourceIdentifier);
		command.Parameters.AddWithValue("$sourceUrl", entry.SourceUrl ?? string.Empty);
		command.Parameters.AddWithValue("$sourceName", entry.SourceName);
		command.Parameters.AddWithValue("$sourceVersion", entry.SourceVersion ?? string.Empty);
		command.Parameters.AddWithValue("$sourceLastUpdatedUtc", entry.SourceLastUpdatedUtc?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty);
		command.Parameters.AddWithValue("$sourceReportedSize", entry.SourceReportedSize ?? 0L);
		command.Parameters.AddWithValue("$stage", entry.Stage);
		command.Parameters.AddWithValue("$errorMessage", entry.ErrorMessage);

		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	private static async Task EnablePragmasAsync(SqliteConnection connection, CancellationToken cancellationToken) {
		var command = connection.CreateCommand();
		command.CommandText = "PRAGMA foreign_keys = ON; PRAGMA journal_mode = WAL; PRAGMA synchronous = NORMAL;";
		await command.ExecuteNonQueryAsync(cancellationToken);
	}

	private static async Task EnsureSchemaAsync(SqliteConnection connection, string databasePath, CancellationToken cancellationToken) {
		var normalizedPath = Path.GetFullPath(databasePath);
		lock (SchemaLock) {
			if (InitializedDatabases.Contains(normalizedPath)) {
				return;
			}
		}

		var command = connection.CreateCommand();
		command.CommandText = """
			CREATE TABLE IF NOT EXISTS ingestion_records (
				id INTEGER PRIMARY KEY AUTOINCREMENT,
				ingested_at_utc TEXT NOT NULL,
				provider TEXT NOT NULL,
				system_name TEXT,
				source_identifier TEXT NOT NULL,
				source_url TEXT,
				source_name TEXT NOT NULL,
				source_version TEXT,
				source_last_updated_utc TEXT,
				source_reported_size INTEGER,
				saved_source_path TEXT NOT NULL,
				saved_source_file_name TEXT NOT NULL,
				saved_source_size INTEGER NOT NULL,
				saved_source_created_utc TEXT NOT NULL,
				saved_source_modified_utc TEXT NOT NULL,
				saved_normalized_path TEXT NOT NULL,
				saved_summary_path TEXT NOT NULL,
				crc32 TEXT NOT NULL,
				md5 TEXT NOT NULL,
				sha1 TEXT NOT NULL,
				sha256 TEXT NOT NULL
			);

			CREATE TABLE IF NOT EXISTS normalized_catalogs (
				id INTEGER PRIMARY KEY AUTOINCREMENT,
				ingestion_record_id INTEGER NOT NULL,
				normalized_json TEXT NOT NULL,
				normalized_bytes INTEGER NOT NULL,
				created_at_utc TEXT NOT NULL,
				FOREIGN KEY (ingestion_record_id) REFERENCES ingestion_records(id) ON DELETE CASCADE
			);

			CREATE INDEX IF NOT EXISTS idx_ingestion_records_provider_time ON ingestion_records(provider, ingested_at_utc);
			CREATE INDEX IF NOT EXISTS idx_normalized_catalogs_ingestion_id ON normalized_catalogs(ingestion_record_id);

			CREATE TABLE IF NOT EXISTS ingestion_failures (
				id INTEGER PRIMARY KEY AUTOINCREMENT,
				failed_at_utc TEXT NOT NULL,
				provider TEXT NOT NULL,
				system_name TEXT,
				source_identifier TEXT NOT NULL,
				source_url TEXT,
				source_name TEXT NOT NULL,
				source_version TEXT,
				source_last_updated_utc TEXT,
				source_reported_size INTEGER,
				stage TEXT NOT NULL,
				error_message TEXT NOT NULL
			);

			CREATE INDEX IF NOT EXISTS idx_ingestion_failures_provider_time ON ingestion_failures(provider, failed_at_utc);
			""";

		await command.ExecuteNonQueryAsync(cancellationToken);

		lock (SchemaLock) {
			InitializedDatabases.Add(normalizedPath);
		}
	}

	private static async Task<long> InsertIngestionRecordAsync(
		SqliteConnection connection,
		DbTransaction transaction,
		DatIngestionLedgerEntry entry,
		CancellationToken cancellationToken) {
		var command = connection.CreateCommand();
		command.Transaction = (SqliteTransaction)transaction;
		command.CommandText = """
			INSERT INTO ingestion_records (
				ingested_at_utc,
				provider,
				system_name,
				source_identifier,
				source_url,
				source_name,
				source_version,
				source_last_updated_utc,
				source_reported_size,
				saved_source_path,
				saved_source_file_name,
				saved_source_size,
				saved_source_created_utc,
				saved_source_modified_utc,
				saved_normalized_path,
				saved_summary_path,
				crc32,
				md5,
				sha1,
				sha256)
			VALUES (
				$ingestedAtUtc,
				$provider,
				$systemName,
				$sourceIdentifier,
				$sourceUrl,
				$sourceName,
				$sourceVersion,
				$sourceLastUpdatedUtc,
				$sourceReportedSize,
				$savedSourcePath,
				$savedSourceFileName,
				$savedSourceSize,
				$savedSourceCreatedUtc,
				$savedSourceModifiedUtc,
				$savedNormalizedPath,
				$savedSummaryPath,
				$crc32,
				$md5,
				$sha1,
				$sha256);
			SELECT last_insert_rowid();
			""";

		command.Parameters.AddWithValue("$ingestedAtUtc", entry.IngestedAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
		command.Parameters.AddWithValue("$provider", entry.Provider);
		command.Parameters.AddWithValue("$systemName", entry.System ?? string.Empty);
		command.Parameters.AddWithValue("$sourceIdentifier", entry.SourceIdentifier);
		command.Parameters.AddWithValue("$sourceUrl", entry.SourceUrl ?? string.Empty);
		command.Parameters.AddWithValue("$sourceName", entry.SourceName);
		command.Parameters.AddWithValue("$sourceVersion", entry.SourceVersion ?? string.Empty);
		command.Parameters.AddWithValue("$sourceLastUpdatedUtc", entry.SourceLastUpdatedUtc?.UtcDateTime.ToString("O", CultureInfo.InvariantCulture) ?? string.Empty);
		command.Parameters.AddWithValue("$sourceReportedSize", entry.SourceReportedSize ?? 0L);
		command.Parameters.AddWithValue("$savedSourcePath", entry.SavedSourcePath);
		command.Parameters.AddWithValue("$savedSourceFileName", entry.SavedSourceFileName);
		command.Parameters.AddWithValue("$savedSourceSize", entry.SavedSourceSize);
		command.Parameters.AddWithValue("$savedSourceCreatedUtc", entry.SavedSourceCreatedUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
		command.Parameters.AddWithValue("$savedSourceModifiedUtc", entry.SavedSourceModifiedUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));
		command.Parameters.AddWithValue("$savedNormalizedPath", entry.SavedNormalizedPath);
		command.Parameters.AddWithValue("$savedSummaryPath", entry.SavedSummaryPath);
		command.Parameters.AddWithValue("$crc32", entry.Crc32);
		command.Parameters.AddWithValue("$md5", entry.Md5);
		command.Parameters.AddWithValue("$sha1", entry.Sha1);
		command.Parameters.AddWithValue("$sha256", entry.Sha256);

		var result = await command.ExecuteScalarAsync(cancellationToken);
		if (result is null) {
			throw new InvalidOperationException("Failed to insert ingestion record row.");
		}

		return Convert.ToInt64(result, CultureInfo.InvariantCulture);
	}

	private static async Task InsertNormalizedCatalogAsync(
		SqliteConnection connection,
		DbTransaction transaction,
		long ingestionId,
		DatIngestionLedgerEntry entry,
		CancellationToken cancellationToken) {
		var command = connection.CreateCommand();
		command.Transaction = (SqliteTransaction)transaction;
		command.CommandText = """
			INSERT INTO normalized_catalogs (
				ingestion_record_id,
				normalized_json,
				normalized_bytes,
				created_at_utc)
			VALUES (
				$ingestionRecordId,
				$normalizedJson,
				$normalizedBytes,
				$createdAtUtc);
			""";

		command.Parameters.AddWithValue("$ingestionRecordId", ingestionId);
		command.Parameters.AddWithValue("$normalizedJson", Encoding.UTF8.GetString(entry.NormalizedCatalogUtf8));
		command.Parameters.AddWithValue("$normalizedBytes", entry.NormalizedCatalogUtf8.Length);
		command.Parameters.AddWithValue("$createdAtUtc", DateTimeOffset.UtcNow.UtcDateTime.ToString("O", CultureInfo.InvariantCulture));

		await command.ExecuteNonQueryAsync(cancellationToken);
	}
}

/// <summary>
/// Database write model for a single successful DAT ingestion.
/// </summary>
public sealed record DatIngestionLedgerEntry {
	public required DateTimeOffset IngestedAtUtc { get; init; }
	public required string Provider { get; init; }
	public required string SourceIdentifier { get; init; }
	public required string SourceName { get; init; }
	public required string SavedSourcePath { get; init; }
	public required string SavedSourceFileName { get; init; }
	public required long SavedSourceSize { get; init; }
	public required DateTimeOffset SavedSourceCreatedUtc { get; init; }
	public required DateTimeOffset SavedSourceModifiedUtc { get; init; }
	public required string SavedNormalizedPath { get; init; }
	public required string SavedSummaryPath { get; init; }
	public required string Crc32 { get; init; }
	public required string Md5 { get; init; }
	public required string Sha1 { get; init; }
	public required string Sha256 { get; init; }
	public required byte[] NormalizedCatalogUtf8 { get; init; }
	public string? System { get; init; }
	public string? SourceUrl { get; init; }
	public string? SourceVersion { get; init; }
	public DateTimeOffset? SourceLastUpdatedUtc { get; init; }
	public long? SourceReportedSize { get; init; }
}

/// <summary>
/// Database write model for a failed DAT ingestion attempt.
/// </summary>
public sealed record DatIngestionFailureEntry {
	public required DateTimeOffset FailedAtUtc { get; init; }
	public required string Provider { get; init; }
	public required string SourceIdentifier { get; init; }
	public required string SourceName { get; init; }
	public required string Stage { get; init; }
	public required string ErrorMessage { get; init; }
	public string? System { get; init; }
	public string? SourceUrl { get; init; }
	public string? SourceVersion { get; init; }
	public DateTimeOffset? SourceLastUpdatedUtc { get; init; }
	public long? SourceReportedSize { get; init; }
}
