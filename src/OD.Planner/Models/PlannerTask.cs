namespace OD.Planner.Models;

public sealed class PlannerTask
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public long? CategoryId { get; set; }
    public Priority Priority { get; set; } = Priority.Medium;
    public DeadlineType DeadlineType { get; set; } = DeadlineType.None;
    public int? DeadlineDays { get; set; }
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
