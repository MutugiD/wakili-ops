using Microsoft.Data.Sqlite;
using WakiliDms.Core.Domain;
using WakiliDms.Core.Matter;

namespace WakiliDms.Infrastructure.Matter;

public sealed class SqliteMatterRepository : IMatterRepository
{
    private readonly string _databasePath;

    public SqliteMatterRepository(string databasePath)
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
            CREATE TABLE IF NOT EXISTS matters (
                id TEXT PRIMARY KEY,
                name TEXT NOT NULL,
                internal_reference TEXT NULL,
                court_case_number TEXT NULL,
                court TEXT NULL,
                court_station TEXT NULL,
                division TEXT NULL,
                practice_area TEXT NULL,
                client_name TEXT NULL,
                responsible_advocate TEXT NULL,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL
            );
            """;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task AddAsync(WakiliDms.Core.Domain.Matter matter, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            INSERT INTO matters (
                id,
                name,
                internal_reference,
                court_case_number,
                court,
                court_station,
                division,
                practice_area,
                client_name,
                responsible_advocate,
                created_at,
                updated_at
            )
            VALUES (
                $id,
                $name,
                $internal_reference,
                $court_case_number,
                $court,
                $court_station,
                $division,
                $practice_area,
                $client_name,
                $responsible_advocate,
                $created_at,
                $updated_at
            );
            """;

        BindMatter(command, matter);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<WakiliDms.Core.Domain.Matter>> ListAsync(CancellationToken cancellationToken)
    {
        var matters = new List<WakiliDms.Core.Domain.Matter>();

        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                id,
                name,
                internal_reference,
                court_case_number,
                court,
                court_station,
                division,
                practice_area,
                client_name,
                responsible_advocate,
                created_at,
                updated_at
            FROM matters
            ORDER BY updated_at DESC, name ASC;
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            matters.Add(ReadMatter(reader));
        }

        return matters;
    }

    public async Task<WakiliDms.Core.Domain.Matter?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT
                id,
                name,
                internal_reference,
                court_case_number,
                court,
                court_station,
                division,
                practice_area,
                client_name,
                responsible_advocate,
                created_at,
                updated_at
            FROM matters
            WHERE id = $id;
            """;
        command.Parameters.AddWithValue("$id", id.ToString());

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadMatter(reader) : null;
    }

    public async Task UpdateAsync(WakiliDms.Core.Domain.Matter matter, CancellationToken cancellationToken)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            """
            UPDATE matters
            SET
                name = $name,
                internal_reference = $internal_reference,
                court_case_number = $court_case_number,
                court = $court,
                court_station = $court_station,
                division = $division,
                practice_area = $practice_area,
                client_name = $client_name,
                responsible_advocate = $responsible_advocate,
                created_at = $created_at,
                updated_at = $updated_at
            WHERE id = $id;
            """;

        BindMatter(command, matter);
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

    private static void BindMatter(SqliteCommand command, WakiliDms.Core.Domain.Matter matter)
    {
        command.Parameters.AddWithValue("$id", matter.Id.ToString());
        command.Parameters.AddWithValue("$name", matter.Name);
        command.Parameters.AddWithValue("$internal_reference", DbValue(matter.InternalReference));
        command.Parameters.AddWithValue("$court_case_number", DbValue(matter.CourtCaseNumber));
        command.Parameters.AddWithValue("$court", DbValue(matter.Court));
        command.Parameters.AddWithValue("$court_station", DbValue(matter.CourtStation));
        command.Parameters.AddWithValue("$division", DbValue(matter.Division));
        command.Parameters.AddWithValue("$practice_area", DbValue(matter.PracticeArea));
        command.Parameters.AddWithValue("$client_name", DbValue(matter.ClientName));
        command.Parameters.AddWithValue("$responsible_advocate", DbValue(matter.ResponsibleAdvocate));
        command.Parameters.AddWithValue("$created_at", matter.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("$updated_at", matter.UpdatedAt.ToString("O"));
    }

    private static WakiliDms.Core.Domain.Matter ReadMatter(SqliteDataReader reader)
    {
        return WakiliDms.Core.Domain.Matter.Rehydrate(
            Guid.Parse(reader.GetString(0)),
            reader.GetString(1),
            ReadNullableString(reader, 2),
            ReadNullableString(reader, 3),
            ReadNullableString(reader, 4),
            ReadNullableString(reader, 5),
            ReadNullableString(reader, 6),
            ReadNullableString(reader, 7),
            ReadNullableString(reader, 8),
            ReadNullableString(reader, 9),
            DateTimeOffset.Parse(reader.GetString(10)),
            DateTimeOffset.Parse(reader.GetString(11)));
    }

    private static object DbValue(string? value)
    {
        return value is null ? DBNull.Value : value;
    }

    private static string? ReadNullableString(SqliteDataReader reader, int ordinal)
    {
        return reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);
    }
}
