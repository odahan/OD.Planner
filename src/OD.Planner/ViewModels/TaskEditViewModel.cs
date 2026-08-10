using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OD.Planner.Models;

namespace OD.Planner.ViewModels;

public sealed class CategoryOption
{
    public long? Id { get; init; }
    public string Name { get; init; } = string.Empty;

    public override string ToString() => Name;
}

public sealed class PriorityOption
{
    public Priority Value { get; init; }
    public string Label { get; init; } = string.Empty;
    public Brush? Foreground { get; init; }

    public override string ToString() => Label;
}

public sealed partial class TaskEditViewModel : ObservableObject
{
    public bool IsNew { get; }
    public PlannerTask Task { get; }

    public string WindowTitle => IsNew ? "Nouvelle tâche" : "Modifier la tâche";

    [ObservableProperty]
    private string title;

    [ObservableProperty]
    private CategoryOption? selectedCategory;

    [ObservableProperty]
    private PriorityOption? selectedPriority;

    [ObservableProperty]
    private DeadlineType deadlineType;

    [ObservableProperty]
    private int deadlineDays = 1;

    [ObservableProperty]
    private DateTime? deadlineDate;

    [ObservableProperty]
    private string? error;

    public ObservableCollection<CategoryOption> Categories { get; } = new();
    public ObservableCollection<PriorityOption> Priorities { get; } = new();

    public TaskEditViewModel(PlannerTask? original, IReadOnlyList<Category> categories)
    {
        IsNew = original is null;
        Task = original?.Clone() ?? new PlannerTask { CreatedAt = DateTime.Now, Priority = Priority.Medium };
        title = Task.Title;
        deadlineType = Task.DeadlineType;
        deadlineDays = Task.DeadlineDays ?? 1;
        deadlineDate = Task.DeadlineDate;

        Categories.Add(new CategoryOption { Id = null, Name = "Sans catégorie" });
        foreach (var category in categories.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Categories.Add(new CategoryOption { Id = category.Id, Name = category.Name });
        }

        selectedCategory = Categories.FirstOrDefault(o => o.Id == Task.CategoryId) ?? Categories[0];

        Priorities.Add(new PriorityOption { Value = Priority.Low, Label = "Faible", Foreground = Res("PriorityLowForeground") });
        Priorities.Add(new PriorityOption { Value = Priority.Medium, Label = "Moyenne", Foreground = Res("PriorityMediumForeground") });
        Priorities.Add(new PriorityOption { Value = Priority.Urgent, Label = "Urgente", Foreground = Res("PriorityUrgentForeground") });
        Priorities.Add(new PriorityOption { Value = Priority.VeryUrgent, Label = "Très urgente", Foreground = Res("PriorityVeryUrgentForeground") });

        selectedPriority = Priorities.FirstOrDefault(p => p.Value == Task.Priority) ?? Priorities[1];
    }

    public bool Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            Error = "Le titre est obligatoire.";
            return false;
        }

        Task.Title = Title.Trim();
        Task.CategoryId = SelectedCategory?.Id;
        Task.Priority = SelectedPriority?.Value ?? Task.Priority;
        Task.DeadlineType = DeadlineType;
        Task.DeadlineDays = DeadlineType == DeadlineType.DaysFromCreation ? Math.Max(1, DeadlineDays) : null;
        Task.DeadlineDate = DeadlineType == DeadlineType.FixedDate ? DeadlineDate?.Date : null;
        Error = null;
        return true;
    }

    private static Brush? Res(string key)
    {
        if (Application.Current is null)
        {
            return Brushes.Transparent;
        }

        var value = Application.Current.TryFindResource(key) as Brush;
        return value ?? Brushes.Transparent;
    }
}
