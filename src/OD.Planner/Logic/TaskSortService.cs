using OD.Planner.Models;

namespace OD.Planner.Logic;

public static class TaskSortService
{
    public static IEnumerable<PlannerTask> Sort(IEnumerable<PlannerTask> tasks)
    {
        return tasks
            .OrderBy(t => t.IsCompleted)
            .ThenBy(t => DeadlineCalculator.GetEffectiveDeadline(t) ?? DateTime.MaxValue)
            .ThenByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt);
    }
}
