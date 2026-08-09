using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using OD.Planner.Logic;
using OD.Planner.Models;

namespace OD.Planner.ViewModels;

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

    public TaskItemViewModel(PlannerTask task, string? categoryName)
    {
        Task = task;
        _categoryName = categoryName;
        Refresh();
    }

    public PlannerTask Task { get; }

    public string Title => Task.Title;

    public bool IsCompleted => Task.IsCompleted;

    public string CategoryLabel => _categoryLabel;

    public bool HasCategory => _hasCategory;

    public string PriorityLabel => _priorityLabel;

    public Brush? PriorityBackground => _priorityBackground;

    public Brush? PriorityForeground => _priorityForeground;

    public string DeadlineText => _deadlineText;

    public bool DeadlineVisible => _deadlineVisible;

    public Brush? DeadlineForeground => _deadlineForeground;

    public Brush? DeadlineBackground => _deadlineBackground;

    public bool IsBlinking => _isBlinking;

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

    public Brush? GlowBrush => _glowBrush;

    public void Refresh()
    {
        var today = DateTime.Today;
        var deadline = DeadlineCalculator.GetEffectiveDeadline(Task);
        int? days = null;
        if (deadline.HasValue)
        {
            days = (int)(deadline.Value.Date - today.Date).TotalDays;
        }

        if (!deadline.HasValue)
        {
            _deadlineText = string.Empty;
            _deadlineVisible = false;
            _deadlineForeground = null;
            _deadlineBackground = null;
        }
        else if (days < 0)
        {
            _deadlineText = $"{days}j";
            _deadlineVisible = true;
            _deadlineForeground = Res("OverdueForeground");
            _deadlineBackground = Res("OverdueBackground");
        }
        else if (days == 0)
        {
            _deadlineText = "Aujourd'hui";
            _deadlineVisible = true;
            _deadlineForeground = Res("DueTodayForeground");
            _deadlineBackground = Res("DueTodayBackground");
        }
        else
        {
            _deadlineText = $"{days}j";
            _deadlineVisible = true;
            _deadlineForeground = Res("TextSecondaryBrush");
            _deadlineBackground = Res("SurfaceAltBrush");
        }

        _priorityLabel = Task.Priority switch
        {
            Priority.Low => "Faible",
            Priority.Medium => "Moyenne",
            Priority.Urgent => "Urgente",
            _ => "Très urgente",
        };

        (_priorityBackground, _priorityForeground) = Task.Priority switch
        {
            Priority.Low => (Res("PriorityLowBackground"), Res("PriorityLowForeground")),
            Priority.Medium => (Res("PriorityMediumBackground"), Res("PriorityMediumForeground")),
            Priority.Urgent => (Res("PriorityUrgentBackground"), Res("PriorityUrgentForeground")),
            _ => (Res("PriorityVeryUrgentBackground"), Res("PriorityVeryUrgentForeground")),
        };

        _categoryLabel = _categoryName ?? string.Empty;
        _hasCategory = _categoryName is not null;

        _isBlinking = !Task.IsCompleted &&
                      ((days.HasValue && days < 0) || Task.Priority == Priority.VeryUrgent);
        _glowBrush = Res("AlarmGlowBrush");

        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(IsCompleted));
        OnPropertyChanged(nameof(CategoryLabel));
        OnPropertyChanged(nameof(HasCategory));
        OnPropertyChanged(nameof(PriorityLabel));
        OnPropertyChanged(nameof(PriorityBackground));
        OnPropertyChanged(nameof(PriorityForeground));
        OnPropertyChanged(nameof(DeadlineText));
        OnPropertyChanged(nameof(DeadlineVisible));
        OnPropertyChanged(nameof(DeadlineForeground));
        OnPropertyChanged(nameof(DeadlineBackground));
        OnPropertyChanged(nameof(IsBlinking));
        OnPropertyChanged(nameof(GlowBrush));
    }

    private static Brush? Res(string key)
    {
        var value = Application.Current.TryFindResource(key) as Brush;
        return value ?? Brushes.Transparent;
    }
}
