using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using OD.Planner.Models;

namespace OD.Planner.Data;

public sealed class AppDatabase
{
    private readonly string _connectionString;
    private const int CurrentSchemaVersion = 2;

    private static readonly string[] DateFormats =
    [
        "yyyy-MM-dd",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-dd HH:mm:ss.fff",
    ];

    private static readonly string[] DateTimeFormats =
    [
        "yyyy-MM-dd HH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss",
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-dd HH:mm:ss.fff",
    ];

    public AppDatabase(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = dbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Private,
            ForeignKeys = true,
        }.ToString();
    }

    private SqliteConnection Open() => new(_connectionString);

    public void EnsureCreated()
    {
        using var conn = Open();
        conn.Open();
        Execute(conn, """
            CREATE TABLE IF NOT EXISTS schema_version (
                Version INTEGER NOT NULL
            );
            """);
        Execute(conn, """
            CREATE TABLE IF NOT EXISTS categories (
                Id   INTEGER PRIMARY KEY AUTOINCREMENT,
                Name TEXT NOT NULL UNIQUE
            );
            """);
        Execute(conn, """
            CREATE TABLE IF NOT EXISTS tasks (
                Id            INTEGER PRIMARY KEY AUTOINCREMENT,
                Title         TEXT    NOT NULL,
                Comment       TEXT    NULL,
                CategoryId    INTEGER NULL REFERENCES categories(Id) ON DELETE SET NULL,
                Priority      INTEGER NOT NULL DEFAULT 1,
                DeadlineType  INTEGER NOT NULL DEFAULT 0,
                DeadlineDays  INTEGER NULL,
                DeadlineDate  TEXT    NULL,
                CreatedAt     TEXT    NOT NULL,
                IsCompleted   INTEGER NOT NULL DEFAULT 0,
                CompletedAt   TEXT    NULL
            );
            """);

        var version = GetSchemaVersion(conn);
        if (version < CurrentSchemaVersion)
        {
            Migrate(conn, version, CurrentSchemaVersion);
            SetSchemaVersion(conn, CurrentSchemaVersion);
        }

        using var check = conn.CreateCommand();
        check.CommandText = "SELECT COUNT(*) FROM categories";
        if (Convert.ToInt64(check.ExecuteScalar()) == 0)
        {
            foreach (var name in new[] { "Travail", "Personnel" })
            {
                using var insert = conn.CreateCommand();
                insert.CommandText = "INSERT INTO categories (Name) VALUES ($name)";
                insert.Parameters.AddWithValue("$name", name);
                insert.ExecuteNonQuery();
            }
        }
    }

    private static int GetSchemaVersion(SqliteConnection conn)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name='schema_version'";
        if (cmd.ExecuteScalar() is null)
        {
            return 0;
        }

        cmd.CommandText = "SELECT Version FROM schema_version LIMIT 1";
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    private static void SetSchemaVersion(SqliteConnection conn, int version)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM schema_version";
        cmd.ExecuteNonQuery();

        cmd.CommandText = "INSERT INTO schema_version (Version) VALUES ($version)";
        cmd.Parameters.AddWithValue("$version", version);
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Migrates the database schema from one version to another.
    /// Each migration is idempotent and can be safely re-run.
    /// </summary>
    private static void Migrate(SqliteConnection conn, int fromVersion, int toVersion)
    {
        for (var v = fromVersion; v < toVersion; v++)
        {
            switch (v)
            {
                case 1:
                    if (!HasColumn(conn, "tasks", "Comment"))
                    {
                        Execute(conn, "ALTER TABLE tasks ADD COLUMN Comment TEXT NULL");
                    }
                    break;
                default:
                    break;
            }
        }
    }

    private static void Execute(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    /// <summary>
    /// Determines whether a table contains a column.
    /// </summary>
    private static bool HasColumn(SqliteConnection conn, string tableName, string columnName)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    // ----- Categories -----

    public List<Category> GetCategories()
    {
        var result = new List<Category>();
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Name FROM categories ORDER BY Name COLLATE NOCASE";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new Category { Id = reader.GetInt64(0), Name = reader.GetString(1) });
        }

        return result;
    }

    public long AddCategory(string name)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "INSERT INTO categories (Name) VALUES ($name); SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("$name", name.Trim());
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public void RenameCategory(long id, string name)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE categories SET Name = $name WHERE Id = $id";
        cmd.Parameters.AddWithValue("$name", name.Trim());
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    public void DeleteCategory(long id)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM categories WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }

    // ----- Tasks -----

    public List<PlannerTask> GetTasks()
    {
        var result = new List<PlannerTask>();
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT Id, Title, Comment, CategoryId, Priority, DeadlineType, DeadlineDays, DeadlineDate, CreatedAt, IsCompleted, CompletedAt FROM tasks";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new PlannerTask
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                Comment = reader.IsDBNull(2) ? null : reader.GetString(2),
                CategoryId = reader.IsDBNull(3) ? null : reader.GetInt64(3),
                Priority = (Priority)reader.GetInt32(4),
                DeadlineType = (DeadlineType)reader.GetInt32(5),
                DeadlineDays = reader.IsDBNull(6) ? null : reader.GetInt32(6),
                DeadlineDate = reader.IsDBNull(7) ? null : ParseDate(reader.GetString(7)),
                CreatedAt = ParseDateTime(reader.GetString(8)),
                IsCompleted = reader.GetInt64(9) != 0,
                CompletedAt = reader.IsDBNull(10)
                    ? null
                    : ParseDateTime(reader.GetString(10)),
            });
        }

        return result;
    }

    private static DateTime ParseDate(string value)
    {
        return DateTime.TryParseExact(value, DateFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.Parse(value, CultureInfo.InvariantCulture);
    }

    private static DateTime ParseDateTime(string value)
    {
        return DateTime.TryParseExact(value, DateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt)
            ? dt
            : DateTime.Parse(value, CultureInfo.InvariantCulture);
    }

    public long InsertTask(PlannerTask task)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO tasks (Title, Comment, CategoryId, Priority, DeadlineType, DeadlineDays, DeadlineDate, CreatedAt, IsCompleted, CompletedAt)
            VALUES ($title, $comment, $cat, $prio, $dtype, $ddays, $ddate, $created, $done, $doneat);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$title", task.Title);
        cmd.Parameters.AddWithValue("$comment", (object?)task.Comment ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cat", (object?)task.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$prio", (int)task.Priority);
        cmd.Parameters.AddWithValue("$dtype", (int)task.DeadlineType);
        cmd.Parameters.AddWithValue("$ddays", (object?)task.DeadlineDays ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ddate", task.DeadlineDate is DateTime d ? d.ToString("yyyy-MM-dd") : DBNull.Value);
        cmd.Parameters.AddWithValue("$created", task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("$done", task.IsCompleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$doneat", task.CompletedAt is DateTime c ? c.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        return Convert.ToInt64(cmd.ExecuteScalar());
    }

    public void UpdateTask(PlannerTask task)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE tasks SET
                Title = $title, Comment = $comment, CategoryId = $cat, Priority = $prio, DeadlineType = $dtype,
                DeadlineDays = $ddays, DeadlineDate = $ddate, CreatedAt = $created,
                IsCompleted = $done, CompletedAt = $doneat
            WHERE Id = $id
            """;
        cmd.Parameters.AddWithValue("$id", task.Id);
        cmd.Parameters.AddWithValue("$title", task.Title);
        cmd.Parameters.AddWithValue("$comment", (object?)task.Comment ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cat", (object?)task.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$prio", (int)task.Priority);
        cmd.Parameters.AddWithValue("$dtype", (int)task.DeadlineType);
        cmd.Parameters.AddWithValue("$ddays", (object?)task.DeadlineDays ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$ddate", task.DeadlineDate is DateTime d ? d.ToString("yyyy-MM-dd") : DBNull.Value);
        cmd.Parameters.AddWithValue("$created", task.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss"));
        cmd.Parameters.AddWithValue("$done", task.IsCompleted ? 1 : 0);
        cmd.Parameters.AddWithValue("$doneat", task.CompletedAt is DateTime c ? c.ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value);
        cmd.ExecuteNonQuery();
    }

    public void DeleteTask(long id)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM tasks WHERE Id = $id";
        cmd.Parameters.AddWithValue("$id", id);
        cmd.ExecuteNonQuery();
    }
}
