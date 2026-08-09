namespace OD.Planner.Models;

public sealed class AlarmEntry
{
    public required PlannerTask Task { get; init; }
    public required AlarmLevel Level { get; init; }
    public int DaysRemaining { get; init; }
}
