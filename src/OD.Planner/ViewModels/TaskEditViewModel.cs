using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OD.Planner.Localization;
using OD.Planner.Models;

namespace OD.Planner.ViewModels;

/// <summary>
/// Represents a category option in the task edit dialog.
/// </summary>
public sealed class CategoryOption
{
    /// <summary>
    /// Gets the category ID (null for "no category").
    /// </summary>
    public long? Id { get; init; }

    /// <summary>
    /// Gets the category display name.
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Returns the category name.
    /// </summary>
    public override string ToString() => Name;
}

/// <summary>
/// Represents a priority option in the task edit dialog.
/// </summary>
public sealed class PriorityOption
{
    /// <summary>
    /// Gets the priority value.
    /// </summary>
    public Priority Value { get; init; }

    /// <summary>
    /// Gets the priority display label.
    /// </summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>
    /// Gets the priority foreground brush.
    /// </summary>
    public Brush? Foreground { get; init; }

    /// <summary>
    /// Returns the priority label.
    /// </summary>
    public override string ToString() => Label;
}

/// <summary>
/// ViewModel for the Task Edit dialog.
/// Handles creating new tasks and editing existing ones.
/// </summary>
public sealed partial class TaskEditViewModel : ObservableObject
{
    /// <summary>
    /// Gets whether this is a new task (vs. editing an existing one).
    /// </summary>
    public bool IsNew { get; }

    /// <summary>
    /// Gets the task being edited.
    /// </summary>
    public PlannerTask Task { get; }

    /// <summary>
    /// Gets the window title based on whether this is a new or existing task.
    /// </summary>
    public string WindowTitle => IsNew
        ? LocalizationService.Instance["NewTaskTitle"]
        : LocalizationService.Instance["EditTaskTitle"];

    /// <summary>
    /// Gets or sets the task title.
    /// </summary>
    [ObservableProperty]
    private string title;

    /// <summary>
    /// Gets or sets the selected category.
    /// </summary>
    [ObservableProperty]
    private CategoryOption? selectedCategory;

    /// <summary>
    /// Gets or sets the selected priority.
    /// </summary>
    [ObservableProperty]
    private PriorityOption? selectedPriority;

    /// <summary>
    /// Gets or sets the deadline type.
    /// </summary>
    [ObservableProperty]
    private DeadlineType deadlineType;

    /// <summary>
    /// Gets or sets the deadline days (for DaysFromCreation type).
    /// </summary>
    [ObservableProperty]
    private int deadlineDays = 1;

    /// <summary>
    /// Gets or sets the deadline date (for FixedDate type).
    /// </summary>
    [ObservableProperty]
    private DateTime? deadlineDate;

    /// <summary>
    /// Gets or sets the validation error message.
    /// </summary>
    [ObservableProperty]
    private string? error;

    /// <summary>
    /// Gets the collection of available categories.
    /// </summary>
    public ObservableCollection<CategoryOption> Categories { get; } = new();

    /// <summary>
    /// Gets the collection of available priorities.
    /// </summary>
    public ObservableCollection<PriorityOption> Priorities { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskEditViewModel"/> class.
    /// </summary>
    /// <param name="original">The original task to edit, or null for a new task.</param>
    /// <param name="categories">The available categories.</param>
    public TaskEditViewModel(PlannerTask? original, IReadOnlyList<Category> categories)
    {
        IsNew = original is null;
        Task = original?.Clone() ?? new PlannerTask { CreatedAt = DateTime.Now, Priority = Priority.Medium };
        title = Task.Title;
        deadlineType = Task.DeadlineType;
        deadlineDays = Task.DeadlineDays ?? 1;
        deadlineDate = Task.DeadlineDate;

        Categories.Add(new CategoryOption { Id = null, Name = LocalizationService.Instance["NoCategory"] });
        foreach (var category in categories.OrderBy(c => c.Name, StringComparer.CurrentCultureIgnoreCase))
        {
            Categories.Add(new CategoryOption { Id = category.Id, Name = category.Name });
        }

        selectedCategory = Categories.FirstOrDefault(o => o.Id == Task.CategoryId) ?? Categories[0];

        Priorities.Add(new PriorityOption { Value = Priority.Low, Label = LocalizationService.Instance["PriorityLow"], Foreground = Res("PriorityLowForeground") });
        Priorities.Add(new PriorityOption { Value = Priority.Medium, Label = LocalizationService.Instance["PriorityMedium"], Foreground = Res("PriorityMediumForeground") });
        Priorities.Add(new PriorityOption { Value = Priority.Urgent, Label = LocalizationService.Instance["PriorityUrgent"], Foreground = Res("PriorityUrgentForeground") });
        Priorities.Add(new PriorityOption { Value = Priority.VeryUrgent, Label = LocalizationService.Instance["PriorityVeryUrgent"], Foreground = Res("PriorityVeryUrgentForeground") });

        selectedPriority = Priorities.FirstOrDefault(p => p.Value == Task.Priority) ?? Priorities[1];
    }

    /// <summary>
    /// Validates and saves the task data.
    /// </summary>
    /// <returns>True if validation passes; otherwise, false.</returns>
    public bool Save()
    {
        if (string.IsNullOrWhiteSpace(Title))
        {
            Error = LocalizationService.Instance["TitleRequired"];
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

    /// <summary>
    /// Finds a brush resource by key.
    /// </summary>
    /// <param name="key">The resource key.</param>
    /// <returns>The brush if found; otherwise, a transparent brush.</returns>
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
