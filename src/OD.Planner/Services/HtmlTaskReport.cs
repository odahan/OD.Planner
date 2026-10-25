using System.Globalization;
using System.Net;
using System.Text;
using OD.Planner.Logic;
using OD.Planner.Models;

namespace OD.Planner.Services;

/// <summary>
/// Generates standalone dark HTML task reports without external resources or scripts.
/// </summary>
public static class HtmlTaskReport
{
    /// <summary>
    /// Builds a report for the selected task status across all categories.
    /// </summary>
    public static string Generate(
        IEnumerable<PlannerTask> tasks,
        IReadOnlyDictionary<long, string> categories,
        TaskExportMode mode,
        string version,
        string userName,
        DateTimeOffset generatedAt)
    {
        var filtered = mode switch
        {
            TaskExportMode.Active => tasks.Where(task => !task.IsCompleted),
            TaskExportMode.Done => tasks.Where(task => task.IsCompleted),
            TaskExportMode.All => tasks,
            _ => throw new ArgumentOutOfRangeException(nameof(mode)),
        };
        var ordered = TaskSortService.Sort(filtered, groupByCompletion: false).ToList();
        var title = Encode($"OD.Planner V {version}");
        var html = new StringBuilder();
        html.Append($$"""
            <!doctype html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <meta name="color-scheme" content="dark">
            <title>{{title}}</title>
            <style>
            :root { color-scheme: dark; font-family: "Segoe UI", sans-serif; background: #16181d; color: #eceff4; }
            * { box-sizing: border-box; }
            body { margin: 0; padding: 36px; }
            main { max-width: 1600px; margin: auto; }
            h1 { margin: 0 0 12px; font-size: 28px; }
            .metadata { color: #aeb6c2; line-height: 1.8; margin-bottom: 22px; }
            .legend { display: flex; flex-wrap: wrap; gap: 16px; margin: 18px 0; color: #aeb6c2; }
            .legend span { display: inline-flex; align-items: center; gap: 8px; }
            .table-scroll { overflow-x: auto; border: 1px solid #343943; border-radius: 10px; }
            table { width: 100%; border-collapse: collapse; background: #1e2127; }
            th, td { padding: 14px 16px; text-align: left; vertical-align: top; border-bottom: 1px solid #343943; }
            th { background: #262a31; color: #aeb6c2; font-size: 12px; letter-spacing: .04em; text-transform: uppercase; }
            tbody tr:last-child td { border-bottom: 0; }
            tbody tr:nth-child(even) { background: #22262d; }
            .task-title { min-width: 180px; font-weight: 600; }
            .comment { min-width: 200px; max-width: 520px; white-space: pre-wrap; overflow-wrap: anywhere; }
            .task-title, .category { overflow-wrap: anywhere; }
            .deadline { white-space: nowrap; }
            .status { display: inline-flex; flex-shrink: 0; align-items: center; justify-content: center; width: 23px; height: 23px;
                border: 2px solid currentColor; border-radius: 4px; font: bold 22px/1 "Segoe UI", sans-serif; }
            .active { color: #69de9a; }
            .due { color: #ff7979; }
            .done { color: #ffffff; }
            .empty { color: #aeb6c2; text-align: center; padding: 32px; }
            @media (max-width: 640px) { body { padding: 18px 12px; } th, td { padding: 10px; } }
            </style>
            </head>
            <body><main>
            <h1>{{title}}</h1>
            <div class="metadata">Created: {{Encode(generatedAt.ToString("yyyy-MM-dd HH:mm:ss zzz", CultureInfo.InvariantCulture))}}<br>
            Windows user: {{Encode(userName)}}<br>
            View: {{mode}} · {{ordered.Count}} task(s) · All categories</div>
            <div class="legend">
            <span><span class="status active" aria-hidden="true">·</span>Active</span>
            <span><span class="status due" aria-hidden="true">·</span>Due today or overdue</span>
            <span><span class="status done" aria-hidden="true">×</span>Done</span>
            </div>
            <div class="table-scroll"><table aria-label="Tasks">
            <thead><tr><th scope="col">Status</th><th scope="col">Title</th><th scope="col">Category</th><th scope="col">Priority</th><th scope="col">Deadline</th><th scope="col">Comment</th></tr></thead>
            <tbody>
            """);

        foreach (var task in ordered)
        {
            var deadline = DeadlineCalculator.GetEffectiveDeadline(task);
            var (statusClass, statusLabel, symbol) = task.IsCompleted
                ? ("done", "Done", "×")
                : deadline.HasValue && deadline.Value.Date <= generatedAt.Date
                    ? ("due", "Active — due today or overdue", "·")
                    : ("active", "Active", "·");
            var category = task.CategoryId is long id && categories.TryGetValue(id, out var name) ? name : "—";
            var priority = task.Priority == Priority.VeryUrgent ? "Very urgent" : task.Priority.ToString();
            html.AppendLine($$"""
                <tr>
                <td><span class="status {{statusClass}}" role="img" aria-label="{{Encode(statusLabel)}}" title="{{Encode(statusLabel)}}">{{symbol}}</span></td>
                <td class="task-title">{{Encode(task.Title)}}</td>
                <td class="category">{{Encode(category)}}</td>
                <td>{{Encode(priority)}}</td>
                <td class="deadline">{{Encode(deadline?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—")}}</td>
                <td class="comment">{{Encode(task.Comment)}}</td>
                </tr>
                """);
        }

        if (ordered.Count == 0)
        {
            html.AppendLine("<tr><td class=\"empty\" colspan=\"6\">No tasks in this view.</td></tr>");
        }

        html.AppendLine("</tbody></table></div></main></body></html>");
        return html.ToString();
    }

    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
