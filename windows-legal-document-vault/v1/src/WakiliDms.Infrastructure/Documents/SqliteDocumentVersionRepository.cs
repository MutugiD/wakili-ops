using Microsoft.Data.Sqlite;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Domain;

namespace WakiliDms.Infrastructure.Documents;

public sealed class SqliteDocumentVersionRepository : IDocumentVersionRepository
{
    private readonly string _databasePath;

    public SqliteDocumentVersionRepository(string databasePath)
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
            CREATE TABLE IF NOT EXISTS document_versions (
                id TEXT PRIMARY KEY,
                document_id TEXT NOT NULL,
                version_number INTEGER NOT NULL,
                vault_object_id TEXT NOT NULL,
                sha256_hash TEXT NOT NULL,
                byte_length INTEGER NOT NULL,
                original_file_name TEXT NOT NULL,
                status INTEGER NOT NULL,
                created_at TEXT NOT NULL,
                notes TEXT NULL,
                FOREIGN KEY (document_id) REFERENCES documents(id)
            );

            CREATE UNIQUE INDEX IF NOT EXISTS ux_document_versions_document_number
                ON document_versions (document_id, version_number);

            CREATE INDEX IF NOT EXISTS ix_document_versions_document_id_created_at
                ON document_versions (document_id, created_at DESC);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddAsync(DocumentVersion version, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO document_versions (
                id,
                document_id,
                version_number,
                vault_object_id,
                sha256_hash,
                byte_length,
                original_file_name,
                status,
                created_at,
                notes
            )
            VALUES (
                $id,
                $document_id,
                $version_number,
                $vault_object_id,
                $sha256_hash,
                $byte_length,
                $original_file_name,
                $status,
                $created_at,
                $notes
            );
            """;

        BindVersion(command, version);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentVersion>> ListByDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var versions = new List<DocumentVersion>();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                id,
                document_id,
                version_number,
                vault_object_id,
                sha256_hash,
                byte_length,
                original_file_name,
                status,
                created_at,
                notes
            FROM document_versions
            WHERE document_id = $document_id
            ORDER BY version_number DESC;
            """;
        command.Parameters.AddWithValue("$document_id", documentId.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            versions.Add(ReadVersion(reader));
        }

        return versions;
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Pooling = false
        }.ToString());
    }

    private static void BindVersion(SqliteCommand command, DocumentVersion version)
    {
        command.Parameters.AddWithValue("$id", version.Id.ToString());
        command.Parameters.AddWithValue("$document_id", version.DocumentId.ToString());
        command.Parameters.AddWithValue("$version_number", version.VersionNumber);
        command.Parameters.AddWithValue("$vault_object_id", version.VaultObjectId);
        command.Parameters.AddWithValue("$sha256_hash", version.Sha256Hash);
        command.Parameters.AddWithValue("$byte_length", version.ByteLength);
        command.Parameters.AddWithValue("$original_file_name", version.OriginalFileName);
        command.Parameters.AddWithValue("$status", (int)version.Status);
        command.Parameters.AddWithValue("$created_at", version.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$notes", version.Notes is null ? DBNull.Value : version.Notes);
    }

    private static DocumentVersion ReadVersion(SqliteDataReader reader)
    {
        return DocumentVersion.Rehydrate(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetInt32(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetInt64(5),
            reader.GetString(6),
            (DocumentStatus)reader.GetInt32(7),
            DateTimeOffset.Parse(reader.GetString(8)),
            ReadNullableString(reader, 9));
    }

    private static string? ReadNullableString(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
