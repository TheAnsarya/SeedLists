# Ingestion Database

Issues: `#38`, `#39`, `#40`, `#41`, `#43`, `#44`, `#45`, `#46`, `#47`

SeedLists records successful DAT ingestions and failed ingestion attempts into SQLite while still writing normalized JSON and summary files to disk.

## Database Location

Configured by:

- `SeedListsDat:IngestionDatabasePath`

Behavior:

- If path is relative, it is resolved under `SeedListsDat:OutputDirectory`.
- If path is absolute, it is used as-is.

Default:

- `ingestion/ingestion-ledger.sqlite`

## What Is Persisted

For each successfully processed source:

- ingestion timestamp (UTC)
- source provider and system
- source identifier, URL, file name, version metadata
- source file metadata (saved path/name, created/modified timestamps, size)
- basic file hashes (`crc32`, `md5`, `sha1`, `sha256`)
- normalized JSON catalog payload (in SQLite)
- normalized JSON path and summary path (on disk)

For each failed source attempt:

- failure timestamp (UTC)
- source provider and system
- source identifier, URL, file name, version metadata
- failure stage classification (`download`, `validation`, `parsing`, `persistence`, `sync`)
- error message

## Source File Hierarchy

Ingested source payload files are persisted under provider output roots:

- `{OutputDirectory}/{provider}/ingested-sources/{system}/{yyyy}/{MM}/{dd}/`

This allows deterministic traceability and replay for audited ingestions.

## SQLite Schema

### `ingestion_records`

- `id` (PK)
- `ingested_at_utc`
- `provider`
- `system_name`
- `source_identifier`
- `source_url`
- `source_name`
- `source_version`
- `source_last_updated_utc`
- `source_reported_size`
- `saved_source_path`
- `saved_source_file_name`
- `saved_source_size`
- `saved_source_created_utc`
- `saved_source_modified_utc`
- `saved_normalized_path`
- `saved_summary_path`
- `crc32`
- `md5`
- `sha1`
- `sha256`

### `normalized_catalogs`

- `id` (PK)
- `ingestion_record_id` (FK -> `ingestion_records.id`)
- `normalized_json`
- `normalized_bytes`
- `created_at_utc`

### `ingestion_failures`

- `id` (PK)
- `failed_at_utc`
- `provider`
- `system_name`
- `source_identifier`
- `source_url`
- `source_name`
- `source_version`
- `source_last_updated_utc`
- `source_reported_size`
- `stage`
- `error_message`

## Example Queries

```sql
SELECT id, ingested_at_utc, provider, system_name, source_name, saved_source_size
FROM ingestion_records
ORDER BY id DESC
LIMIT 20;
```

```sql
SELECT r.provider, r.system_name, r.source_name, c.normalized_bytes
FROM ingestion_records r
JOIN normalized_catalogs c ON c.ingestion_record_id = r.id
ORDER BY r.id DESC
LIMIT 20;
```

```sql
SELECT provider, COUNT(*) AS ingestions
FROM ingestion_records
GROUP BY provider
ORDER BY ingestions DESC;
```

```sql
SELECT provider, stage, source_name, error_message, failed_at_utc
FROM ingestion_failures
ORDER BY id DESC
LIMIT 20;
```

```sql
SELECT provider, stage, COUNT(*) AS failures
FROM ingestion_failures
GROUP BY provider, stage
ORDER BY failures DESC;
```

## Validation Command

```powershell
& "C:\Program Files\dotnet\dotnet.exe" test tests/SeedLists.Dat.Tests/SeedLists.Dat.Tests.csproj -c Release --filter "DatCollectionServicePersistenceTests"
```
