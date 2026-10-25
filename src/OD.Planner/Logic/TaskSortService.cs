using OD.Planner.Models;

namespace OD.Planner.Logic;

public static class TaskSortService
{
    /// <summary>
    /// Orders tasks by deadline, priority, and creation date, optionally grouping completed tasks last.
    /// </summary>
    public static IEnumerable<PlannerTask> Sort(IEnumerable<PlannerTask> tasks, bool groupByCompletion = true)
    {
        return tasks
            .OrderBy(t => groupByCompletion && t.IsCompleted)
            .ThenBy(t => DeadlineCalculator.GetEffectiveDeadline(t) ?? DateTime.MaxValue)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt);
    }
}
