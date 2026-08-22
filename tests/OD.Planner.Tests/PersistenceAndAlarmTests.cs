using OD.Planner.Data;
using OD.Planner.Models;
using OD.Planner.Services;
using Microsoft.Data.Sqlite;

namespace OD.Planner.Tests;

public sealed class AppDatabaseTests : IDisposable
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"od-planner-{Guid.NewGuid():N}.db");

    [Fact]
    public void EnsureCreated_MigratesExistingDatabaseToSupportComments()
    {
        using (var connection = new SqliteConnection($"Data Source={_databasePath}"))
        {
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE schema_version (Version INTEGER NOT NULL);
                INSERT INTO schema_version (Version) VALUES (1);
                CREATE TABLE categories (Id INTEGER PRIMARY KEY AUTOINCREMENT, Name TEXT NOT NULL UNIQUE);
                CREATE TABLE tasks (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Title TEXT NOT NULL,
                    CategoryId INTEGER NULL,
                    Priority INTEGER NOT NULL DEFAULT 1,
                    DeadlineType INTEGER NOT NULL DEFAULT 0,
                    DeadlineDays INTEGER NULL,
                    DeadlineDate TEXT NULL,
                    CreatedAt TEXT NOT NULL,
                    IsCompleted INTEGER NOT NULL DEFAULT 0,
                    CompletedAt TEXT NULL
                );
                """;
            command.ExecuteNonQuery();
        }

        var database = new AppDatabase(_databasePath);
        database.EnsureCreated();
        database.InsertTask(new PlannerTask
        {
            Title = "Migrated task",
            Comment = "Migrated comment",
            CreatedAt = DateTime.Now,
        });

        var task = Assert.Single(database.GetTasks());
        Assert.Equal("Migrated comment", task.Comment);
    }

    [Fact]
    public void DeleteCategory_SetsRelatedTaskCategoryToNull()
    {
        var database = new AppDatabase(_databasePath);
        database.EnsureCreated();
        var category = database.GetCategories().First();
        var task = new PlannerTask
        {
            Title = "Task with category",
            CategoryId = category.Id,
            CreatedAt = DateTime.Now,
        };

        task.Id = database.InsertTask(task);
        database.DeleteCategory(category.Id);

        var savedTask = Assert.Single(database.GetTasks());
        Assert.Null(savedTask.CategoryId);
    }

    [Fact]
    public void UpdateTask_PersistsAllEditableFields()
    {
        var database = new AppDatabase(_databasePath);
        database.EnsureCreated();
        var task = new PlannerTask
        {
            Title = "Initial title",
            Comment = "Initial comment",
            Priority = Priority.Low,
            DeadlineType = DeadlineType.FixedDate,
            DeadlineDate = new DateTime(2026, 8, 15),
            CreatedAt = new DateTime(2026, 8, 1, 9, 30, 0),
        };

        task.Id = database.InsertTask(task);
        task.Title = "Updated title";
        task.Comment = "First line\nSecond line";
        task.Priority = Priority.VeryUrgent;
        task.IsCompleted = true;
        task.CompletedAt = new DateTime(2026, 8, 2, 10, 0, 0);
        database.UpdateTask(task);

        var savedTask = Assert.Single(database.GetTasks());
        Assert.Equal("Updated title", savedTask.Title);
        Assert.Equal("First line\nSecond line", savedTask.Comment);
        Assert.Equal(Priority.VeryUrgent, savedTask.Priority);
        Assert.True(savedTask.IsCompleted);
        Assert.Equal(task.CompletedAt, savedTask.CompletedAt);
    }

    public void Dispose()
    {
        SqliteConnection.ClearAllPools();
        if (File.Exists(_databasePath))
        {
            File.Delete(_databasePath);
        }
    }
}

public sealed class AlarmEngineTests : IDisposable
{
    private readonly SoundService _sounds = new();

    [Fact]
    public void CheckNow_RaisesStoppedAlarmAgainWhenDeadlineChangesAtSameLevel()
    {
        var task = new PlannerTask
        {
            Id = 1,
            Title = "Overdue task",
            DeadlineType = DeadlineType.FixedDate,
            DeadlineDate = DateTime.Today.AddDays(-1),
        };
        var settings = new AppSettings { SoundEnabled = true, OverdueEnabled = true };
        var engine = new AlarmEngine(() => new[] { task }, settings, _sounds);
        IReadOnlyList<AlarmEntry>? entries = null;
        engine.AlarmRaised += raised => entries = raised;

        engine.CheckNow();
        var originalEntry = Assert.Single(entries!);
        engine.Stop(originalEntry);
        entries = null;

        engine.CheckNow();
        Assert.Null(entries);

        task.DeadlineDate = DateTime.Today.AddDays(-2);
        engine.CheckNow();

        var reRaisedEntry = Assert.Single(entries!);
        Assert.Equal(AlarmLevel.Overdue, reRaisedEntry.Level);
        engine.Dispose();
    }

    public void Dispose() => _sounds.Dispose();
}
