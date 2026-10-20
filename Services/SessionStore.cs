using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Data.Sqlite;
using SprintFocus.Models;

namespace SprintFocus.Services;

/// <summary>
/// Acesso ao banco SQLite onde as sessões de foco ficam guardadas.
/// Datas são gravadas como milissegundos Unix (UTC) em colunas INTEGER: comparar e somar fica trivial.
/// Para ler no terminal: SELECT datetime(started_at/1000, 'unixepoch', 'localtime') FROM sessions;
/// </summary>
public class SessionStore
{
    private readonly string _connectionString;

    public SessionStore(string? databasePath = null)
    {
        DatabasePath = databasePath ?? DefaultDatabasePath();
        Directory.CreateDirectory(Path.GetDirectoryName(DatabasePath)!);
        _connectionString = new SqliteConnectionStringBuilder { DataSource = DatabasePath }.ToString();
        EnsureCreated();
    }

    public string DatabasePath { get; }

    /// <summary>~/.local/share/SprintFocus/sprintfocus.db no Linux (%LOCALAPPDATA% no Windows).</summary>
    public static string DefaultDatabasePath() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SprintFocus",
        "sprintfocus.db");

    /// <summary>Insere a sessão e devolve o Id gerado pelo banco.</summary>
    public long Insert(FocusSession session)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO sessions (category, started_at, ended_at)
            VALUES ($category, $startedAt, $endedAt)
            RETURNING id;
            """;
        command.Parameters.AddWithValue("$category", session.Category.ToString());
        command.Parameters.AddWithValue("$startedAt", ToUnixMs(session.StartedAt));
        command.Parameters.AddWithValue("$endedAt", ToUnixMs(session.EndedAt));
        return (long)command.ExecuteScalar()!;
    }

    /// <summary>Atualiza o fim de uma sessão já gravada (ela cresceu desde o último save).</summary>
    public void UpdateEnd(long id, DateTime endedAt)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE sessions SET ended_at = $endedAt WHERE id = $id;";
        command.Parameters.AddWithValue("$endedAt", ToUnixMs(endedAt));
        command.Parameters.AddWithValue("$id", id);
        command.ExecuteNonQuery();
    }

    /// <summary>Soma a duração das sessões que começaram a partir de <paramref name="sinceUtc"/>, por categoria.</summary>
    public Dictionary<ActivityCategory, TimeSpan> GetTotalsSince(DateTime sinceUtc)
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT category, SUM(ended_at - started_at)
            FROM sessions
            WHERE started_at >= $since
            GROUP BY category;
            """;
        command.Parameters.AddWithValue("$since", ToUnixMs(sinceUtc));

        var totals = new Dictionary<ActivityCategory, TimeSpan>();
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            // Ignora categorias desconhecidas (ex.: renomeadas numa versão futura) em vez de quebrar.
            if (Enum.TryParse<ActivityCategory>(reader.GetString(0), out var category))
                totals[category] = TimeSpan.FromMilliseconds(reader.GetInt64(1));
        }
        return totals;
    }

    private void EnsureCreated()
    {
        using var connection = Open();
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS sessions (
                id          INTEGER PRIMARY KEY AUTOINCREMENT,
                category    TEXT    NOT NULL,
                started_at  INTEGER NOT NULL,
                ended_at    INTEGER NOT NULL
            );
            CREATE INDEX IF NOT EXISTS ix_sessions_started_at ON sessions (started_at);
            """;
        command.ExecuteNonQuery();
    }

    private SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        return connection;
    }

    private static long ToUnixMs(DateTime utc) => new DateTimeOffset(utc, TimeSpan.Zero).ToUnixTimeMilliseconds();
}
