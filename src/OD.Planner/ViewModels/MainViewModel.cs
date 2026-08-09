using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OD.Planner.Data;
using OD.Planner.Logic;
using OD.Planner.Models;
using OD.Planner.Services;
using OD.Planner.Views;

namespace OD.Planner.ViewModels;

public sealed class CategoryFilter
{
    public string DisplayName { get; init; } = string.Empty;
    public long? CategoryId { get; init; }

    public override string ToString() => DisplayName;
}

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly ThemeService _themes;
    private readonly AlarmEngine _alarmEngine;
    private readonly DispatcherTimer _blinkTimer;
    private readonly Dictionary<long, string> _categoryNames = new();
    private AppDatabase _db;
    private bool _blinkOn;

    public ObservableCollection<TaskItemViewModel> Tasks { get; } = new();
    public ObservableCollection<CategoryFilter> CategoryFilters { get; } = new();

    [ObservableProperty]
    private CategoryFilter? selectedCategoryFilter;

    [ObservableProperty]
    private TaskItemViewModel? selectedTask;

    [ObservableProperty]
    private bool showCompleted;

    public bool HasTasks => Tasks.Count > 0;

    public bool NoTasks => Tasks.Count == 0;

    public MainViewModel(AppDatabase db, AppSettings settings, ThemeService themes, AlarmEngine alarmEngine)
    {
        _db = db;
        _settings = settings;
        _themes = themes;
        _alarmEngine = alarmEngine;
        showCompleted = settings.ShowCompleted;

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
        _blinkTimer.Tick += (_, _) => OnBlinkTick();
        _themes.ThemeChanged += _ => RefreshList();

        RefreshCategories();
        RefreshList();
        UpdateBlinkTimer();
    }

    partial void OnShowCompletedChanged(bool value)
    {
        _settings.ShowCompleted = value;
        SettingsService.Save(_settings);
        RefreshList();
    }

    partial void OnSelectedCategoryFilterChanged(CategoryFilter? value) => RefreshList();

    public void RefreshCategories()
    {
        var selectedId = SelectedCategoryFilter?.CategoryId;
        _categoryNames.Clear();
        foreach (var cat in _db.GetCategories())
        {
            _categoryNames[cat.Id] = cat.Name;
        }

        CategoryFilters.Clear();
        CategoryFilters.Add(new CategoryFilter { DisplayName = "Toutes", CategoryId = null });
        foreach (var cat in _categoryNames.OrderBy(kv => kv.Value, StringComparer.CurrentCultureIgnoreCase))
        {
            CategoryFilters.Add(new CategoryFilter { DisplayName = cat.Value, CategoryId = cat.Key });
        }

        SelectedCategoryFilter = CategoryFilters.FirstOrDefault(f => f.CategoryId == selectedId) ?? CategoryFilters[0];
    }

    public void RefreshList()
    {
        IEnumerable<PlannerTask> query = _db.GetTasks();
        if (SelectedCategoryFilter?.CategoryId is long categoryId)
        {
            query = query.Where(t => t.CategoryId == categoryId);
        }

        if (!ShowCompleted)
        {
            query = query.Where(t => !t.IsCompleted);
        }

        var ordered = TaskSortService.Sort(query);

        Tasks.Clear();
        foreach (var task in ordered)
        {
            var categoryName = task.CategoryId is long id && _categoryNames.TryGetValue(id, out var name)
                ? name
                : null;
            Tasks.Add(new TaskItemViewModel(task, categoryName));
        }

        OnPropertyChanged(nameof(HasTasks));
        OnPropertyChanged(nameof(NoTasks));
        UpdateBlinkTimer();
    }

    public void RefreshDeadlines()
    {
        foreach (var item in Tasks)
        {
            item.Refresh();
        }

        UpdateBlinkTimer();
    }

    public void ChangeDatabase(string path)
    {
        _db = new AppDatabase(path);
        _db.EnsureCreated();
        _alarmEngine.ResetSession();
        RefreshCategories();
        RefreshList();
    }

    // ----- Commands -----

    [RelayCommand]
    private void AddTask() => ShowEditor(null);

    [RelayCommand]
    private void EditTask(TaskItemViewModel? item)
    {
        if (item is not null)
        {
            ShowEditor(item.Task);
        }
    }

    [RelayCommand]
    private void EditSelectedTask() => ShowEditor(SelectedTask?.Task);

    [RelayCommand]
    private void DeleteTask(TaskItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var result = MessageBox.Show(
            $"Supprimer la tâche « {item.Title} » ?",
            "OD.Planner",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result == MessageBoxResult.Yes)
        {
            _db.DeleteTask(item.Task.Id);
            RefreshList();
        }
    }

    [RelayCommand]
    private void DeleteSelectedTask() => DeleteTask(SelectedTask);

    [RelayCommand]
    private void ToggleCompleted(TaskItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var task = item.Task;
        task.IsCompleted = !task.IsCompleted;
        task.CompletedAt = task.IsCompleted ? DateTime.Now : null;
        _db.UpdateTask(task);
        RefreshList();
    }

    private bool CanAddOneDay(TaskItemViewModel? item) => item?.Task.DeadlineType != DeadlineType.None;

    [RelayCommand(CanExecute = nameof(CanAddOneDay))]
    private void AddOneDay(TaskItemViewModel? item) => ShiftDeadline(item, 1);

    private bool CanAddOneWeek(TaskItemViewModel? item) => item?.Task.DeadlineType != DeadlineType.None;

    [RelayCommand(CanExecute = nameof(CanAddOneWeek))]
    private void AddOneWeek(TaskItemViewModel? item) => ShiftDeadline(item, 7);

    private void ShiftDeadline(TaskItemViewModel? item, int days)
    {
        if (item is null)
        {
            return;
        }

        var task = item.Task;
        switch (task.DeadlineType)
        {
            case DeadlineType.DaysFromCreation when task.DeadlineDays is int d:
                task.DeadlineDays = d + days;
                break;
            case DeadlineType.FixedDate when task.DeadlineDate is DateTime date:
                task.DeadlineDate = date.Date.AddDays(days);
                break;
            default:
                return;
        }

        _db.UpdateTask(task);
        RefreshList();
    }

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel(
            _settings,
            _db,
            _themes,
            onDatabaseChanged: ChangeDatabase,
            onShowCompletedChanged: value => ShowCompleted = value,
            onCategoriesChanged: RefreshCategories);
        var dialog = new SettingsDialog { DataContext = vm, Owner = Application.Current.MainWindow };
        dialog.ShowDialog();
    }

    private void ShowEditor(PlannerTask? task)
    {
        var vm = new TaskEditViewModel(task, _db.GetCategories());
        var dialog = new TaskEditDialog { DataContext = vm, Owner = Application.Current.MainWindow };
        if (dialog.ShowDialog() != true || !vm.Save())
        {
            return;
        }

        if (vm.IsNew)
        {
            vm.Task.Id = _db.InsertTask(vm.Task);
        }
        else
        {
            _db.UpdateTask(vm.Task);
        }

        RefreshList();
    }

    // ----- Blink -----

    private void UpdateBlinkTimer()
    {
        if (_settings.ReduceAnimations)
        {
            _blinkTimer.Stop();
            _blinkOn = false;
            foreach (var item in Tasks)
            {
                item.BlinkOn = item.IsBlinking;
            }
        }
        else
        {
            if (Tasks.Any(t => t.IsBlinking))
            {
                _blinkTimer.Start();
            }
            else
            {
                _blinkTimer.Stop();
                _blinkOn = false;
                foreach (var item in Tasks)
                {
                    item.BlinkOn = false;
                }
            }
        }
    }

    private void OnBlinkTick()
    {
        _blinkOn = !_blinkOn;
        foreach (var item in Tasks)
        {
            if (item.IsBlinking)
            {
                item.BlinkOn = _blinkOn;
            }
        }
    }
}
