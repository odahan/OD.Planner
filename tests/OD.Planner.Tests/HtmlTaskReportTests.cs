using OD.Planner.Models;
using OD.Planner.Services;

namespace OD.Planner.Tests;

public sealed class HtmlTaskReportTests
{
    private static readonly DateTimeOffset GeneratedAt = new(2026, 10, 25, 14, 30, 0, TimeSpan.FromHours(1));

    [Theory]
    [InlineData(TaskExportMode.Active, true, false)]
    [InlineData(TaskExportMode.Done, false, true)]
    [InlineData(TaskExportMode.All, true, true)]
    public void Generate_IncludesOnlySelectedStatusesAcrossCategories(TaskExportMode mode, bool active, bool done)
    {
        var tasks = new[]
        {
            new PlannerTask { Title = "Active task one", CategoryId = 1 },
            new PlannerTask { Title = "Active task two", CategoryId = 2 },
            new PlannerTask { Title = "Done task", IsCompleted = true, CategoryId = 3 },
        };

        var html = Generate(tasks, mode);

        Assert.Equal(active, html.Contains("Active task one"));
        Assert.Equal(active, html.Contains("Active task two"));
        Assert.Equal(done, html.Contains("Done task"));
    }

    [Fact]
    public void Generate_All_InterleavesStatusesUsingDeadlinePriorityAndCreationDate()
    {
        var tasks = new[]
        {
            TaskWithDeadline("Later active", 27),
            TaskWithDeadline("Earlier done", 24, done: true),
            TaskWithDeadline("Middle active", 25),
            TaskWithDeadline("Middle urgent done", 25, done: true, priority: Priority.Urgent),
            TaskWithDeadline("Middle older active", 25, created: new DateTime(2026, 1, 1)),
            new PlannerTask { Title = "No deadline done", IsCompleted = true },
        };

        var html = Generate(tasks);
        var expectedOrder = new[]
        {
            "Earlier done", "Middle urgent done", "Middle older active", "Middle active", "Later active", "No deadline done",
        };
        var positions = expectedOrder.Select(title => html.IndexOf(title, StringComparison.Ordinal)).ToArray();

        Assert.All(positions, position => Assert.True(position >= 0));
        Assert.Equal(positions.Order().ToArray(), positions);
    }

    [Theory]
    [InlineData(false, 24, "due", "·")]
    [InlineData(false, 25, "due", "·")]
    [InlineData(false, 26, "active", "·")]
    [InlineData(false, null, "active", "·")]
    [InlineData(true, 24, "done", "×")]
    [InlineData(true, null, "done", "×")]
    public void Generate_UsesCorrectStatusAndSymbol(bool done, int? day, string status, string symbol)
    {
        var task = day is int value ? TaskWithDeadline("Task", value, done) : new PlannerTask { IsCompleted = done };

        var html = Generate([task]);

        Assert.Contains($"class=\"status {status}\" role=\"img\"", html);
        Assert.Contains($">{symbol}</span></td>", html);
    }

    [Fact]
    public void Generate_ResolvesRelativeDeadlines()
    {
        var task = new PlannerTask
        {
            DeadlineType = DeadlineType.DaysFromCreation,
            CreatedAt = new DateTime(2026, 10, 20),
            DeadlineDays = 5,
        };

        var html = Generate([task]);

        Assert.Contains("class=\"status due\" role=\"img\"", html);
        Assert.Contains("<td class=\"deadline\">2026-10-25</td>", html);
    }

    [Fact]
    public void Generate_EncodesTaskContentAndMetadata()
    {
        var html = HtmlTaskReport.Generate(
            [new PlannerTask { Title = "<script>alert('title')</script>", Comment = "<img src=x onerror=alert(1)>\nNext line", CategoryId = 1 }],
            new Dictionary<long, string> { [1] = "<b>Category</b>" },
            TaskExportMode.All, "1.5.0", "<user>&", GeneratedAt);

        Assert.DoesNotContain("<script>", html);
        Assert.DoesNotContain("<img ", html);
        Assert.Contains("&lt;script&gt;", html);
        Assert.Contains("&lt;b&gt;Category&lt;/b&gt;", html);
        Assert.Contains("&lt;user&gt;&amp;", html);
        Assert.Contains("\nNext line", html);
        Assert.Contains("<title>OD.Planner V 1.5.0</title>", html);
        Assert.Contains("2026-10-25 14:30:00 +01:00", html);
    }

    [Fact]
    public void Generate_EmptyView_ProducesCompleteReport()
    {
        var html = Generate([new PlannerTask { IsCompleted = true }], TaskExportMode.Active);

        Assert.Contains("0 task(s)", html);
        Assert.Contains("No tasks in this view.", html);
        Assert.EndsWith("</tbody></table></div></main></body></html>" + Environment.NewLine, html);
    }

    private static string Generate(IEnumerable<PlannerTask> tasks, TaskExportMode mode = TaskExportMode.All)
        => HtmlTaskReport.Generate(tasks, new Dictionary<long, string>(), mode, "1.5.0", "Windows user", GeneratedAt);

    private static PlannerTask TaskWithDeadline(string title, int day, bool done = false, Priority priority = Priority.Medium, DateTime? created = null)
        => new()
        {
            Title = title,
            IsCompleted = done,
            DeadlineType = DeadlineType.FixedDate,
            DeadlineDate = new DateTime(2026, 10, day),
            Priority = priority,
            CreatedAt = created ?? new DateTime(2026, 10, 1),
        };
}
