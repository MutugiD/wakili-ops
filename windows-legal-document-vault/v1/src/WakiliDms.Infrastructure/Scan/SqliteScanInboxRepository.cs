using Microsoft.Data.Sqlite;
using WakiliDms.Core.Domain;
using WakiliDms.Core.Scan;

namespace WakiliDms.Infrastructure.Scan;

public sealed class SqliteScanInboxRepository : IScanInboxRepository
{
    private readonly string _databasePath;

    public SqliteScanInboxRepository(string databasePath)
    {
        if (string.IsNullOrWhiteSpace(databasePath))
        {
            throw new ArgumentException("Database path is required.", nameof(databasePath));
        }

        _databasePath = databasePath;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_databasePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            CREATE TABLE IF NOT EXISTS scan_inbox (
                id TEXT PRIMARY KEY,
                source_path TEXT NOT NULL,
                original_file_name TEXT NOT NULL,
                extension TEXT NOT NULL,
                sha256_hash TEXT NOT NULL,
                byte_length INTEGER NOT NULL,
                status INTEGER NOT NULL,
                document_id TEXT NULL,
                detected_at TEXT NOT NULL,
                imported_at TEXT NULL
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_scan_inbox_source_hash
                ON scan_inbox (source_path, sha256_hash);

            CREATE INDEX IF NOT EXISTS ix_scan_inbox_status_detected_at
                ON scan_inbox (status, detected_at DESC);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddAsync(ScanInboxItem item, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO scan_inbox (
                id,
                source_path,
                original_file_name,
                extension,
                sha256_hash,
                byte_length,
                status,
                document_id,
                detected_at,
                imported_at
            )
            VALUES (
                $id,
                $source_path,
                $original_file_name,
                $extension,
                $sha256_hash,
                $byte_length,
                $status,
                $document_id,
                $detected_at,
                $imported_at
            );
            """;

        BindItem(command, item);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(string sourcePath, string sha256Hash, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT 1
            FROM scan_inbox
            WHERE source_path = $source_path
              AND sha256_hash = $sha256_hash
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("$source_path", Path.GetFullPath(sourcePath));
        command.Parameters.AddWithValue("$sha256_hash", sha256Hash.Trim().ToUpperInvariant());

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is not null;
    }

    public async Task<IReadOnlyList<ScanInboxItem>> ListPendingAsync(CancellationToken cancellationToken)
    {
        var items = new List<ScanInboxItem>();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                id,
                source_path,
                original_file_name,
                extension,
                sha256_hash,
                byte_length,
                status,
                document_id,
                detected_at,
                imported_at
            FROM scan_inbox
            WHERE status = $status
            ORDER BY detected_at DESC, original_file_name ASC;
            """;
        command.Parameters.AddWithValue("$status", (int)ScanInboxStatus.Pending);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(ReadItem(reader));
        }

        return items;
    }

    public async Task MarkImportedAsync(
        Guid scanInboxItemId,
        Guid documentId,
        DateTimeOffset importedAt,
        CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE scan_inbox
            SET
                status = $status,
                document_id = $document_id,
                imported_at = $imported_at
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$status", (int)ScanInboxStatus.Imported);
        command.Parameters.AddWithValue("$document_id", documentId.ToString());
        command.Parameters.AddWithValue("$imported_at", importedAt.ToString("O"));
        command.Parameters.AddWithValue("$id", scanInboxItemId.ToString());

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Pooling = false
        }.ToString());
    }

    private static void BindItem(SqliteCommand command, ScanInboxItem item)
    {
        command.Parameters.AddWithValue("$id", item.Id.ToString());
        command.Parameters.AddWithValue("$source_path", item.SourcePath);
        command.Parameters.AddWithValue("$original_file_name", item.OriginalFileName);
        command.Parameters.AddWithValue("$extension", item.Extension);
        command.Parameters.AddWithValue("$sha256_hash", item.Sha256Hash);
        command.Parameters.AddWithValue("$byte_length", item.ByteLength);
        command.Parameters.AddWithValue("$status", (int)item.Status);
        command.Parameters.AddWithValue("$document_id", item.DocumentId is null ? DBNull.Value : item.DocumentId.Value.ToString());
        command.Parameters.AddWithValue("$detected_at", item.DetectedAt.ToString("O"));
        command.Parameters.AddWithValue("$imported_at", item.ImportedAt is null ? DBNull.Value : item.ImportedAt.Value.ToString("O"));
    }

    private static ScanInboxItem ReadItem(SqliteDataReader reader)
    {
        return ScanInboxItem.Rehydrate(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetInt64(5),
            (ScanInboxStatus)reader.GetInt32(6),
            ReadNullableGuid(reader, 7),
            DateTimeOffset.Parse(reader.GetString(8)),
            ReadNullableDateTimeOffset(reader, 9));
    }

    private static Guid? ReadNullableGuid(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : Guid.Parse(reader.GetString(ordinal));
    }

    private static DateTimeOffset? ReadNullableDateTimeOffset(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : DateTimeOffset.Parse(reader.GetString(ordinal));
    }
}
