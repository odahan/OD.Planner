using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OD.Planner.Localization;
using OD.Planner.Models;
using OD.Planner.Services;

namespace OD.Planner.ViewModels;

/// <summary>
/// ViewModel for individual alarm items in the alarm popup.
/// </summary>
public sealed partial class AlarmItemViewModel : ObservableObject
{
    private readonly AlarmEngine _engine;
    private readonly AlarmEntry _entry;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlarmItemViewModel"/> class.
    /// </summary>
    public AlarmItemViewModel(AlarmEntry entry, AlarmEngine engine)
    {
        _entry = entry;
        _engine = engine;
    }

    /// <summary>
    /// Gets the alarm entry.
    /// </summary>
    public AlarmEntry Entry => _entry;

    /// <summary>
    /// Gets the task title.
    /// </summary>
    public string Title => _entry.Task.Title;

    /// <summary>
    /// Gets the localized level text based on alarm level.
    /// </summary>
    public string LevelText => _entry.Level switch
    {
        AlarmLevel.Attention => LocalizationService.Instance["AlarmAttention"],
        AlarmLevel.Due => LocalizationService.Instance["AlarmDue"],
        _ => string.Format(LocalizationService.Instance["AlarmOverdue"], _entry.DaysRemaining),
    };

    /// <summary>
    /// Gets the brush for the alarm level indicator.
    /// </summary>
    public Brush LevelBrush => _entry.Level switch
    {
        AlarmLevel.Attention => Res("TextSecondaryBrush"),
        AlarmLevel.Due => Res("DueTodayForeground"),
        _ => Res("OverdueForeground"),
    };

    /// <summary>
    /// Gets the priority background brush.
    /// </summary>
    public Brush PriorityBackground => Res(_entry.Task.Priority switch
    {
        Priority.Low => "PriorityLowBackground",
        Priority.Medium => "PriorityMediumBackground",
        Priority.Urgent => "PriorityUrgentBackground",
        _ => "PriorityVeryUrgentBackground",
    });

    /// <summary>
    /// Gets the priority foreground brush.
    /// </summary>
    public Brush PriorityForeground => Res(_entry.Task.Priority switch
    {
        Priority.Low => "PriorityLowForeground",
        Priority.Medium => "PriorityMediumForeground",
        Priority.Urgent => "PriorityUrgentForeground",
        _ => "PriorityVeryUrgentForeground",
    });

    /// <summary>
    /// Gets the localized priority label.
    /// </summary>
    public string PriorityLabel => _entry.Task.Priority switch
    {
        Priority.Low => LocalizationService.Instance["PriorityLow"],
        Priority.Medium => LocalizationService.Instance["PriorityMedium"],
        Priority.Urgent => LocalizationService.Instance["PriorityUrgent"],
        _ => LocalizationService.Instance["PriorityVeryUrgent"],
    };

    /// <summary>
    /// Gets or sets the callback invoked when this alarm is resolved.
    /// </summary>
    public Action? OnResolved { get; set; }

    /// <summary>
    /// Snoozes this alarm for 1 hour.
    /// </summary>
    [RelayCommand]
    private void Snooze()
    {
        _engine.Snooze(_entry);
        OnResolved?.Invoke();
    }

    /// <summary>
    /// Stops this alarm permanently.
    /// </summary>
    [RelayCommand]
    private void Stop()
    {
        _engine.Stop(_entry);
        OnResolved?.Invoke();
    }

    /// <summary>
    /// Finds a brush resource by key.
    /// </summary>
    private static Brush Res(string key)
    {
        var value = Application.Current.TryFindResource(key) as Brush;
        return value ?? Brushes.Transparent;
    }
}

/// <summary>
/// ViewModel for the alarm popup window.
/// </summary>
public sealed partial class AlarmPopupViewModel : ObservableObject
{
    private readonly AlarmEngine _engine;

    /// <summary>
    /// Gets the collection of alarm items.
    /// </summary>
    public ObservableCollection<AlarmItemViewModel> Items { get; } = new();

    /// <summary>
    /// Gets whether there are any alarm items.
    /// </summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>
    /// Occurs when all alarms have been resolved.
    /// </summary>
    public event Action? AllResolved;

    /// <summary>
    /// Initializes a new instance of the <see cref="AlarmPopupViewModel"/> class.
    /// </summary>
    public AlarmPopupViewModel(IReadOnlyList<AlarmEntry> entries, AlarmEngine engine)
    {
        _engine = engine;
        foreach (var entry in entries)
        {
            var item = new AlarmItemViewModel(entry, engine);
            item.OnResolved = () => Remove(item);
            Items.Add(item);
        }
    }

    /// <summary>
    /// Removes an alarm item from the collection.
    /// </summary>
    public void Remove(AlarmItemViewModel item)
    {
        Items.Remove(item);
        OnPropertyChanged(nameof(HasItems));
        if (Items.Count == 0)
        {
            AllResolved?.Invoke();
        }
    }

    /// <summary>
    /// Snoozes all alarms for 1 hour.
    /// </summary>
    [RelayCommand]
    private void SnoozeAll()
    {
        foreach (var item in Items.ToList())
        {
            _engine.Snooze(item.Entry);
        }

        Items.Clear();
        OnPropertyChanged(nameof(HasItems));
        AllResolved?.Invoke();
    }

    /// <summary>
    /// Stops all alarms permanently.
    /// </summary>
    [RelayCommand]
    private void StopAll()
    {
        foreach (var item in Items.ToList())
        {
            _engine.Stop(item.Entry);
        }

        Items.Clear();
        OnPropertyChanged(nameof(HasItems));
        AllResolved?.Invoke();
    }
}
