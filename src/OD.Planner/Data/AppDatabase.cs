using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using OD.Planner.Models;

namespace OD.Planner.Data;

public sealed class AppDatabase
{
    private readonly string _connectionString;

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
            Cache = SqliteCacheMode.Shared,
        }.ToString();
    }

    private SqliteConnection Open() => new(_connectionString);

    public void EnsureCreated()
    {
        using var conn = Open();
        conn.Open();
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

    private static void Execute(SqliteConnection conn, string sql)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
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
        cmd.CommandText = "SELECT Id, Title, CategoryId, Priority, DeadlineType, DeadlineDays, DeadlineDate, CreatedAt, IsCompleted, CompletedAt FROM tasks";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            result.Add(new PlannerTask
            {
                Id = reader.GetInt64(0),
                Title = reader.GetString(1),
                CategoryId = reader.IsDBNull(2) ? null : reader.GetInt64(2),
                Priority = (Priority)reader.GetInt32(3),
                DeadlineType = (DeadlineType)reader.GetInt32(4),
                DeadlineDays = reader.IsDBNull(5) ? null : reader.GetInt32(5),
                DeadlineDate = reader.IsDBNull(6) ? null : DateTime.ParseExact(reader.GetString(6), "yyyy-MM-dd", CultureInfo.InvariantCulture),
                CreatedAt = DateTime.ParseExact(reader.GetString(7), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                IsCompleted = reader.GetInt64(8) != 0,
                CompletedAt = reader.IsDBNull(9)
                    ? null
                    : DateTime.ParseExact(reader.GetString(9), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
            });
        }

        return result;
    }

    public long InsertTask(PlannerTask task)
    {
        using var conn = Open();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO tasks (Title, CategoryId, Priority, DeadlineType, DeadlineDays, DeadlineDate, CreatedAt, IsCompleted, CompletedAt)
            VALUES ($title, $cat, $prio, $dtype, $ddays, $ddate, $created, $done, $doneat);
            SELECT last_insert_rowid();
            """;
        cmd.Parameters.AddWithValue("$title", task.Title);
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
                Title = $title, CategoryId = $cat, Priority = $prio, DeadlineType = $dtype,
                DeadlineDays = $ddays, DeadlineDate = $ddate, CreatedAt = $created,
                IsCompleted = $done, CompletedAt = $doneat
            WHERE Id = $id
            """;
        cmd.Parameters.AddWithValue("$id", task.Id);
        cmd.Parameters.AddWithValue("$title", task.Title);
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
