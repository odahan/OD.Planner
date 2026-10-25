using CommunityToolkit.Mvvm.ComponentModel;
using OD.Planner.Localization;
using OD.Planner.Models;

namespace OD.Planner.ViewModels;

/// <summary>
/// Represents a localized export mode.
/// </summary>
public sealed record TaskExportOption(TaskExportMode Mode, string DisplayName)
{
    /// <summary>
    /// Returns the localized label used by the combo box selection template.
    /// </summary>
    public override string ToString() => DisplayName;
}

/// <summary>
/// Manages the HTML export mode selection.
/// </summary>
public sealed partial class TaskExportViewModel : ObservableObject
{
    /// <summary>
    /// Gets the available report modes.
    /// </summary>
    public IReadOnlyList<TaskExportOption> Modes { get; } =
    [
        new(TaskExportMode.Active, LocalizationService.Instance["ActiveTasks"]),
        new(TaskExportMode.Done, LocalizationService.Instance["CompletedTasks"]),
        new(TaskExportMode.All, LocalizationService.Instance["ShowAllCategories"]),
    ];

    /// <summary>
    /// Gets or sets the selected export mode.
    /// </summary>
    [ObservableProperty]
    private TaskExportOption selectedMode;

    /// <summary>
    /// Initializes the export selection using the current task view.
    /// </summary>
    public TaskExportViewModel(bool showCompleted)
    {
        selectedMode = Modes[showCompleted ? 1 : 0];
    }
}
