namespace OD.Planner.Models;

public sealed class PlannerTask
{
    private int? _deadlineDays;

    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public DeadlineType DeadlineType { get; set; } = DeadlineType.None;

    public int? DeadlineDays
    {
        get => _deadlineDays;
        set => _deadlineDays = value is int d && d < 1 ? 1 : value;
    }

    public DateTime? DeadlineDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime? CompletedAt { get; set; }

    public PlannerTask Clone() => new()
    {
        Id = Id,
        Title = Title,
        CategoryId = CategoryId,
        Priority = Priority,
        DeadlineType = DeadlineType,
        DeadlineDays = DeadlineDays,
        DeadlineDate = DeadlineDate,
        CreatedAt = CreatedAt,
        IsCompleted = IsCompleted,
        CompletedAt = CompletedAt,
    };
}
