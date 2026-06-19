using System.Text.RegularExpressions;
using Microsoft.Data.Sqlite;
using WakiliDms.Core.Search;

namespace WakiliDms.Infrastructure.Search;

public sealed partial class SqliteDocumentSearchRepository : IDocumentSearchRepository
{
    private readonly string _databasePath;

    public SqliteDocumentSearchRepository(string databasePath)
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
            CREATE TABLE IF NOT EXISTS document_text_index (
                document_id TEXT PRIMARY KEY,
                matter_id TEXT NOT NULL,
                original_file_name TEXT NOT NULL,
                text_content TEXT NOT NULL,
                indexed_at TEXT NOT NULL,
                FOREIGN KEY (document_id) REFERENCES documents(id)
            );

            CREATE VIRTUAL TABLE IF NOT EXISTS document_search_fts
            USING fts5(
                document_id UNINDEXED,
                matter_id UNINDEXED,
                original_file_name,
                text_content
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertAsync(DocumentTextIndexEntry entry, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = connection.BeginTransaction();

        await ExecuteAsync(
            connection,
            transaction,
            """
            DELETE FROM document_text_index
            WHERE document_id = $document_id;
            """,
            cancellationToken,
            ("$document_id", entry.DocumentId.ToString()));

        await ExecuteAsync(
            connection,
            transaction,
            """
            DELETE FROM document_search_fts
            WHERE document_id = $document_id;
            """,
            cancellationToken,
            ("$document_id", entry.DocumentId.ToString()));

        await ExecuteAsync(
            connection,
            transaction,
            """
            INSERT INTO document_text_index (
                document_id,
                matter_id,
                original_file_name,
                text_content,
                indexed_at
            )
            VALUES (
                $document_id,
                $matter_id,
                $original_file_name,
                $text_content,
                $indexed_at
            );
            """,
            cancellationToken,
            ("$document_id", entry.DocumentId.ToString()),
            ("$matter_id", entry.MatterId.ToString()),
            ("$original_file_name", entry.OriginalFileName),
            ("$text_content", entry.TextContent),
            ("$indexed_at", entry.IndexedAt.ToString("O")));

        await ExecuteAsync(
            connection,
            transaction,
            """
            INSERT INTO document_search_fts (
                document_id,
                matter_id,
                original_file_name,
                text_content
            )
            VALUES (
                $document_id,
                $matter_id,
                $original_file_name,
                $text_content
            );
            """,
            cancellationToken,
            ("$document_id", entry.DocumentId.ToString()),
            ("$matter_id", entry.MatterId.ToString()),
            ("$original_file_name", entry.OriginalFileName),
            ("$text_content", entry.TextContent));

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<DocumentSearchResult>> SearchAsync(
        Guid matterId,
        string query,
        CancellationToken cancellationToken)
    {
        var ftsQuery = BuildFtsQuery(query);
        if (string.IsNullOrWhiteSpace(ftsQuery))
        {
            return [];
        }

        var results = new List<DocumentSearchResult>();
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                document_id,
                matter_id,
                original_file_name,
                snippet(document_search_fts, 3, '[', ']', '...', 12) AS snippet,
                bm25(document_search_fts) AS rank
            FROM document_search_fts
            WHERE matter_id = $matter_id
              AND document_search_fts MATCH $query
            ORDER BY rank ASC
            LIMIT 25;
            """;
        command.Parameters.AddWithValue("$matter_id", matterId.ToString());
        command.Parameters.AddWithValue("$query", ftsQuery);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new DocumentSearchResult(
                Guid.Parse(reader.GetString(0)),
                Guid.Parse(reader.GetString(1)),
                reader.GetString(2),
                reader.GetString(3),
                reader.GetDouble(4)));
        }

        return results;
    }

    private SqliteConnection CreateConnection()
    {
        return new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Pooling = false
        }.ToString());
    }

    private static async Task ExecuteAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string commandText,
        CancellationToken cancellationToken,
        params (string Name, string Value)[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = commandText;
        foreach (var (name, value) in parameters)
        {
            command.Parameters.AddWithValue(name, value);
        }

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static string BuildFtsQuery(string query)
    {
        var terms = SearchTermRegex()
            .Matches(query)
            .Select(match => match.Value.Trim())
            .Where(term => term.Length >= 2)
            .Take(8)
            .Select(term => $"{term}*");

        return string.Join(' ', terms);
    }

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex SearchTermRegex();
}
