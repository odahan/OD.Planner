using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Data.Sqlite;
using OD.Planner.Data;
using OD.Planner.Localization;
using OD.Planner.Models;
using OD.Planner.Services;

namespace OD.Planner.ViewModels;

/// <summary>
/// ViewModel for the Settings dialog.
/// Manages application settings including theme, alarms, categories, database, and language.
/// </summary>
public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppSettings _settings;
    private readonly AppDatabase _db;
    private readonly ThemeService _themes;
    private readonly Action<string> _onDatabaseChanged;
    private readonly Action<bool> _onShowCompletedChanged;
    private readonly Action _onCategoriesChanged;

    /// <summary>
    /// Gets the collection of task categories.
    /// </summary>
    public ObservableCollection<Category> Categories { get; } = new();

    /// <summary>
    /// Gets or sets the currently selected category.
    /// </summary>
    [ObservableProperty]
    private Category? selectedCategory;

    /// <summary>
    /// Gets or sets the new category name for add/rename operations.
    /// </summary>
    [ObservableProperty]
    private string newCategoryName = string.Empty;

    /// <summary>
    /// Gets or sets whether the category input is in rename mode.
    /// </summary>
    [ObservableProperty]
    private bool isRenameMode;

    /// <summary>
    /// Gets or sets the category error message.
    /// </summary>
    [ObservableProperty]
    private string? categoryError;

    /// <summary>
    /// Gets or sets whether the dark theme is enabled.
    /// </summary>
    [ObservableProperty]
    private bool isDarkTheme;

    /// <summary>
    /// Gets or sets whether completed tasks are shown.
    /// </summary>
    [ObservableProperty]
    private bool showCompleted;

    /// <summary>
    /// Gets or sets whether the application starts with Windows.
    /// </summary>
    [ObservableProperty]
    private bool autoStart;

    /// <summary>
    /// Gets or sets whether sound is enabled for alarms.
    /// </summary>
    [ObservableProperty]
    private bool soundEnabled;

    /// <summary>
    /// Gets or sets whether the J-1 (day before) alarm is enabled.
    /// </summary>
    [ObservableProperty]
    private bool j1Enabled;

    /// <summary>
    /// Gets or sets whether the J0 (due day) alarm is enabled.
    /// </summary>
    [ObservableProperty]
    private bool j0Enabled;

    /// <summary>
    /// Gets or sets whether the overdue alarm is enabled.
    /// </summary>
    [ObservableProperty]
    private bool overdueEnabled;

    /// <summary>
    /// Gets or sets whether animations are reduced for accessibility.
    /// </summary>
    [ObservableProperty]
    private bool reduceAnimations;

    /// <summary>
    /// Gets or sets the database file path.
    /// </summary>
    [ObservableProperty]
    private string dbPath = string.Empty;

    /// <summary>
    /// Gets the application version string.
    /// </summary>
    public string AppVersion => $"v{Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0"}";

    /// <summary>
    /// Gets whether English language is selected.
    /// </summary>
    public bool IsEnglishLanguage
    {
        get => _settings.Language == "en";
        set
        {
            if (value)
            {
                _settings.Language = "en";
                SettingsService.Save(_settings);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsFrenchLanguage));
                ApplyLanguage("en");
            }
        }
    }

    /// <summary>
    /// Gets whether French language is selected.
    /// </summary>
    public bool IsFrenchLanguage
    {
        get => _settings.Language == "fr";
        set
        {
            if (value)
            {
                _settings.Language = "fr";
                SettingsService.Save(_settings);
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsEnglishLanguage));
                ApplyLanguage("fr");
            }
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SettingsViewModel"/> class.
    /// </summary>
    public SettingsViewModel(
        AppSettings settings,
        AppDatabase db,
        ThemeService themes,
        Action<string> onDatabaseChanged,
        Action<bool> onShowCompletedChanged,
        Action onCategoriesChanged)
    {
        _settings = settings;
        _db = db;
        _themes = themes;
        _onDatabaseChanged = onDatabaseChanged;
        _onShowCompletedChanged = onShowCompletedChanged;
        _onCategoriesChanged = onCategoriesChanged;

        isDarkTheme = settings.IsDarkTheme;
        showCompleted = settings.ShowCompleted;
        autoStart = settings.AutoStartEnabled;
        soundEnabled = settings.SoundEnabled;
        j1Enabled = settings.J1Enabled;
        j0Enabled = settings.J0Enabled;
        overdueEnabled = settings.OverdueEnabled;
        reduceAnimations = settings.ReduceAnimations;
        dbPath = settings.DbPath ?? string.Empty;

        ReloadCategories();
    }

    /// <summary>
    /// Applies the specified language to the application.
    /// </summary>
    /// <param name="language">The language code ("en" or "fr").</param>
    private void ApplyLanguage(string language)
    {
        if (Application.Current is App app)
        {
            app.ApplyLanguage(language);
        }
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        _settings.IsDarkTheme = value;
        _themes.Apply(value);
        SettingsService.Save(_settings);
    }

    partial void OnShowCompletedChanged(bool value) => _onShowCompletedChanged(value);

    partial void OnAutoStartChanged(bool value)
    {
        _settings.AutoStartEnabled = value;
        if (!StartupService.SetEnabled(value))
        {
            _settings.AutoStartEnabled = !value;
            OnPropertyChanged(nameof(AutoStart));
        }
        SettingsService.Save(_settings);
    }

    partial void OnSoundEnabledChanged(bool value)
    {
        _settings.SoundEnabled = value;
        SettingsService.Save(_settings);
    }

    partial void OnJ1EnabledChanged(bool value)
    {
        _settings.J1Enabled = value;
        SettingsService.Save(_settings);
    }

    partial void OnJ0EnabledChanged(bool value)
    {
        _settings.J0Enabled = value;
        SettingsService.Save(_settings);
    }

    partial void OnOverdueEnabledChanged(bool value)
    {
        _settings.OverdueEnabled = value;
        SettingsService.Save(_settings);
    }

    partial void OnReduceAnimationsChanged(bool value)
    {
        _settings.ReduceAnimations = value;
        SettingsService.Save(_settings);
    }

    // ----- Categories -----

    /// <summary>
    /// Adds a new category or renames the selected category.
    /// </summary>
    [RelayCommand]
    private void AddCategory()
    {
        CategoryError = null;
        var name = NewCategoryName.Trim();
        if (string.IsNullOrEmpty(name))
        {
            return;
        }

        try
        {
            if (IsRenameMode)
            {
                if (SelectedCategory is null)
                {
                    return;
                }

                _db.RenameCategory(SelectedCategory.Id, name);
                IsRenameMode = false;
            }
            else
            {
                _db.AddCategory(name);
            }

            NewCategoryName = string.Empty;
            ReloadCategories();
            _onCategoriesChanged();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 19)
        {
            CategoryError = LocalizationService.Instance["CategoryExists"];
        }
        catch (Exception ex)
        {
            CategoryError = $"Erreur : {ex.Message}";
        }
    }

    /// <summary>
    /// Begins the rename mode for the selected category.
    /// </summary>
    [RelayCommand]
    private void BeginRename()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        CategoryError = null;
        NewCategoryName = SelectedCategory.Name;
        IsRenameMode = true;
    }

    /// <summary>
    /// Cancels the rename mode.
    /// </summary>
    [RelayCommand]
    private void CancelRename()
    {
        IsRenameMode = false;
        NewCategoryName = string.Empty;
        CategoryError = null;
    }

    /// <summary>
    /// Deletes the selected category after confirmation.
    /// </summary>
    [RelayCommand]
    private void DeleteCategory()
    {
        if (SelectedCategory is null)
        {
            return;
        }

        var message = string.Format(LocalizationService.Instance["ConfirmDeleteCategory"], SelectedCategory.Name);
        var result = MessageBox.Show(
            message,
            LocalizationService.Instance["AppTitle"],
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No);
        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        _db.DeleteCategory(SelectedCategory.Id);
        ReloadCategories();
        _onCategoriesChanged();
    }

    // ----- Database -----

    /// <summary>
    /// Changes the database file path.
    /// </summary>
    /// <param name="path">The new database file path.</param>
    public void ChangeDatabase(string path)
    {
        _settings.DbPath = path;
        SettingsService.Save(_settings);
        DbPath = path;
        _onDatabaseChanged(path);
        ReloadCategories();
    }

    /// <summary>
    /// Reloads the categories from the database.
    /// </summary>
    private void ReloadCategories()
    {
        Categories.Clear();
        foreach (var category in _db.GetCategories())
        {
            Categories.Add(category);
        }

        SelectedCategory = null;
    }
}
