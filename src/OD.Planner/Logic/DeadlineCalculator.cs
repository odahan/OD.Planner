using OD.Planner.Models;

namespace OD.Planner.Logic;

public static class DeadlineCalculator
{
    public static DateTime? GetEffectiveDeadline(PlannerTask task)
    {
        if (task.DeadlineType == DeadlineType.DaysFromCreation && task.DeadlineDays is int days)
        {
            return task.CreatedAt.Date.AddDays(days);
        }

        if (task.DeadlineType == DeadlineType.FixedDate && task.DeadlineDate is DateTime date)
        {
            return date.Date;
        }

        return null;
    }

    public static int? GetDaysRemaining(PlannerTask task, DateTime today)
    {
        var deadline = GetEffectiveDeadline(task);
        if (!deadline.HasValue)
        {
            return null;
        }

        return (int)(deadline.Value.Date - today.Date).TotalDays;
    }
}
