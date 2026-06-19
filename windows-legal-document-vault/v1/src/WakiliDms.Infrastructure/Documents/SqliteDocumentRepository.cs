using Microsoft.Data.Sqlite;
using WakiliDms.Core.Documents;
using WakiliDms.Core.Domain;

namespace WakiliDms.Infrastructure.Documents;

public sealed class SqliteDocumentRepository : IDocumentRepository
{
    private readonly string _databasePath;

    public SqliteDocumentRepository(string databasePath)
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
            CREATE TABLE IF NOT EXISTS documents (
                id TEXT PRIMARY KEY,
                matter_id TEXT NOT NULL,
                original_file_name TEXT NOT NULL,
                extension TEXT NOT NULL,
                vault_object_id TEXT NOT NULL,
                sha256_hash TEXT NOT NULL,
                byte_length INTEGER NOT NULL,
                document_type INTEGER NOT NULL,
                status INTEGER NOT NULL,
                imported_at TEXT NOT NULL,
                FOREIGN KEY (matter_id) REFERENCES matters(id)
            );

            CREATE INDEX IF NOT EXISTS ix_documents_matter_id_imported_at
                ON documents (matter_id, imported_at DESC);
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddAsync(LegalDocument document, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO documents (
                id,
                matter_id,
                original_file_name,
                extension,
                vault_object_id,
                sha256_hash,
                byte_length,
                document_type,
                status,
                imported_at
            )
            VALUES (
                $id,
                $matter_id,
                $original_file_name,
                $extension,
                $vault_object_id,
                $sha256_hash,
                $byte_length,
                $document_type,
                $status,
                $imported_at
            );
            """;

        BindDocument(command, document);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<LegalDocument>> ListByMatterAsync(Guid matterId, CancellationToken cancellationToken)
    {
        var documents = new List<LegalDocument>();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                id,
                matter_id,
                original_file_name,
                extension,
                vault_object_id,
                sha256_hash,
                byte_length,
                document_type,
                status,
                imported_at
            FROM documents
            WHERE matter_id = $matter_id
            ORDER BY imported_at DESC, original_file_name ASC;
            """;
        command.Parameters.AddWithValue("$matter_id", matterId.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            documents.Add(ReadDocument(reader));
        }

        return documents;
    }

    public async Task<LegalDocument?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                id,
                matter_id,
                original_file_name,
                extension,
                vault_object_id,
                sha256_hash,
                byte_length,
                document_type,
                status,
                imported_at
            FROM documents
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDocument(reader) : null;
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Pooling = false
        }.ToString());
    }

    private static void BindDocument(SqliteCommand command, LegalDocument document)
    {
        command.Parameters.AddWithValue("$id", document.Id.ToString());
        command.Parameters.AddWithValue("$matter_id", document.MatterId.ToString());
        command.Parameters.AddWithValue("$original_file_name", document.OriginalFileName);
        command.Parameters.AddWithValue("$extension", document.Extension);
        command.Parameters.AddWithValue("$vault_object_id", document.VaultObjectId);
        command.Parameters.AddWithValue("$sha256_hash", document.Sha256Hash);
        command.Parameters.AddWithValue("$byte_length", document.ByteLength);
        command.Parameters.AddWithValue("$document_type", (int)document.DocumentType);
        command.Parameters.AddWithValue("$status", (int)document.Status);
        command.Parameters.AddWithValue("$imported_at", document.ImportedAt.ToString("O"));
    }

    private static LegalDocument ReadDocument(SqliteDataReader reader)
    {
        return LegalDocument.Rehydrate(
            Guid.Parse(reader.GetString(0)),
            Guid.Parse(reader.GetString(1)),
            reader.GetString(2),
            reader.GetString(3),
            reader.GetString(4),
            reader.GetString(5),
            reader.GetInt64(6),
            (DocumentType)reader.GetInt32(7),
            (DocumentStatus)reader.GetInt32(8),
            DateTimeOffset.Parse(reader.GetString(9)));
    }
}
