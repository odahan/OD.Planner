using OD.Planner.Models;

namespace OD.Planner.Logic;

public static class AlarmEvaluator
{
    public static AlarmLevel GetLevel(int daysRemaining)
    {
        if (daysRemaining < 0)
        {
            return AlarmLevel.Overdue;
        }

        if (daysRemaining == 0)
        {
            return AlarmLevel.Due;
        }

        if (daysRemaining == 1)
        {
            return AlarmLevel.Attention;
        }

        return AlarmLevel.None;
    }

    public static AlarmLevel GetLevel(PlannerTask task, DateTime today)
    {
        var days = DeadlineCalculator.GetDaysRemaining(task, today);
        return days.HasValue ? GetLevel(days.Value) : AlarmLevel.None;
    }
}
