using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OD.Planner.Models;
using OD.Planner.Services;

namespace OD.Planner.ViewModels;

public sealed partial class AlarmItemViewModel : ObservableObject
{
    private readonly AlarmEngine _engine;
    private readonly AlarmEntry _entry;

    public AlarmItemViewModel(AlarmEntry entry, AlarmEngine engine)
    {
        _entry = entry;
        _engine = engine;
    }

    public AlarmEntry Entry => _entry;

    public string Title => _entry.Task.Title;

    public string LevelText => _entry.Level switch
    {
        AlarmLevel.Attention => "J-1 — échéance demain",
        AlarmLevel.Due => "J0 — échéance aujourd'hui",
        _ => $"{_entry.DaysRemaining}j — échéance dépassée",
    };

    public Brush LevelBrush => _entry.Level switch
    {
        AlarmLevel.Attention => Res("TextSecondaryBrush"),
        AlarmLevel.Due => Res("DueTodayForeground"),
        _ => Res("OverdueForeground"),
    };

    public Brush PriorityBackground => Res(_entry.Task.Priority switch
    {
        Priority.Low => "PriorityLowBackground",
        Priority.Medium => "PriorityMediumBackground",
        Priority.Urgent => "PriorityUrgentBackground",
        _ => "PriorityVeryUrgentBackground",
    });

    public Brush PriorityForeground => Res(_entry.Task.Priority switch
    {
        Priority.Low => "PriorityLowForeground",
        Priority.Medium => "PriorityMediumForeground",
        Priority.Urgent => "PriorityUrgentForeground",
        _ => "PriorityVeryUrgentForeground",
    });

    public string PriorityLabel => _entry.Task.Priority switch
    {
        Priority.Low => "Faible",
        Priority.Medium => "Moyenne",
        Priority.Urgent => "Urgente",
        _ => "Très urgente",
    };

    public Action? OnResolved { get; set; }

    [RelayCommand]
    private void Snooze()
    {
        _engine.Snooze(_entry);
        OnResolved?.Invoke();
    }

    [RelayCommand]
    private void Stop()
    {
        _engine.Stop(_entry);
        OnResolved?.Invoke();
    }

    private static Brush Res(string key)
    {
        var value = Application.Current.TryFindResource(key) as Brush;
        return value ?? Brushes.Transparent;
    }
}

public sealed partial class AlarmPopupViewModel : ObservableObject
{
    private readonly AlarmEngine _engine;

    public ObservableCollection<AlarmItemViewModel> Items { get; } = new();

    public bool HasItems => Items.Count > 0;

    public event Action? AllResolved;

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

    public void Remove(AlarmItemViewModel item)
    {
        Items.Remove(item);
        OnPropertyChanged(nameof(HasItems));
        if (Items.Count == 0)
        {
            AllResolved?.Invoke();
        }
    }

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
