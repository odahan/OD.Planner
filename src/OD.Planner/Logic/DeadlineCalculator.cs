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

        // Use Date property to ensure we're comparing dates only (no time component).
        // This avoids DST issues since we're working with whole days only.
        var deadlineDate = deadline.Value.Date;
        var todayDate = today.Date;

        return (int)(deadlineDate - todayDate).TotalDays;
    }

    /// <summary>
    /// Gets the days remaining using the current local date.
    /// </summary>
    public static int? GetDaysRemaining(PlannerTask task)
    {
        return GetDaysRemaining(task, DateTime.Today);
    }
}
