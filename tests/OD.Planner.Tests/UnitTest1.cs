using OD.Planner.Logic;
using OD.Planner.Models;

namespace OD.Planner.Tests;

public class DeadlineCalculatorTests
{
    [Fact]
    public void GetEffectiveDeadline_DaysFromCreation_ReturnsCorrectDate()
    {
        var task = new PlannerTask
        {
            DeadlineType = DeadlineType.DaysFromCreation,
            DeadlineDays = 7,
            CreatedAt = new DateTime(2024, 1, 1, 14, 30, 0)
        };

        var result = DeadlineCalculator.GetEffectiveDeadline(task);

        Assert.Equal(new DateTime(2024, 1, 8), result);
    }

    [Fact]
    public void GetEffectiveDeadline_FixedDate_ReturnsDateOnly()
    {
        var task = new PlannerTask
        {
            DeadlineType = DeadlineType.FixedDate,
            DeadlineDate = new DateTime(2024, 6, 15, 10, 30, 0)
        };

        var result = DeadlineCalculator.GetEffectiveDeadline(task);

        Assert.Equal(new DateTime(2024, 6, 15), result);
    }

    [Fact]
    public void GetEffectiveDeadline_None_ReturnsNull()
    {
        var task = new PlannerTask
        {
            DeadlineType = DeadlineType.None
        };

        var result = DeadlineCalculator.GetEffectiveDeadline(task);

        Assert.Null(result);
    }

    [Fact]
    public void GetDaysRemaining_Today_ReturnsZero()
    {
        var task = new PlannerTask
        {
            DeadlineType = DeadlineType.FixedDate,
            DeadlineDate = new DateTime(2024, 3, 15)
        };

        var result = DeadlineCalculator.GetDaysRemaining(task, new DateTime(2024, 3, 15));

        Assert.Equal(0, result);
    }

    [Fact]
    public void GetDaysRemaining_Tomorrow_ReturnsOne()
    {
        var task = new PlannerTask
        {
            DeadlineType = DeadlineType.FixedDate,
            DeadlineDate = new DateTime(2024, 3, 16)
        };

        var result = DeadlineCalculator.GetDaysRemaining(task, new DateTime(2024, 3, 15));

        Assert.Equal(1, result);
    }

    [Fact]
    public void GetDaysRemaining_Yesterday_ReturnsMinusOne()
    {
        var task = new PlannerTask
        {
            DeadlineType = DeadlineType.FixedDate,
            DeadlineDate = new DateTime(2024, 3, 14)
        };

        var result = DeadlineCalculator.GetDaysRemaining(task, new DateTime(2024, 3, 15));

        Assert.Equal(-1, result);
    }

    [Fact]
    public void GetDaysRemaining_NoDeadline_ReturnsNull()
    {
        var task = new PlannerTask
        {
            DeadlineType = DeadlineType.None
        };

        var result = DeadlineCalculator.GetDaysRemaining(task, DateTime.Today);

        Assert.Null(result);
    }
}

public class AlarmEvaluatorTests
{
    [Fact]
    public void GetLevel_Overdue_ReturnsOverdue()
    {
        Assert.Equal(AlarmLevel.Overdue, AlarmEvaluator.GetLevel(-1));
        Assert.Equal(AlarmLevel.Overdue, AlarmEvaluator.GetLevel(-7));
    }

    [Fact]
    public void GetLevel_Today_ReturnsDue()
    {
        Assert.Equal(AlarmLevel.Due, AlarmEvaluator.GetLevel(0));
    }

    [Fact]
    public void GetLevel_Tomorrow_ReturnsAttention()
    {
        Assert.Equal(AlarmLevel.Attention, AlarmEvaluator.GetLevel(1));
    }

    [Fact]
    public void GetLevel_Future_ReturnsNone()
    {
        Assert.Equal(AlarmLevel.None, AlarmEvaluator.GetLevel(2));
        Assert.Equal(AlarmLevel.None, AlarmEvaluator.GetLevel(30));
    }

    [Fact]
    public void GetLevel_WithTask_ReturnsCorrectLevel()
    {
        var task = new PlannerTask
        {
            DeadlineType = DeadlineType.FixedDate,
            DeadlineDate = DateTime.Today.AddDays(-1)
        };

        var level = AlarmEvaluator.GetLevel(task, DateTime.Today);

        Assert.Equal(AlarmLevel.Overdue, level);
    }

    [Fact]
    public void GetLevel_WithTaskNoDeadline_ReturnsNone()
    {
        var task = new PlannerTask
        {
            DeadlineType = DeadlineType.None
        };

        var level = AlarmEvaluator.GetLevel(task, DateTime.Today);

        Assert.Equal(AlarmLevel.None, level);
    }
}

public class TaskSortServiceTests
{
    [Fact]
    public void Sort_IncompleteBeforeComplete()
    {
        var tasks = new List<PlannerTask>
        {
            new() { Id = 1, IsCompleted = true, CreatedAt = DateTime.MinValue },
            new() { Id = 2, IsCompleted = false, CreatedAt = DateTime.MinValue }
        };

        var result = TaskSortService.Sort(tasks).ToList();

        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
    }

    [Fact]
    public void Sort_EarlierDeadlineFirst()
    {
        var tasks = new List<PlannerTask>
        {
            new() { Id = 1, IsCompleted = false, DeadlineType = DeadlineType.FixedDate, DeadlineDate = new DateTime(2024, 6, 1) },
            new() { Id = 2, IsCompleted = false, DeadlineType = DeadlineType.FixedDate, DeadlineDate = new DateTime(2024, 3, 1) }
        };

        var result = TaskSortService.Sort(tasks).ToList();

        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
    }

    [Fact]
    public void Sort_HigherPriorityFirst()
    {
        var tasks = new List<PlannerTask>
        {
            new() { Id = 1, IsCompleted = false, Priority = Priority.Low, CreatedAt = DateTime.MinValue },
            new() { Id = 2, IsCompleted = false, Priority = Priority.VeryUrgent, CreatedAt = DateTime.MinValue }
        };

        var result = TaskSortService.Sort(tasks).ToList();

        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
    }

    [Fact]
    public void Sort_NoDeadlineLast()
    {
        var tasks = new List<PlannerTask>
        {
            new() { Id = 1, IsCompleted = false, DeadlineType = DeadlineType.None, CreatedAt = DateTime.MinValue },
            new() { Id = 2, IsCompleted = false, DeadlineType = DeadlineType.FixedDate, DeadlineDate = new DateTime(2024, 12, 31) }
        };

        var result = TaskSortService.Sort(tasks).ToList();

        Assert.Equal(2, result[0].Id);
        Assert.Equal(1, result[1].Id);
    }
}
