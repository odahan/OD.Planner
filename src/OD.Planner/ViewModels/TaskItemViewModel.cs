using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OD.Planner.Localization;
using OD.Planner.Logic;
using OD.Planner.Models;

namespace OD.Planner.ViewModels;

/// <summary>
/// ViewModel for individual task items in the main window list.
/// Displays task information including title, priority, deadline, and category.
/// </summary>
public sealed partial class TaskItemViewModel : ObservableObject
{
    private readonly string? _categoryName;
    private string _categoryLabel = string.Empty;
    private bool _hasCategory;
    private string _priorityLabel = string.Empty;
    private Brush? _priorityBackground;
    private Brush? _priorityForeground;
    private string _deadlineText = string.Empty;
    private bool _deadlineVisible;
    private Brush? _deadlineForeground;
    private Brush? _deadlineBackground;
    private bool _isBlinking;
    private bool _blinkOn;
    private Brush? _glowBrush;

    // Static cache for brushes - loaded once and reused across all instances.
    // This avoids repeated Application.Current.TryFindResource calls on every Refresh.
    private static readonly BrushCache Cache = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskItemViewModel"/> class.
    /// </summary>
    public TaskItemViewModel(PlannerTask task, string? categoryName)
    {
        Task = task;
        _categoryName = categoryName;
        Refresh();
    }

    /// <summary>
    /// Gets the underlying task model.
    /// </summary>
    public PlannerTask Task { get; }

    /// <summary>
    /// Gets the task title.
    /// </summary>
    public string Title => Task.Title;

    /// <summary>
    /// Gets the task comment, if any.
    /// </summary>
    public string? Comment => Task.Comment;

    /// <summary>
    /// Gets whether the task has a comment to display in a tooltip.
    /// </summary>
    public bool HasComment => !string.IsNullOrWhiteSpace(Task.Comment);

    /// <summary>
    /// Gets whether the task is completed.
    /// </summary>
    public bool IsCompleted => Task.IsCompleted;

    /// <summary>
    /// Gets the category label.
    /// </summary>
    public string CategoryLabel => _categoryLabel;

    /// <summary>
    /// Gets whether the task has a category.
    /// </summary>
    public bool HasCategory => _hasCategory;

    /// <summary>
    /// Gets the priority label.
    /// </summary>
    public string PriorityLabel => _priorityLabel;

    /// <summary>
    /// Gets the priority background brush.
    /// </summary>
    public Brush? PriorityBackground => _priorityBackground;

    /// <summary>
    /// Gets the priority foreground brush.
    /// </summary>
    public Brush? PriorityForeground => _priorityForeground;

    /// <summary>
    /// Gets the deadline display text.
    /// </summary>
    public string DeadlineText => _deadlineText;

    /// <summary>
    /// Gets whether the deadline is visible.
    /// </summary>
    public bool DeadlineVisible => _deadlineVisible;

    /// <summary>
    /// Gets the deadline foreground brush.
    /// </summary>
    public Brush? DeadlineForeground => _deadlineForeground;

    /// <summary>
    /// Gets the deadline background brush.
    /// </summary>
    public Brush? DeadlineBackground => _deadlineBackground;

    /// <summary>
    /// Gets whether the task should blink (overdue or very urgent).
    /// </summary>
    public bool IsBlinking => _isBlinking;

    /// <summary>
    /// Gets or sets whether the blink animation is currently visible.
    /// </summary>
    public bool BlinkOn
    {
        get => _blinkOn;
        set
        {
            if (_blinkOn == value)
            {
                return;
            }

            _blinkOn = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets the glow brush for blinking tasks.
    /// </summary>
    public Brush? GlowBrush => _glowBrush;

    /// <summary>
    /// Refreshes the display properties from the task model.
    /// </summary>
    public void Refresh()
    {
        var today = DateTime.Today;
        var deadline = DeadlineCalculator.GetEffectiveDeadline(Task);
        int? days = null;
        if (deadline.HasValue)
        {
            days = (int)(deadline.Value.Date - today.Date).TotalDays;
        }

        var loc = LocalizationService.Instance;
        var daysSuffix = loc["DaysSuffix"];

        if (!deadline.HasValue)
        {
            _deadlineText = string.Empty;
            _deadlineVisible = false;
            _deadlineForeground = null;
            _deadlineBackground = null;
        }
        else if (days < 0)
        {
            _deadlineText = $"{days}{daysSuffix}";
            _deadlineVisible = true;
            _deadlineForeground = Cache.Get("OverdueForeground");
            _deadlineBackground = Cache.Get("OverdueBackground");
        }
        else if (days == 0)
        {
            _deadlineText = loc["Today"];
            _deadlineVisible = true;
            _deadlineForeground = Cache.Get("DueTodayForeground");
            _deadlineBackground = Cache.Get("DueTodayBackground");
        }
        else
        {
            _deadlineText = $"{days}{daysSuffix}";
            _deadlineVisible = true;
            _deadlineForeground = Cache.Get("TextSecondaryBrush");
            _deadlineBackground = Cache.Get("SurfaceAltBrush");
        }

        _priorityLabel = Task.Priority switch
        {
            Priority.Low => loc["PriorityLow"],
            Priority.Medium => loc["PriorityMedium"],
            Priority.Urgent => loc["PriorityUrgent"],
            _ => loc["PriorityVeryUrgent"],
        };

        (_priorityBackground, _priorityForeground) = Task.Priority switch
        {
            Priority.Low => (Cache.Get("PriorityLowBackground"), Cache.Get("PriorityLowForeground")),
            Priority.Medium => (Cache.Get("PriorityMediumBackground"), Cache.Get("PriorityMediumForeground")),
            Priority.Urgent => (Cache.Get("PriorityUrgentBackground"), Cache.Get("PriorityUrgentForeground")),
            _ => (Cache.Get("PriorityVeryUrgentBackground"), Cache.Get("PriorityVeryUrgentForeground")),
        };

        _categoryLabel = _categoryName ?? string.Empty;
        _hasCategory = _categoryName is not null;

        _isBlinking = !Task.IsCompleted &&
                       ((days.HasValue && days < 0) || Task.Priority == Priority.VeryUrgent);
        _glowBrush = Cache.Get("AlarmGlowBrush");

        OnPropertyChanged(string.Empty);
    }

    /// <summary>
    /// Caches WPF brushes after first lookup to avoid repeated resource tree walks.
    /// </summary>
    private sealed class BrushCache
    {
        private readonly Dictionary<string, Brush?> _cache = new();

        public Brush? Get(string key)
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                return cached;
            }

            var value = Application.Current?.TryFindResource(key) as Brush;
            _cache[key] = value;
            return value;
        }
    }
}
