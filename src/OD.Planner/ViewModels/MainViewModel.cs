using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OD.Planner.Data;
using OD.Planner.Localization;
using OD.Planner.Logic;
using OD.Planner.Models;
using OD.Planner.Services;
using OD.Planner.Views;

namespace OD.Planner.ViewModels;

/// <summary>
/// Represents a category filter option in the main window.
/// </summary>
public sealed class CategoryFilter
{
    /// <summary>
    /// Gets the display name for the filter.
    /// </summary>
    public string DisplayName { get; init; } = string.Empty;

    /// <summary>
    /// Gets the category ID (null for "All").
    /// </summary>
    public long? CategoryId { get; init; }

    /// <summary>
    /// Returns the display name.
    /// </summary>
    public override string ToString() => DisplayName;
}

/// <summary>
/// ViewModel for the main application window.
/// Manages the task list, commands, and window state.
/// </summary>
public sealed partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly AppSettings _settings;
    private readonly ThemeService _themes;
    private readonly AlarmEngine _alarmEngine;
    private readonly DispatcherTimer _blinkTimer;
    private readonly Dictionary<long, string> _categoryNames = new();
    private readonly Action<bool> _themeChangedHandler;
    private AppDatabase _db;
    private bool _blinkOn;
    private bool _disposed;

    /// <summary>
    /// Gets the collection of task items.
    /// </summary>
    public ObservableCollection<TaskItemViewModel> Tasks { get; } = new();

    /// <summary>
    /// Gets the collection of category filters.
    /// </summary>
    public ObservableCollection<CategoryFilter> CategoryFilters { get; } = new();

    /// <summary>
    /// Gets or sets the selected category filter.
    /// </summary>
    [ObservableProperty]
    private CategoryFilter? selectedCategoryFilter;

    /// <summary>
    /// Gets or sets the selected task.
    /// </summary>
    [ObservableProperty]
    private TaskItemViewModel? selectedTask;

    /// <summary>
    /// Gets or sets whether completed tasks are shown in the list.
    /// </summary>
    [ObservableProperty]
    private bool showCompleted;

    /// <summary>
    /// Gets whether there are any tasks.
    /// </summary>
    public bool HasTasks => Tasks.Count > 0;

    /// <summary>
    /// Gets whether there are no tasks.
    /// </summary>
    public bool NoTasks => Tasks.Count == 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainViewModel"/> class.
    /// </summary>
    public MainViewModel(AppDatabase db, AppSettings settings, ThemeService themes, AlarmEngine alarmEngine)
    {
        _db = db;
        _settings = settings;
        _themes = themes;
        _alarmEngine = alarmEngine;
        showCompleted = settings.ShowCompleted;

        _blinkTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(650) };
        _blinkTimer.Tick += (_, _) => OnBlinkTick();
        _themeChangedHandler = _ => RefreshList();
        _themes.ThemeChanged += _themeChangedHandler;
        LocalizationService.LanguageChangedStatic += OnLanguageChanged;

        RefreshCategories();
        RefreshList();
        UpdateBlinkTimer();
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        RefreshCategories();
        RefreshList();
        RefreshLocalizedText();
    }

    partial void OnShowCompletedChanged(bool value)
    {
        _settings.ShowCompleted = value;
        SettingsService.Save(_settings);
        RefreshList();
    }

    partial void OnSelectedCategoryFilterChanged(CategoryFilter? value) => RefreshList();

    /// <summary>
    /// Refreshes the category filter list.
    /// </summary>
    public void RefreshCategories()
    {
        var selectedId = SelectedCategoryFilter?.CategoryId;
        _categoryNames.Clear();
        foreach (var cat in _db.GetCategories())
        {
            _categoryNames[cat.Id] = cat.Name;
        }

        CategoryFilters.Clear();
        CategoryFilters.Add(new CategoryFilter { DisplayName = LocalizationService.Instance["ShowAllCategories"], CategoryId = null });
        foreach (var cat in _categoryNames.OrderBy(kv => kv.Value, StringComparer.CurrentCultureIgnoreCase))
        {
            CategoryFilters.Add(new CategoryFilter { DisplayName = cat.Value, CategoryId = cat.Key });
        }

        SelectedCategoryFilter = CategoryFilters.FirstOrDefault(f => f.CategoryId == selectedId) ?? CategoryFilters[0];
    }

    /// <summary>
    /// Refreshes the task list.
    /// </summary>
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

        var ordered = TaskSortService.Sort(query).ToList();

        // Rebuild the list to ensure sorting and filtering are applied
        Tasks.Clear();
        foreach (var task in ordered)
        {
            var categoryName = task.CategoryId is long id && _categoryNames.TryGetValue(id, out var name)
                ? name
                : null;
            Tasks.Add(new TaskItemViewModel(task, categoryName));
        }

        OnPropertyChanged(string.Empty);
        UpdateBlinkTimer();
    }

    /// <summary>
    /// Refreshes localized text for all existing task items without rebuilding the list.
    /// Called when language changes to update labels immediately.
    /// </summary>
    public void RefreshLocalizedText()
    {
        foreach (var item in Tasks)
        {
            item.Refresh();
        }

        OnPropertyChanged(string.Empty);
    }

    /// <summary>
    /// Refreshes the deadline display for all tasks.
    /// </summary>
    public void RefreshDeadlines()
    {
        foreach (var item in Tasks)
        {
            item.Refresh();
        }

        UpdateBlinkTimer();
    }

    /// <summary>
    /// Changes the database location.
    /// </summary>
    public void ChangeDatabase(string path)
    {
        _db = new AppDatabase(path);
        _db.EnsureCreated();
        _alarmEngine.SetTaskSource(() => _db.GetTasks());
        RefreshCategories();
        RefreshList();
    }

    // ----- Commands -----

    /// <summary>
    /// Shows the task editor to create a new task.
    /// </summary>
    [RelayCommand]
    private void AddTask() => ShowEditor(null);

    /// <summary>
    /// Shows the task editor for the specified task.
    /// </summary>
    [RelayCommand]
    private void EditTask(TaskItemViewModel? item)
    {
        if (item is not null)
        {
            ShowEditor(item.Task);
        }
    }

    /// <summary>
    /// Shows the task editor for the selected task.
    /// </summary>
    [RelayCommand]
    private void EditSelectedTask() => ShowEditor(SelectedTask?.Task);

    /// <summary>
    /// Deletes the specified task after confirmation.
    /// </summary>
    [RelayCommand]
    private void DeleteTask(TaskItemViewModel? item)
    {
        if (item is null)
        {
            return;
        }

        var message = string.Format(LocalizationService.Instance["ConfirmDeleteTask"], item.Title);
        var result = MessageBox.Show(
            message,
            LocalizationService.Instance["AppTitle"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result == MessageBoxResult.Yes)
        {
            _db.DeleteTask(item.Task.Id);
            RefreshList();
        }
    }

    /// <summary>
    /// Deletes the selected task.
    /// </summary>
    [RelayCommand]
    private void DeleteSelectedTask() => DeleteTask(SelectedTask);

    /// <summary>
    /// Toggles the completed state of the specified task.
    /// </summary>
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

    /// <summary>
    /// Adds one day to the task deadline.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanAddOneDay))]
    private void AddOneDay(TaskItemViewModel? item) => ShiftDeadline(item, 1);

    private bool CanAddOneWeek(TaskItemViewModel? item) => item?.Task.DeadlineType != DeadlineType.None;

    /// <summary>
    /// Adds one week to the task deadline.
    /// </summary>
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

    /// <summary>
    /// Opens the settings dialog.
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel(
            _settings,
            _db,
            _themes,
            onDatabaseChanged: ChangeDatabase,
            onCategoriesChanged: RefreshCategories);
        var dialog = new SettingsDialog { DataContext = vm, Owner = Application.Current.MainWindow };
        dialog.Language = System.Windows.Markup.XmlLanguage.GetLanguage(LocalizationService.Instance.CurrentCulture.Name);
        dialog.ShowDialog();
    }

    private void ShowEditor(PlannerTask? task)
    {
        var vm = new TaskEditViewModel(task, _db.GetCategories());
        var dialog = new TaskEditDialog { DataContext = vm, Owner = Application.Current.MainWindow };
        dialog.Language = System.Windows.Markup.XmlLanguage.GetLanguage(LocalizationService.Instance.CurrentCulture.Name);
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

    /// <summary>
    /// Disposes the view model and unsubscribes from events.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _blinkTimer.Stop();
        _themes.ThemeChanged -= _themeChangedHandler;
        LocalizationService.LanguageChangedStatic -= OnLanguageChanged;
    }
}
